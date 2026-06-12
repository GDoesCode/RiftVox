using System;
using System.Collections.Generic;
using System.Drawing; // Requires System.Drawing.Common reference
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
    private readonly ChampionIconMatcher _matcher = new();
    private int _x, _y, _size;
    private CancellationTokenSource? _cts;

    public event EventHandler? PositionsUpdated;

    /// <summary>Diagnostic message property to bubble up internal vision state strings to the UI.</summary>
    public string DiagnosticLog { get; private set; } = "Engine Idle.";

    public List<Player> TrackedPlayers { get; set; } = new();
    public string AssetsDirectoryPath { get; set; } = string.Empty;
    public string? LocalPlayerName { get; set; }
    public ISpatialAudioMixer? AudioMixer { get; set; }

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

        try
        {
            while (await timer.WaitForNextTickAsync(_cts.Token))
            {
                byte[] rawPixels = _screenCapturer.CaptureRegion(_x, _y, _size);
                if (rawPixels.Length == 0)
                {
                    DiagnosticLog = $"⚠️ Screen Capturer returned 0 bytes for region: X={_x}, Y={_y}, Size={_size}";
                    PositionsUpdated?.Invoke(this, EventArgs.Empty);
                    continue;
                }

                int matchCount = 0;
                var debugSummary = new System.Text.StringBuilder();
                debugSummary.AppendLine($"Scanning Area: X={_x}, Y={_y}, Size={_size}px");

                foreach (var player in TrackedPlayers)
                {
                    if (player.IsDead) continue;

                    string cleanName = GetRiotAssetFilename(player.ChampionName);
                    string rawIconPath = Path.Combine(AssetsDirectoryPath, $"{cleanName}.png");

                    // Diagnostic Check 2: Verify files exist on local disk
                    if (!File.Exists(rawIconPath))
                    {
                        debugSummary.AppendLine($"  ❌ Missing file: {rawIconPath}.png");
                        continue;
                    }

                    // Dynamic Resizing Step: Create a shrunken processing image to match true minimap sizes (~28x28px)
                    byte[] processedIconBytes = PrepareMinimapTemplate(rawIconPath, 28, 28);

                    var location = _matcher.LocateIconInFrame(rawPixels, processedIconBytes);

                    if (location.HasValue)
                    {
                        matchCount++;
                        player.CurrentX = location.Value.X;
                        player.CurrentY = location.Value.Y;

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
                    }
                }

                DiagnosticLog = debugSummary.ToString() + $"\nStatus: Match Run Completed. Successfully locked onto ({matchCount}/{TrackedPlayers.Count}) targets.";
                PositionsUpdated?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (OperationCanceledException)
        {
            DiagnosticLog = "Engine Safely Stopped.";
        }
    }

    /// <summary>
    /// Compresses a high-resolution portrait down to match exact target scale requirements.
    /// </summary>
    private byte[] PrepareMinimapTemplate(string filePath, int width, int height)
    {
        using var originalImage = Image.FromFile(filePath);
        using var resizedImage = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(resizedImage);

        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;

        graphics.DrawImage(originalImage, 0, 0, width, height);

        using var ms = new MemoryStream();
        resizedImage.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    public void StopCaptureLoop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>
    /// Converts standard Live Client API champion names into official Data Dragon asset filenames.
    /// </summary>
    private string GetRiotAssetFilename(string championName)
    {
        if (string.IsNullOrWhiteSpace(championName)) return "Unknown";

        // Handle Riot's absolute classic internal naming exceptions
        if (championName.Equals("Wukong", StringComparison.OrdinalIgnoreCase)) return "MonkeyKing";
        if (championName.Equals("Nunu & Willump", StringComparison.OrdinalIgnoreCase)) return "Nunu";
        if (championName.Equals("Renata Glasc", StringComparison.OrdinalIgnoreCase)) return "Renata";

        // Strip spaces, apostrophes, and periods for champions like Dr. Mundo, Kai'Sa, Cho'Gath, etc.
        return championName
            .Replace(" ", "")
            .Replace("'", "")
            .Replace(".", "");
    }
}