using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using RiftVox.Core.Models;
using RiftVox.Core.Services;
using RiftVox.Core.Platforms;

namespace RiftVox.UI;

public partial class MainWindow : Window
{
    private readonly RiotApiClient _apiClient = new();
    private readonly VisionCaptureEngine _captureEngine = new(new WindowsScreenCapturer());
    private MatchMonitorService _monitorService;
    private string? _localPlayerName;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize our automatic polling coordinator
        _monitorService = new MatchMonitorService(_apiClient);

        // Wire up state mutation triggers
        _monitorService.StateChanged += MonitorService_StateChanged;
        _monitorService.MatchStarted += async (s, e) => await HandleMatchStartAsync();
        _monitorService.MatchEnded += HandleMatchEnd;

        // Wire up our fast 200ms minimap vision ticker ticks
        _captureEngine.PositionsUpdated += Engine_PositionsUpdated;

        Loaded += (s, e) => {
            _monitorService.StartMonitoring();
            ConfigDisplayTextBox.Text = "🎙️ RiftVox Initialized. Standing by for client connectivity...";
        };

        Closed += (s, e) => {
            _monitorService.StopMonitoring();
            _captureEngine.StopCaptureLoop();
        };
    }

    private void MonitorService_StateChanged(object? sender, AppState newState)
    {
        // Thread-safe update of UI components based on client state switches
        Dispatcher.InvokeAsync(() =>
        {
            if (newState == AppState.InLobby)
            {
                Title = "RiftVox - Connected to Voice Lobby";
            }
            else if (newState == AppState.InGame)
            {
                Title = "RiftVox - Live Match Detected";
            }
        });
    }

    private async Task HandleMatchStartAsync()
    {
        var parser = new GameConfigParser();
        string configPath = LeaguePathResolver.GetGameCfgPath();

        if (File.Exists(configPath))
        {
            parser.LoadConfig(configPath);
            var (x, y, size) = parser.GetMinimapBounds();
            _captureEngine.UpdateBounds(x, y, size);
        }

        var allPlayers = await _apiClient.GetPlayerListAsync();
        _localPlayerName = await _apiClient.GetActivePlayerNameAsync();

        if (allPlayers == null || string.IsNullOrEmpty(_localPlayerName)) return;

        var localPlayerEntry = allPlayers.FirstOrDefault(p => p.SummonerName == _localPlayerName);
        if (localPlayerEntry == null) return;

        // Filter down to teammates only
        var alliedTeamPlayers = allPlayers.Where(p => p.Team == localPlayerEntry.Team).ToList();

        // Assign properties to the vision capture engine BEFORE it loops
        _captureEngine.LocalPlayerName = _localPlayerName;
        _captureEngine.TrackedPlayers = alliedTeamPlayers;
        _captureEngine.AssetsDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");

        await _captureEngine.StartCaptureLoopAsync(200);
    }

    private void HandleMatchEnd(object? sender, EventArgs e)
    {
        // Shut down the computer vision engine loops so the computer rests between games
        _captureEngine.StopCaptureLoop();

        Dispatcher.InvokeAsync(() =>
        {
            ConfigDisplayTextBox.Text = "🎮 Match concluded.\n\n" +
                                        "🔊 Left Spatial Mode.\n" +
                                        "🎙️ Channel Switched to: [Global Team Lobby Channel]";
        });
    }


    private void Engine_PositionsUpdated(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (_monitorService.CurrentState != AppState.InGame) return;

            var localPlayer = _captureEngine.TrackedPlayers.FirstOrDefault(p => p.SummonerName == _localPlayerName);
            if (localPlayer == null) return;

            var sb = new StringBuilder();
            sb.AppendLine("⚡ RIFTVOX AUTOMATED STATE MACHINE: IN-GAME SPATIAL AUDIO ENGAGED ⚡");
            sb.AppendLine($"Local Player : {localPlayer.ChampionName} (Team: {localPlayer.Team})");
            sb.AppendLine($"My Position  : X={localPlayer.CurrentX}px, Y={localPlayer.CurrentY}px");
            sb.AppendLine("--------------------------------------------------------------------");

            // --- ADD THIS LINE FOR REAL-TIME TROUBLESHOOTING VISIBILITY ---
            sb.AppendLine($"\n[CORE VISION FEEDBACK]:\n{_captureEngine.DiagnosticLog}");
            sb.AppendLine("--------------------------------------------------------------------\n");

            var teammates = _captureEngine.TrackedPlayers.Where(p => p.SummonerName != _localPlayerName);
            // ... (rest of teammate loops stay exactly the same)

            ConfigDisplayTextBox.Text = sb.ToString();
        });
    }
}