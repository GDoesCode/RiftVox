using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace RiftVox.Core;

public class RiotApiClient
{
    /// <summary>HttpClient configured specifically to bypass local loopback SSL validation errors.</summary>
    private readonly HttpClient _httpClient;

    public RiotApiClient()
    {
        // Bypass the self-signed certificate check specifically for Riot's local server
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://127.0.0.1:2999/"),
            Timeout = TimeSpan.FromSeconds(2) // Short timeout so our UI doesn't hang if the game isn't open
        };
    }

    /// <summary>
    /// Asynchronously queries Riot's local live client port to fetch the active match roster.
    /// </summary>
    /// <returns>A list of populated <see cref="Player"/> objects, or null if the client is closed or lobby bound.</returns>
    public async Task<List<Player>?> GetPlayerListAsync()
    {
        try
        {
            // Riot uses camelCase in JSON, but our C# class uses PascalCase. 
            // Turning on CaseInsensitive mapping links them flawlessly.
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            return await _httpClient.GetFromJsonAsync<List<Player>>("liveclientdata/playerlist", options);
        }
        catch (HttpRequestException)
        {
            // This exception fires if the game client isn't actively running or if you aren't in a match
            return null;
        }
        catch (Exception)
        {
            // Catch-all for unexpected parsing drops
            return null;
        }
    }
}