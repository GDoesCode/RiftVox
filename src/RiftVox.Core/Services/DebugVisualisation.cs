using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace RiftVox.Core.Services;

/// <summary>
/// Provides debug visualisation capabilities for template matching results.
/// Saves annotated frames with detected matches for manual validation and accuracy testing.
/// </summary>
public class DebugVisualisation
{
    private readonly string _outputDirectory;
    private int _frameCounter = 0;
    private readonly List<DebugFrame> _capturedFrames = new();

    public struct MatchResult
    {
        public string PlayerName;
        public int X;
        public int Y;
        public double Score;
        public bool Matched;
    }

    private struct DebugFrame
    {
        public int FrameNumber;
        public string Timestamp;
        public List<MatchResult> Matches;
        public string FramePath;
    }

    public DebugVisualisation(string? outputDirectory = null)
    {
        _outputDirectory = outputDirectory ?? Path.Combine(
            Path.GetTempPath(),
            "RiftVox",
            "Debug_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));

        Directory.CreateDirectory(_outputDirectory);
    }

    /// <summary>
    /// Saves a visualisation of the scene with match markers.
    /// Converts raw BGRA bytes to Bitmap, draws match locations, and saves to disk.
    /// </summary>
    public void SaveFrameWithMatches(
        byte[] sceneFrameBgra,
        int width,
        int height,
        List<DebugVisualisation.MatchResult> matches)
    {
        try
        {
            // Convert BGRA to Bitmap
            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bmpData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            System.Runtime.InteropServices.Marshal.Copy(sceneFrameBgra, 0, bmpData.Scan0, sceneFrameBgra.Length);
            bitmap.UnlockBits(bmpData);

            // Draw match annotations
            using var graphics = Graphics.FromImage(bitmap);
            using var greenPen = new Pen(Color.Lime, 2);
            using var redPen = new Pen(Color.Red, 2);
            using var font = new Font(FontFamily.GenericSansSerif, 8);
            using var brush = new SolidBrush(Color.Lime);

            foreach (var match in matches)
            {
                if (match.Matched)
                {
                    // Draw circle around match
                    int radius = 15;
                    graphics.DrawEllipse(greenPen, match.X - radius, match.Y - radius, radius * 2, radius * 2);

                    // Draw text label
                    var textSize = graphics.MeasureString(match.PlayerName, font);
                    graphics.DrawString(
                        match.PlayerName,
                        font,
                        brush,
                        match.X - textSize.Width / 2,
                        match.Y - 25);

                    // Draw score
                    string scoreText = $"{match.Score:F2}";
                    graphics.DrawString(
                        scoreText,
                        font,
                        brush,
                        match.X - graphics.MeasureString(scoreText, font).Width / 2,
                        match.Y + 20);
                }
            }

            // Save frame
            string framePath = Path.Combine(_outputDirectory, $"frame_{_frameCounter:D5}.png");
            bitmap.Save(framePath, ImageFormat.Png);

            _capturedFrames.Add(new DebugFrame
            {
                FrameNumber = _frameCounter,
                Timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
                Matches = new List<MatchResult>(matches),
                FramePath = framePath
            });

            _frameCounter++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to save debug frame: {ex.Message}");
        }
    }

    /// <summary>
    /// Exports a CSV report of all captured frames and match results for analysis.
    /// </summary>
    public void ExportCsvReport()
    {
        try
        {
            string csvPath = Path.Combine(_outputDirectory, "matches_report.csv");
            using var writer = new StreamWriter(csvPath);

            writer.WriteLine("Frame,Timestamp,PlayerName,X,Y,Score,Matched");

            foreach (var frame in _capturedFrames)
            {
                foreach (var match in frame.Matches)
                {
                    writer.WriteLine($"{frame.FrameNumber},{frame.Timestamp},{match.PlayerName}," +
                        $"{match.X},{match.Y},{match.Score},{match.Matched}");
                }
            }

            Console.WriteLine($"✅ CSV Report exported to: {csvPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to export CSV: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates an HTML report with embedded frame images for easy review.
    /// </summary>
    public void ExportHtmlReport()
    {
        try
        {
            string htmlPath = Path.Combine(_outputDirectory, "matches_report.html");
            using var writer = new StreamWriter(htmlPath);

            writer.WriteLine("<!DOCTYPE html>");
            writer.WriteLine("<html>");
            writer.WriteLine("<head>");
            writer.WriteLine("<title>RiftVox Debug Report</title>");
            writer.WriteLine("<style>");
            writer.WriteLine("body { font-family: Arial, sans-serif; margin: 20px; background: #1e1e1e; color: #ddd; }");
            writer.WriteLine("h1 { color: #00ff00; }");
            writer.WriteLine(".frame { margin: 20px 0; border: 1px solid #444; padding: 10px; }");
            writer.WriteLine(".frame h3 { color: #00ff00; margin: 0; }");
            writer.WriteLine(".frame img { max-width: 800px; border: 1px solid #666; margin-top: 10px; }");
            writer.WriteLine(".matches { background: #252525; padding: 10px; margin-top: 10px; }");
            writer.WriteLine(".match { padding: 5px; margin: 5px 0; }");
            writer.WriteLine(".matched { background: #1a3a1a; }");
            writer.WriteLine(".missed { background: #3a1a1a; }");
            writer.WriteLine("</style>");
            writer.WriteLine("</head>");
            writer.WriteLine("<body>");
            writer.WriteLine($"<h1>🎮 RiftVox Debug Report - {DateTime.Now:g}</h1>");
            writer.WriteLine($"<p>Total Frames Captured: {_capturedFrames.Count}</p>");

            foreach (var frame in _capturedFrames)
            {
                writer.WriteLine("<div class='frame'>");
                writer.WriteLine($"<h3>Frame #{frame.FrameNumber} - {frame.Timestamp}</h3>");

                string relativeImagePath = Path.GetFileName(frame.FramePath);
                writer.WriteLine($"<img src='{relativeImagePath}' />");

                writer.WriteLine("<div class='matches'>");
                foreach (var match in frame.Matches)
                {
                    string matchClass = match.Matched ? "matched" : "missed";
                    string symbol = match.Matched ? "✅" : "❌";
                    writer.WriteLine($"<div class='match {matchClass}'>" +
                        $"{symbol} {match.PlayerName}: ({match.X}, {match.Y}) Score: {match.Score:F3}</div>");
                }
                writer.WriteLine("</div>");
                writer.WriteLine("</div>");
            }

            writer.WriteLine("</body>");
            writer.WriteLine("</html>");

            Console.WriteLine($"✅ HTML Report exported to: {htmlPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to export HTML: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the output directory path where debug files are saved.
    /// </summary>
    public string GetOutputDirectory() => _outputDirectory;

    /// <summary>
    /// Gets the number of frames captured so far.
    /// </summary>
    public int GetFrameCount() => _frameCounter;

    /// <summary>
    /// Clears captured frame data (but keeps on-disk files).
    /// </summary>
    public void ClearMemory()
    {
        _capturedFrames.Clear();
    }

    /// <summary>
    /// Gets summary statistics about captured matches.
    /// </summary>
    public string GetSummary()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("═══ DEBUG VISUALISATION SUMMARY ═══");
        sb.AppendLine($"📂 Output Directory: {_outputDirectory}");
        sb.AppendLine($"📸 Frames Captured: {_frameCounter}");

        int totalMatches = _capturedFrames.Sum(f => f.Matches.Count(m => m.Matched));
        int totalDetections = _capturedFrames.Sum(f => f.Matches.Count);

        sb.AppendLine($"✅ Successful Matches: {totalMatches}");
        sb.AppendLine($"🔍 Total Detections Attempted: {totalDetections}");

        if (totalDetections > 0)
        {
            double accuracy = (double)totalMatches / totalDetections * 100;
            sb.AppendLine($"📊 Match Accuracy: {accuracy:F1}%");
        }

        return sb.ToString();
    }
}