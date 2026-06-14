using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RiftVox.Core.Abstractions;
using RiftVox.Core.Models;

namespace RiftVox.Core.Services;

public class VisionCaptureEngine
{
    private readonly IScreenCapturer _screenCapturer;
    private int _x, _y, _size;
    private CancellationTokenSource? _cts;

    // Cache preprocessed template grayscale buffers and dimensions
    private readonly Dictionary<string, (byte[] grayData, int width, int height)> _templateCache = new();

    // Performance profiling
    private readonly PerformanceProfiler _profiler = new();
    private int _profileFrameInterval = 60; // Log stats every N frames

    // Memory and debug tracking
    private readonly MemoryProfiler _memoryProfiler = new();
    private DebugVisualisation? _debugVisualiser;
    private bool _debugMode = false;
    private int _debugSampleInterval = 10; // Capture every Nth frame for debug

    public event EventHandler? PositionsUpdated;

    /// <summary>Diagnostic message property to bubble up internal vision state strings to the UI.</summary>
    public string DiagnosticLog { get; private set; } = "Engine Idle.";

    public List<Player> TrackedPlayers { get; set; } = [];
    public string AssetsDirectoryPath { get; set; } = string.Empty;
    public string? LocalPlayerName { get; set; }
    public ISpatialAudioMixer? AudioMixer { get; set; }
    private readonly ChampionAssetDownloader championAssetDownloader = new(new HttpClient());

    public VisionCaptureEngine(IScreenCapturer screenCapturer)
    {
        _screenCapturer = screenCapturer;
    }

    public void UpdateBounds(int x, int y, int size)
    {
        _x = x;
        _y = y;
        _size = size;
    }

    /// <summary>
    /// Enable debug mode to capture frame visualisations and memory metrics.
    /// </summary>
    public void EnableDebugMode(string? outputDirectory = null, int sampleInterval = 10)
    {
        _debugMode = true;
        _debugSampleInterval = sampleInterval;
        _debugVisualiser = new DebugVisualisation(outputDirectory);
        _memoryProfiler.StartSession();
        Console.WriteLine($"✅ Debug mode enabled. Output: {_debugVisualiser.GetOutputDirectory()}");
    }

    /// <summary>
    /// Disable debug mode and export reports.
    /// </summary>
    public void DisableDebugMode()
    {
        if (!_debugMode) return;

        _debugMode = false;
        _memoryProfiler.TakeSnapshot("SessionEnd");
        _debugVisualiser?.ExportCsvReport();
        _debugVisualiser?.ExportHtmlReport();
        Console.WriteLine(_memoryProfiler.GetMemoryReport());
        if (_debugVisualiser != null)
            Console.WriteLine(_debugVisualiser.GetSummary());
    }

    public async Task StartCaptureLoopAsync(int intervalMilliseconds = 200)
    {
        if (_cts != null && !_cts.IsCancellationRequested) return;

        // Diagnostic Check 1: Verify the bounding box coordinates aren't empty
        if (_x == 0 && _y == 0 && _size == 0)
        {
            DiagnosticLog = "❌ Scan Aborted: Minimap bounds are completely empty (0,0,0). Check game.cfg parsing.";
            PositionsUpdated?.Invoke(this, EventArgs.Empty);
            return;
        }

        _cts = new CancellationTokenSource();
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(intervalMilliseconds));
        _profiler.Reset();

        long frameCounter = 0;

