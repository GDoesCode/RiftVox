using System.Text.RegularExpressions;

public static class LeaguePathResolver
{
    /// <summary>
    /// Dynamically locates the player's game.cfg file without hardcoding the drive or directory.
    /// </summary>
    /// <returns>The full path string if found; otherwise, null.</returns>
    public static string GetGameCfgPath()
    {
        // 1. Check the absolute standard default path first
        string defaultPath = @"C:\Riot Games\League of Legends\Config\game.cfg";
        if (File.Exists(defaultPath))
        {
            return defaultPath;
        }

        // 2. Fallback: Parse Riot Client's hidden metadata (handles custom folders, D:\ or E:\ drives)
        try
        {
            // Resolves to "C:\ProgramData" dynamically
            string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string metadataPath = Path.Combine(programData, @"Riot Games\Metadata\league_of_legends.live\league_of_legends.live.product_settings.yaml");

            if (File.Exists(metadataPath))
            {
                string[] lines = File.ReadAllLines(metadataPath);
                foreach (string line in lines)
                {
                    // Find the keys detailing where the game folder lives
                    if (line.Contains("product_install_full_path:") || line.Contains("product_install_root:"))
                    {
                        // Strip out yaml syntax, quotes, and whitespace to extract the raw folder path
                        var match = Regex.Match(line, @":\s*""?(.*?)""?\s*$");
                        if (match.Success)
                        {
                            string installDir = match.Groups[1].Value.Replace('/', '\\').Trim();
                            string candidatePath = Path.Combine(installDir, @"Config\game.cfg");

                            if (File.Exists(candidatePath))
                            {
                                return candidatePath;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Absorb background IO errors quietly to head into the next safety fallback
        }

        // 3. Hail Mary Fallback: Check standard folder structures across all attached SSDs/HDDs
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.IsReady && drive.DriveType == DriveType.Fixed)
            {
                string customDrivePath = Path.Combine(drive.Name, @"Riot Games\League of Legends\Config\game.cfg");
                if (File.Exists(customDrivePath))
                {
                    return customDrivePath;
                }
            }
        }

        // If the game isn't installed or file is entirely missing
        return string.Empty;
    }
}