using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Xunit;
using RiftVox.Core.Services;

namespace RiftVox.Core.Tests;

/// <summary>
/// Encapsulates the verification execution framework validating the accuracy of portrait template coordinate checks.
/// </summary>
public class ChampionIconMatcherTests
{
    /// <summary>
    /// Validates that the coordinate calculation pipeline successfully isolates a known localized pattern within canvas data matrices.
    /// </summary>
    [Fact]
    public void LocateIconInFrame_ShouldReturnCorrectCoordinates_WhenTemplateIsPresent()
    {
        var matcher = new ChampionIconMatcher();

        // 1. Construct template directly into an in-memory byte array
        byte[] templateBytes;
        using (var templateBmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(templateBmp))
            {
                g.Clear(Color.Red);
            }

            using (var ms = new MemoryStream())
            {
                templateBmp.Save(ms, ImageFormat.Png);
                templateBytes = ms.ToArray();
            }
        }

        // 2. Construct scene canvas directly into an in-memory byte array
        byte[] sceneBytes;
        using (var sceneBmp = new Bitmap(100, 100, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(sceneBmp))
            {
                g.Clear(Color.Blue);
                g.FillRectangle(Brushes.Red, 50, 30, 10, 10);
            }

            using (var ms = new MemoryStream())
            {
                sceneBmp.Save(ms, ImageFormat.Bmp);
                sceneBytes = ms.ToArray();
            }
        }

        // 3. Execute matching runs against our synthesized pixel array matrices
        // PASSED: templateBytes array instead of tempTemplatePath string
        var result = matcher.LocateIconInFrame(sceneBytes, templateBytes, 0.90);

        // 4. Confirm the return frame coordinates correctly isolate the exact pattern center offsets
        Assert.NotNull(result);
        Assert.Equal(55, result.Value.X); // 50 + (10 / 2) equals center point location target 55
        Assert.Equal(35, result.Value.Y); // 30 + (10 / 2) equals center point location target 35
    }
}