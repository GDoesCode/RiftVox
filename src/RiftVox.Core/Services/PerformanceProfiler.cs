using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RiftVox.Core.Services;

/// <summary>
/// Lightweight performance profiler for measuring frame timing and match detection latency.
/// Tracks per-player and aggregate statistics without significant overhead.
/// </summary>
public class PerformanceProfiler
{
    private readonly Stopwatch _frameTimer = new();
    private readonly Stopwatch _matchTimer = new();
    private readonly Dictionary<string, PlayerMatchMetrics> _playerMetrics = new();

    private long _frameCount = 0;
    private long _totalFrameTimeMs = 0;
    private long _totalMatchTimeMs = 0;
    private long _minFrameTimeMs = long.MaxValue;
    private long _maxFrameTimeMs = 0;
    private long _minMatchTimeMs = long.MaxValue;
    private long _maxMatchTimeMs = 0;
    private int _matchesPerFrameCount = 0;

    public struct PlayerMatchMetrics
    {
        public long TotalTimeMs;
        public int MatchCount;
        public long BestTimeMs;
        public long WorstTimeMs;
    }

    /// <summary>Signal the start of a frame capture/processing cycle.</summary>
    public void StartFrame()
    {
        _frameTimer.Restart();
    }

    /// <summary>Signal the end of a frame cycle and record metrics.</summary>
    public void EndFrame()
    {
        _frameTimer.Stop();
        long elapsed = _frameTimer.ElapsedMilliseconds;

        _frameCount++;
        _totalFrameTimeMs += elapsed;
        _minFrameTimeMs = Math.Min(_minFrameTimeMs, elapsed);
        _maxFrameTimeMs = Math.Max(_maxFrameTimeMs, elapsed);
    }

    /// <summary>Signal the start of match detection for a player.</summary>
    public void StartPlayerMatch()
    {
        _matchTimer.Restart();
    }

    /// <summary>Signal the end of match detection and record per-player metrics.</summary>
    public void EndPlayerMatch(string playerName, bool matched)
    {
        _matchTimer.Stop();
        long elapsed = _matchTimer.ElapsedMilliseconds;

        _totalMatchTimeMs += elapsed;
        _minMatchTimeMs = Math.Min(_minMatchTimeMs, elapsed);
        _maxMatchTimeMs = Math.Max(_maxMatchTimeMs, elapsed);
        _matchesPerFrameCount++;

        // Track per-player statistics
        if (_playerMetrics.TryGetValue(playerName, out var metrics))
        {
            metrics.TotalTimeMs += elapsed;
            metrics.MatchCount += matched ? 1 : 0;
            metrics.BestTimeMs = Math.Min(metrics.BestTimeMs, elapsed);
            metrics.WorstTimeMs = Math.Max(metrics.WorstTimeMs, elapsed);
            _playerMetrics[playerName] = metrics;
        }
        else
        {
            _playerMetrics[playerName] = new PlayerMatchMetrics
            {
                TotalTimeMs = elapsed,
                MatchCount = matched ? 1 : 0,
                BestTimeMs = elapsed,
                WorstTimeMs = elapsed
            };
        }
    }

    /// <summary>Reset per-frame counters (call at frame start for per-frame stats).</summary>
    public void ResetFrameCounters()
    {
        _matchesPerFrameCount = 0;
    }

    /// <summary>Get average frame time in milliseconds.</summary>
    public double GetAverageFrameTimeMs() =>
        _frameCount > 0 ? (double)_totalFrameTimeMs / _frameCount : 0;

    /// <summary>Get average match detection time per player in milliseconds.</summary>
    public double GetAverageMatchTimeMs() =>
        _matchesPerFrameCount > 0 ? (double)_totalMatchTimeMs / _matchesPerFrameCount : 0;

    /// <summary>Get estimated frames per second based on average frame time.</summary>
    public int GetEstimatedFps() =>
        GetAverageFrameTimeMs() > 0 ? (int)(1000 / GetAverageFrameTimeMs()) : 0;

    /// <summary>Get diagnostic summary as formatted string.</summary>
    public string GetDiagnosticSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══ PERFORMANCE METRICS ═══");
        sb.AppendLine($"📊 Frame Count: {_frameCount}");
        sb.AppendLine($"⏱️  Avg Frame Time: {GetAverageFrameTimeMs():F2}ms (Min: {_minFrameTimeMs}ms, Max: {_maxFrameTimeMs}ms)");
        sb.AppendLine($"🎯 Est. FPS: {GetEstimatedFps()} fps");
        sb.AppendLine($"🔍 Avg Match Time: {GetAverageMatchTimeMs():F2}ms per player");
        sb.AppendLine($"   Min Match: {_minMatchTimeMs}ms, Max Match: {_maxMatchTimeMs}ms");
        sb.AppendLine();

        if (_playerMetrics.Count > 0)
        {
            sb.AppendLine("📋 Per-Player Statistics:");
            foreach (var kvp in _playerMetrics.OrderByDescending(x => x.Value.TotalTimeMs))
            {
                var metrics = kvp.Value;
                double avgTime = metrics.MatchCount > 0 
                    ? (double)metrics.TotalTimeMs / (metrics.MatchCount > 0 ? metrics.MatchCount : 1)
                    : 0;
                sb.AppendLine($"   {kvp.Key}: {metrics.MatchCount} matches, " +
                    $"Avg {avgTime:F2}ms, Best {metrics.BestTimeMs}ms, Worst {metrics.WorstTimeMs}ms");
            }
        }

        return sb.ToString();
    }

    /// <summary>Reset all profiling data.</summary>
    public void Reset()
    {
        _frameCount = 0;
        _totalFrameTimeMs = 0;
        _totalMatchTimeMs = 0;
        _minFrameTimeMs = long.MaxValue;
        _maxFrameTimeMs = 0;
        _minMatchTimeMs = long.MaxValue;
        _maxMatchTimeMs = 0;
        _matchesPerFrameCount = 0;
        _playerMetrics.Clear();
    }
}