        try
        {
            while (await timer.WaitForNextTickAsync(_cts.Token))
            {
                _profiler.StartFrame();
                _profiler.ResetFrameCounters();

                byte[] rawPixels = _screenCapturer.CaptureRegion(_x, _y, _size);
                if (rawPixels.Length == 0)
                {
                    DiagnosticLog = $"⚠️ Screen Capturer returned 0 bytes for region: X={_x}, Y={_y}, Size={_size}";
                    PositionsUpdated?.Invoke(this, EventArgs.Empty);
                    _profiler.EndFrame();
                    continue;
                }

                int matchCount = 0;
                var debugSummary = new System.Text.StringBuilder();
                debugSummary.AppendLine($"Scanning Area: X={_x}, Y={_y}, Size={_size}px");

                // Assume captured region is BGRA, 4 bytes per pixel
                int sceneWidth = _size;
                int sceneHeight = _size;

                var debugMatches = _debugMode ? new List<DebugVisualisation.MatchResult>() : null;

                foreach (var player in TrackedPlayers)
                {
                    if (player.IsDead) continue;

                    _profiler.StartPlayerMatch();

                    string cleanName = await ChampionAssetDownloader.GetCleanChampionName(player.ChampionName);
                    string rawIconPath = Path.Combine(AssetsDirectoryPath, $"{cleanName}.png");

                    // Diagnostic Check 2: Verify files exist on local disk
                    if (!File.Exists(rawIconPath))
                    {
                        debugSummary.AppendLine($"  ❌ Missing file: {rawIconPath}");
                        await championAssetDownloader.DownloadIconAsync(player.ChampionName, AssetsDirectoryPath);
                        _profiler.EndPlayerMatch(player.SummonerName, false);

                        if (_debugMode && debugMatches != null)
                            debugMatches.Add(new DebugVisualisation.MatchResult 
                            { 
                                PlayerName = player.SummonerName, 
                                X = 0, 
                                Y = 0, 
                                Score = 0, 
                                Matched = false 
                            });
                        continue;
                    }

                    // Get or prepare template (cached for performance)
                    byte[]? processedIconBytes = GetOrPrepareTemplate(cleanName, rawIconPath, 28, 28);
                    if (processedIconBytes == null || processedIconBytes.Length == 0)
                    {
                        debugSummary.AppendLine($"  ⚠️ Failed to process template: {cleanName}");
                        _profiler.EndPlayerMatch(player.SummonerName, false);

                        if (_debugMode && debugMatches != null)
                            debugMatches.Add(new DebugVisualisation.MatchResult 
                            { 
                                PlayerName = player.SummonerName, 
                                X = 0, 
                                Y = 0, 
                                Score = 0, 
                                Matched = false 
                            });
                        continue;
                    }

                    // Fast grayscale SSD matching with temporal coherence and early-exit
                    var location = ChampionIconMatcher.LocateIconInFrame(
                        rawPixels, 
                        processedIconBytes,
                        sceneWidth,
                        sceneHeight,
                        cacheKey: player.SummonerName,
                        similarityThreshold: 0.75);  // Lower threshold prioritises performance

                    bool matched = location != Point.Empty;
                    _profiler.EndPlayerMatch(player.SummonerName, matched);

                    if (matched)
                    {
                        matchCount++;
                        player.CurrentX = location.X;
                        player.CurrentY = location.Y;

                        var localPlayer = TrackedPlayers.FirstOrDefault(p => p.SummonerName == LocalPlayerName);
                        if (localPlayer != null && player.SummonerName != LocalPlayerName)
                        {
                            var transformer = new SpatialAudioTransformer();
                            var (panning, volume) = transformer.CalculateSpatialAudio(
                                localPlayer.CurrentX,
                                localPlayer.CurrentY,
                                player.CurrentX,
                                player.CurrentY
                            );

                            AudioMixer?.UpdateChannelSpatialization(player.SummonerName, panning, volume);
                        }

                        if (_debugMode && debugMatches != null)
                            debugMatches.Add(new DebugVisualisation.MatchResult 
                            { 
                                PlayerName = player.SummonerName, 
                                X = location.X, 
                                Y = location.Y, 
                                Score = 0.95, // Placeholder
                                Matched = true 
                            });
                    }
                    else if (_debugMode && debugMatches != null)
                    {
                        debugMatches.Add(new DebugVisualisation.MatchResult 
                        { 
                            PlayerName = player.SummonerName, 
                            X = 0, 
                            Y = 0, 
                            Score = 0, 
                            Matched = false 
                        });
                    }
                }

                _profiler.EndFrame();
                frameCounter++;

                // Debug visualisation: capture every Nth frame
                if (_debugMode && frameCounter % _debugSampleInterval == 0 && debugMatches != null)
                {
                    _memoryProfiler.TakeSnapshot($"Frame_{frameCounter}");
                    _debugVisualiser?.SaveFrameWithMatches(rawPixels, sceneWidth, sceneHeight, debugMatches);
                }

                // Log profiling stats every N frames
                if (frameCounter % _profileFrameInterval == 0)
                {
                    debugSummary.AppendLine(_profiler.GetDiagnosticSummary());
                    if (_debugMode)
                        debugSummary.AppendLine(_memoryProfiler.GetMemoryReport());
                }

                DiagnosticLog = debugSummary.ToString() + $"\nStatus: Match Run Completed. Successfully locked onto ({matchCount}/{TrackedPlayers.Count}) targets.";
                PositionsUpdated?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (OperationCanceledException)
        {
            DiagnosticLog = "Engine Safely Stopped.\n" + _profiler.GetDiagnosticSummary();
        }
    }

    /// <summary>
    /// Gets cached template or prepares it once and caches for future frames.
    /// This avoids repeated Bitmap allocations and conversions.
    /// </summary>
    private byte[]? GetOrPrepareTemplate(string cacheKey, string filePath, int width, int height)
    {
        if (_templateCache.TryGetValue(cacheKey, out var cached))
        {
            return cached.grayData;
        }

        try
        {
            byte[] templateBgra = PrepareMinimapTemplate(filePath, width, height);
            _templateCache[cacheKey] = (templateBgra, width, height);
            return templateBgra;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Compresses a high-resolution portrait down to match exact target scale requirements.
    /// Returns BGRA byte array (4 bytes per pixel).
    /// </summary>
    private static byte[] PrepareMinimapTemplate(string filePath, int width, int height)
    {
        using var originalImage = Image.FromFile(filePath);
        using var resizedImage = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(resizedImage);

        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;

        graphics.DrawImage(originalImage, 0, 0, width, height);

        // Lock bits to extract raw BGRA data
        var bmpData = resizedImage.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        byte[] bgra = new byte[width * height * 4];
        System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, bgra, 0, bgra.Length);
        resizedImage.UnlockBits(bmpData);

        return bgra;
    }

    public void StopCaptureLoop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _templateCache.Clear();
        ChampionIconMatcher.ClearPositionCache();

        if (_debugMode)
            DisableDebugMode();
    }

    /// <summary>
    /// Get current profiling metrics (useful for UI display).
    /// </summary>
    public string GetProfileMetrics() => _profiler.GetDiagnosticSummary();

    /// <summary>
    /// Set the interval (in frames) for logging profiling statistics.
    /// Default is 60 frames (~12 seconds at 200ms per frame).
    /// </summary>
    public void SetProfileLogInterval(int frameInterval) => _profileFrameInterval = frameInterval;

    /// <summary>
    /// Get memory profiling data (only available if debug mode is enabled).
    /// </summary>
    public string GetMemoryMetrics() => _debugMode ? _memoryProfiler.GetMemoryReport() : "Debug mode not enabled";

    /// <summary>
    /// Get debug visualisation summary.
    /// </summary>
    public string GetDebugSummary() => _debugMode && _debugVisualiser != null 
        ? _debugVisualiser.GetSummary() 
        : "Debug mode not enabled";
}
