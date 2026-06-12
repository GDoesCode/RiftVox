using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace RiftVox.Core.Services;

/// <summary>
/// Provides high-performance pixel-matrix template matching to locate champion portraits within a captured image canvas.
/// </summary>
public class ChampionIconMatcher
{
    /// <summary>
    /// Scans a raw bitmap byte stream to find the best matching coordinate location of an in-memory champion icon template.
    /// </summary>
    /// <param name="sceneFrameBytes">The raw uncompressed byte stream array of the captured live minimap frame.</param>
    /// <param name="templateBytes">The in-memory, pre-resized raw byte stream array of the target champion's portrait.</param>
    /// <param name="similarityThreshold">The percentage match baseline value (0.0 to 1.0) required to consider a tracking lock valid.</param>
    /// <returns>A nullable Point structure containing the matched center X and Y pixel positions relative to the frame canvas.</returns>
    public Point? LocateIconInFrame(byte[] sceneFrameBytes, byte[] templateBytes, double similarityThreshold = 0.85)
    {
        // CHANGED: Verify the in-memory template bytes are present instead of checking File.Exists
        if (sceneFrameBytes == null || sceneFrameBytes.Length == 0 || templateBytes == null || templateBytes.Length == 0)
            return null;

        /// <summary>Reconstruct the scene canvas directly out of the memory byte tracking streams.</summary>
        using var sceneMs = new MemoryStream(sceneFrameBytes);
        using var sceneBmp = new Bitmap(sceneMs);

        // CHANGED: Stream the pre-resized template bytes directly out of RAM
        using var templateMs = new MemoryStream(templateBytes);
        using var templateBmp = new Bitmap(templateMs);

        int bestX = -1;
        int bestY = -1;
        double highestMatchScore = 0.0;

        int searchWidth = sceneBmp.Width - templateBmp.Width;
        int searchHeight = sceneBmp.Height - templateBmp.Height;

        /// <summary>Abort execution loops early if the template asset dimensions eclipse the captured display frames.</summary>
        if (searchWidth <= 0 || searchHeight <= 0) return null;

        /// <summary>Coarse scanning iteration block to parse matching pixel structures across the frame bounds.</summary>
        for (int y = 0; y <= searchHeight; y += 2)
        {
            for (int x = 0; x <= searchWidth; x += 2)
            {
                double currentScore = CalculateMatchScore(sceneBmp, templateBmp, x, y);

                if (currentScore > highestMatchScore)
                {
                    highestMatchScore = currentScore;
                    bestX = x;
                    bestY = y;
                }
            }
        }

        /// <summary>Evaluate if our top matching scoring matrix passes the safety threshold parameters.</summary>
        if (highestMatchScore >= similarityThreshold)
        {
            int centerX = bestX + (templateBmp.Width / 2);
            int centerY = bestY + (templateBmp.Height / 2);
            return new Point(centerX, centerY);
        }

        return null;
    }

    /// <summary>
    /// Compares a localized sub-region of the screen canvas directly against the champion icon template pixels.
    /// </summary>
    private double CalculateMatchScore(Bitmap scene, Bitmap template, int startX, int startY)
    {
        long matchingPixels = 0;
        long totalPixels = template.Width * template.Height;

        /// <summary>Sample crucial anchor coordinates across the card dimensions rather than scanning every single pixel.</summary>
        for (int ty = 0; ty < template.Height; ty += 2)
        {
            for (int tx = 0; tx < template.Width; tx += 2)
            {
                Color sceneColor = scene.GetPixel(startX + tx, startY + ty);
                Color templateColor = template.GetPixel(tx, ty);

                /// <summary>Calculate raw delta variance margins across RGB spectrum bounds.</summary>
                int rDiff = Math.Abs(sceneColor.R - templateColor.R);
                int gDiff = Math.Abs(sceneColor.G - templateColor.G);
                int bDiff = Math.Abs(sceneColor.B - templateColor.B);

                /// <summary>Accept pixel validation if the color channel variance stays beneath strict thresholds.</summary>
                if (rDiff < 25 && gDiff < 25 && bDiff < 25)
                {
                    matchingPixels++;
                }
            }
        }

        /// <summary>Re-adjust calculated sample totals based on step skipping counts.</summary>
        double sampleRatio = (double)matchingPixels / (totalPixels / 4.0);
        return sampleRatio;
    }
}