using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Text;
using RiftVox.Core.Services;

namespace RiftVox.Core.Debugging;

/// <summary>
/// Helper class for debugging and validating matcher accuracy during development.
/// Provides utilities to test templates against scenes and analyse results.
/// </summary>
public static class MatchingDebugHelper
{
    /// <summary>
    /// Tests a matcher run and reports detailed diagnostics.
    /// </summary>
    public static void TestMatcherAccuracy(
        string sceneImagePath,
        string templateImagePath,
        int templateWidth = 28,
        int templateHeight = 28)
    {
        Console.WriteLine("🔍 Starting Matcher Accuracy Test...\n");

        try
        {
            // Load images
            using var sceneImg = Image.FromFile(sceneImagePath);
            using var templateImg = Image.FromFile(templateImagePath);

            using var sceneBmp = new Bitmap(sceneImg);
            using var templateBmp = new Bitmap(templateImg, templateWidth, templateHeight);

            // Extract BGRA bytes
            byte[] sceneBytes = BitmapToByteArray(sceneBmp);
            byte[] templateBytes = BitmapToByteArray(templateBmp);

            // Run matcher
            var result = ChampionIconMatcher.LocateIconInFrame(
                sceneBytes,
                templateBytes,
                sceneBmp.Width,
                sceneBmp.Height,
                cacheKey: "test",
                similarityThreshold: 0.75);

            // Report results
            var sb = new StringBuilder();
            sb.AppendLine("═══ MATCHER TEST RESULTS ═══");
            sb.AppendLine($"📸 Scene: {sceneBmp.Width}x{sceneBmp.Height}px");
            sb.AppendLine($"🎯 Template: {templateBmp.Width}x{templateBmp.Height}px");

            if (result != Point.Empty)
            {
                sb.AppendLine($"✅ MATCH FOUND at ({result.X}, {result.Y})");
            }
            else
            {
                sb.AppendLine($"❌ NO MATCH FOUND");
            }

            sb.AppendLine();
            sb.AppendLine("📊 Metrics:");
            sb.AppendLine($"   Memory Used: {GC.GetTotalMemory(false) / 1024}KB");
            sb.AppendLine($"   Search Area: {(sceneBmp.Width - templateBmp.Width) * (sceneBmp.Height - templateBmp.Height)} pixels");

            Console.WriteLine(sb.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs a benchmark of the matcher across multiple iterations.
    /// </summary>
    public static void BenchmarkMatcher(
        string sceneImagePath,
        string templateImagePath,
        int iterations = 100,
        int templateWidth = 28,
        int templateHeight = 28)
    {
        Console.WriteLine($"⏱️  Benchmarking Matcher ({iterations} iterations)...\n");

        try
        {
            using var sceneImg = Image.FromFile(sceneImagePath);
            using var templateImg = Image.FromFile(templateImagePath);

            using var sceneBmp = new Bitmap(sceneImg);
            using var templateBmp = new Bitmap(templateImg, templateWidth, templateHeight);

            byte[] sceneBytes = BitmapToByteArray(sceneBmp);
            byte[] templateBytes = BitmapToByteArray(templateBmp);

            var sw = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                ChampionIconMatcher.LocateIconInFrame(
                    sceneBytes,
                    templateBytes,
                    sceneBmp.Width,
                    sceneBmp.Height,
                    cacheKey: $"bench_{i}");
            }

            sw.Stop();

            double avgMs = (double)sw.ElapsedMilliseconds / iterations;
            double fps = 1000 / avgMs;

            var sb = new StringBuilder();
            sb.AppendLine("═══ BENCHMARK RESULTS ═══");
            sb.AppendLine($"⏱️  Total Time: {sw.ElapsedMilliseconds}ms");
            sb.AppendLine($"⏱️  Average Per Run: {avgMs:F2}ms");
            sb.AppendLine($"🎯 Estimated FPS: {fps:F1}");
            sb.AppendLine($"📊 Throughput: {iterations / (sw.ElapsedMilliseconds / 1000.0):F0} matches/sec");

            Console.WriteLine(sb.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Benchmark failed: {ex.Message}");
        }
    }

    private static byte[] BitmapToByteArray(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return ms.ToArray();
    }
}