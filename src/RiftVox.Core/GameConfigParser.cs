namespace RiftVox.Core;

public class GameConfigParser
{
    public int Width { get; private set; } = 1920;
    public int Height { get; private set; } = 1080;
    public double MinimapScale { get; private set; } = 1.0;
    public bool MinimapOnLeft { get; private set; } = false;

    public void LoadConfig(string filePath)
    {
        if (!File.Exists(filePath)) return;

        foreach (var line in File.ReadLines(filePath))
        {
            string trimmed = line.Trim();

            // Skip empty lines or section headers like [HUD]
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("[")) continue;

            var parts = trimmed.Split('=', 2);
            if (parts.Length != 2) continue;

            string key = parts[0].Trim();
            string val = parts[1].Trim();

            // Fully expanded condition tree to prevent CS0201
            if (key == "Width")
            {
                Width = int.Parse(val);
            }
            else if (key == "Height")
            {
                Height = int.Parse(val);
            }
            else if (key == "MinimapScale")
            {
                if (double.TryParse(val, out var parsedScale))
                {
                    MinimapScale = parsedScale;
                }
            }
            else if (key == "FlipMinimap")
            {
                MinimapOnLeft = (val == "1");
            }
        }
    }

    public (int x, int y, int size) GetMinimapBounds()
    {
        // Calculate responsive base size based on vertical monitor height
        double baseSize = 272.0 * (Height / 1080.0);
        int finalMapSize = (int)Math.Round(baseSize * MinimapScale);

        // Adjust horizontal anchor based on whether user flipped the map alignment
        int xCoord = MinimapOnLeft
            ? 0                             // Anchored to the far left monitor edge
            : Width - finalMapSize;         // Anchored to the right monitor edge

        // Vertical position is always anchored to the bottom edge
        int yCoord = Height - finalMapSize;

        return (xCoord, yCoord, finalMapSize);
    }
}