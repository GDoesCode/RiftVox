using System;
using System.Threading;
using System.Threading.Tasks;
using RiftVox.Core.Models;

namespace RiftVox.Core.Services;

public class MatchMonitorService
{
    private readonly RiotApiClient _apiClient;
    private AppState _currentState = AppState.CheckingClient;
    private CancellationTokenSource? _cts;

    /// <summary> Events to notify the UI and Voice engine when transitions happen. </summary>
    public event EventHandler<AppState>? StateChanged;
    public event EventHandler? MatchStarted;
    public event EventHandler? MatchEnded;

    public AppState CurrentState => _currentState;

    public MatchMonitorService(RiotApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public void StartMonitoring()
    {
        _cts = new CancellationTokenSource();
        Task.Run(() => MonitorLoopAsync(_cts.Token));
    }

    public void StopMonitoring()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private async Task MonitorLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            // Check if the local Riot Live Client API is responding
            var playerList = await _apiClient.GetPlayerListAsync();
            bool isGameActive = playerList != null && playerList.Count > 0;

            if (isGameActive && _currentState != AppState.InGame)
            {
                // Transition: Lobby -> In-Game
                ChangeState(AppState.InGame);
                MatchStarted?.Invoke(this, EventArgs.Empty);
            }
            else if (!isGameActive && _currentState == AppState.InGame)
            {
                // Transition: In-Game -> Lobby
                ChangeState(AppState.InLobby);
                MatchEnded?.Invoke(this, EventArgs.Empty);
            }
            else if (!isGameActive && _currentState == AppState.CheckingClient)
            {
                // Initial baseline state when starting the app cold
                ChangeState(AppState.InLobby);
            }

            // Slow pull interval: 2 seconds is perfect to prevent CPU overhead 
            // while feeling instantaneous when loading screens hit
            await Task.Delay(2000, token);
        }
    }

    private void ChangeState(AppState newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;
        StateChanged?.Invoke(this, _currentState);
    }
}