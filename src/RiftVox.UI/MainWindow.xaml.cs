using System.Windows;
using RiftVox.Core;

namespace RiftVox.UI;

public partial class MainWindow : Window
{
    private readonly RiotApiClient _apiClient = new();

    public MainWindow()
    {
        InitializeComponent();
        // Runs on startup
        /// <summary>Asynchronously check for an active game connection on window boot.</summary>
        Loaded += async (s, e) => await TestLiveClientApiAsync();
    }

    /// <summary>Runs diagnostic calculations for screen positioning based on local configuration.</summary>
    private void TestConfigParser()
    {
        var parser = new GameConfigParser();

        // Point this to your configuration path
        string mockPath = @"C:\Riot Games\League of Legends\Config\game.cfg";
        parser.LoadConfig(mockPath);

        var (x, y, size) = parser.GetMinimapBounds();

        string uiOutput = $"--- RIFTVOX CALIBRATION METRICS ---\n\n" +
                          $"Detected Game Resolution : {parser.Width}x{parser.Height}\n" +
                          $"Detected Minimap Scale   : {parser.MinimapScale}\n" +
                          $"Minimap Side Alignment   : {(parser.MinimapOnLeft ? "LEFT" : "RIGHT")}\n\n" +
                          $"Target Capture Window    : X={x}, Y={y} ({size}x{size}px)";

        // Send it to UI text box
        ConfigDisplayTextBox.Text = uiOutput;
    }

    /// <summary>Pings local loopback to output the live player roster to the diagnostic log box.</summary>
    private async Task TestLiveClientApiAsync()
    {
        ConfigDisplayTextBox.Text = "Pinging Riot Live Client API (Make sure you are in a custom/live match)...";

        var players = await _apiClient.GetPlayerListAsync();

        if (players == null || players.Count == 0)
        {
            ConfigDisplayTextBox.Text = "🔴 FAILED: Could not connect to match data.\n\n" +
                                        "Reasons:\n" +
                                        "1. League of Legends game client is not running.\n" +
                                        "2. You are in the main lobby menu, not an active live/custom match.";
            return;
        }

        string output = $"🟢 SUCCESS: Connected to live match!\n";
        output += $"=====================================\n\n";

        foreach (var p in players)
        {
            output += $"[{p.Team}] {p.SummonerName} -> Playing: {p.ChampionName} (Status: {(p.IsDead ? "DEAD" : "ALIVE")})\n";
        }

        ConfigDisplayTextBox.Text = output;
    }
}