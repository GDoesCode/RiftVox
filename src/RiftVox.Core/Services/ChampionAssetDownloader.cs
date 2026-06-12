using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace RiftVox.Core.Services;

/// <summary>
/// Manages the automated retrieval and local disk caching of champion portrait image assets from Riot's Data Dragon servers.
/// </summary>
public class ChampionAssetDownloader
{
    /// <summary>The shared HTTP network transaction worker instance.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>The memory buffer caching the active version string once fetched.</summary>
    private string? _cachedLatestVersion;

    /// <summary>Initializes a new instance of the ChampionAssetDownloader class.</summary>
    /// <param name="httpClient">The injected network communications handler client.</param>
    public ChampionAssetDownloader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Evaluates local asset presence and downloads missing champion portraits directly from the static content servers.
    /// </summary>
    /// <param name="championName">The formal database key identifier string representing the target champion.</param>
    /// <param name="targetFolder">The local filesystem path location designated to preserve asset data streams.</param>
    /// <param name="patchVersion">Optional override version. If left null, dynamically resolves the active game patch.</param>
    /// <returns>A task tracking the state outcome of the asynchronous transmission pipelines.</returns>
    public async Task DownloadIconAsync(string championName, string targetFolder, string? patchVersion = null)
    {
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        /// <summary>Resolve the active game patch dynamically if none was specified.</summary>
        string activeVersion = patchVersion ?? await GetLatestVersionAsync();

        /// <summary>Handle internal edge-case key mappings (e.g., Wukong is tracked as MonkeyKing).</summary>
        string optimizedName = championName.Replace(" ", "");
        if (optimizedName.Equals("Wukong", StringComparison.OrdinalIgnoreCase))
        {
            optimizedName = "MonkeyKing";
        }

        string localFileName = $"{championName}.png";
        string targetFilePath = Path.Combine(targetFolder, localFileName);

        /// <summary>If we already have this icon cached locally, skip the network roundtrip entirely.</summary>
        if (File.Exists(targetFilePath)) return;

        string remoteUrl = $"https://ddragon.leagueoflegends.com/cdn/{activeVersion}/img/champion/{optimizedName}.png";

        try
        {
            byte[] responseBytes = await _httpClient.GetByteArrayAsync(remoteUrl);
            await File.WriteAllBytesAsync(targetFilePath, responseBytes);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to acquire asset map data for champion token target: {optimizedName} on patch {activeVersion}", ex);
        }
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
            string jsonResponse = await _httpClient.GetStringAsync(versionUrl);
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