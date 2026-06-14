using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RiftVox.Core.Services;

/// <summary>
/// Manages the automated retrieval and local disk caching of champion portrait image assets from Riot's Data Dragon servers.
/// </summary>
/// <remarks>Initializes a new instance of the ChampionAssetDownloader class.</remarks>
/// <param name="httpClient">The injected network communications handler client.</param>
public class ChampionAssetDownloader(HttpClient httpClient)
{

    /// <summary>The memory buffer caching the active version string once fetched.</summary>
    private string? _cachedLatestVersion;

    /// <summary>
    /// Dictionary mapping known champion display name exceptions to their exact Data Dragon ID counterparts.
    /// </summary>
    private static readonly Dictionary<string, string> SpecialCaseMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Wukong", "MonkeyKing" },
        { "Nunu & Willump", "Nunu" },
        { "Nunu and Willump", "Nunu" },
        { "Renata Glasc", "Renata" },
        { "LeBlanc", "Leblanc" }, // Data Dragon uses lowercase 'b'
        { "Bel'Veth", "Belveth" }  // Data Dragon uses lowercase 'v'
    };

    /// <summary>
    /// Evaluates local asset presence and downloads missing champion portraits directly from the static content servers.
    /// </summary>
    /// <param name="championName">The formal database key identifier string representing the target champion.</param>
    /// <param name="targetFolder">The local filesystem path location designated to preserve asset data streams.</param>
    /// <param name="patchVersion">Optional override version. If left null, dynamically resolves the active game patch.</param>
    /// <returns>A task tracking the state outcome of the asynchronous transmission pipelines.</returns>
    public async Task DownloadIconAsync(string championName, string targetFolder, string? patchVersion = null)
    {
        if (string.IsNullOrWhiteSpace(targetFolder))
        {
            throw new ArgumentException("Target folder path cannot be null or whitespace.", nameof(targetFolder));
        }
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        string cleanName = await GetCleanChampionName(championName);

        /// <summary>Resolve the active game patch dynamically if none was specified.</summary>
        string activeVersion = patchVersion ?? await GetLatestVersionAsync();

        string targetFilePath = $"{targetFolder}\\{cleanName}.png";

        /// <summary>If we already have this icon cached locally, skip the network roundtrip entirely.</summary>
        if (File.Exists(targetFilePath)) return;

        string remoteUrl = $"https://ddragon.leagueoflegends.com/cdn/{activeVersion}/img/champion/{cleanName}.png";

        try
        {
            byte[] responseBytes = await httpClient.GetByteArrayAsync(remoteUrl);
            await File.WriteAllBytesAsync(targetFilePath, responseBytes);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to acquire asset map data for champion token target: {cleanName} on patch {activeVersion}", ex);
        }
    }

    public static async Task<string> GetCleanChampionName(string championName)
    {
        string trimmedName = championName.Trim();

        // 1. Check if the name is an outright exception
        if (SpecialCaseMap.TryGetValue(trimmedName, out var exactMatch))
        {
            return exactMatch;
        }

        // 2. Remove punctuation: apostrophes, periods, hyphens
        // This fixes: Cho'Gath -> ChoGath, Dr. Mundo -> Dr Mundo, Kai'Sa -> KaiSa
        string sanitized = Regex.Replace(trimmedName, @"['\.\-]", "");

        // 3. Remove all spaces
        // This fixes: Lee Sin -> LeeSin, Master Yi -> MasterYi, Dr Mundo -> DrMundo
        sanitized = sanitized.Replace(" ", "");

        // 4. Enforce strict CamelCase if lowercase letters follow an apostrophe removal
        // Riot's Data Dragon lowercase the letter after an apostrophe (e.g., "Chogath", not "ChoGath")
        if (sanitized.StartsWith("ChoG")) sanitized = "Chogath";
        if (sanitized.StartsWith("KaiS")) sanitized = "Kaisa";
        if (sanitized.StartsWith("KhaZ")) sanitized = "Khazix";
        if (sanitized.StartsWith("KogM")) sanitized = "KogMaw"; // KogMaw keeps capital M
        if (sanitized.StartsWith("VelK")) sanitized = "Velkoz";

        //Add Kayle different icon after lvl 11.

        return sanitized;
    }

    /// <summary>
    /// Queries Riot's version control ledger to safely extract the absolute newest live game build version token string.
    /// </summary>
    /// <returns>A task returning the resolved version token string sequence.</returns>
    public async Task<string> GetLatestVersionAsync()
    {
        /// <summary>Return memory cache if we already resolved the version during this session initialization run.</summary>
        if (!string.IsNullOrEmpty(_cachedLatestVersion)) return _cachedLatestVersion;

        const string versionUrl = "https://ddragon.leagueoflegends.com/api/versions.json";

        try
        {
            string jsonResponse = await httpClient.GetStringAsync(versionUrl);
            using var jsonDocument = JsonDocument.Parse(jsonResponse);

            /// <summary>The endpoint returns a JSON string array; index 0 is always the absolute latest patch.</summary>
            string? latest = jsonDocument.RootElement[0].GetString();

            if (string.IsNullOrEmpty(latest))
            {
                throw new InvalidOperationException("Riot version array structure returned an empty patch token data entry.");
            }

            _cachedLatestVersion = latest;
            return _cachedLatestVersion;
        }
        catch (Exception)
        {
            /// <summary>Fallback safety buffer to prevent a hard crash if Riot's version site goes down completely.</summary>
            return "14.3.1";
        }
    }
}