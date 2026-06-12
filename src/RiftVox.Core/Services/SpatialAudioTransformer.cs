using System;

namespace RiftVox.Core.Services;

/// <summary>
/// Computes trigonometric relative matrix vectors to convert raw 2D pixel coordinates 
/// into physical stereo panning and volume attenuation parameters.
/// </summary>
public class SpatialAudioTransformer
{
    /// <summary>The maximum distance in pixels on the minimap canvas where proximity voice remains audible.</summary>
    private const double MaxAudioRadius = 120.0;

    /// <summary>
    /// Calculates panning and volume configurations for a target player relative to the local user's position.
    /// </summary>
    /// <param name="localX">The current horizontal pixel anchor coordinate of the local app user.</param>
    /// <param name="localY">The current vertical pixel anchor coordinate of the local app user.</param>
    /// <param name="targetX">The current horizontal pixel anchor coordinate of the tracked teammate.</param>
    /// <param name="targetY">The current vertical pixel anchor coordinate of the tracked teammate.</param>
    /// <returns>A tuple containing the calculated Panning (-1.0 to 1.0) and Volume (0.0 to 1.0) levels.</returns>
    public (double Panning, double Volume) CalculateSpatialAudio(int localX, int localY, int targetX, int targetY)
    {
        double deltaX = targetX - localX;

        /// <summary>Invert Y axis because screen pixels increase downward, but audio spatial coordinates increase upward.</summary>
        double deltaY = localY - targetY;

        double distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));

        /// <summary>A teammate exactly on top of the user shouldn't blow out speakers; apply a minimal safety threshold.</summary>
        if (distance < 5.0)
        {
            return (0.0, 1.0);
        }

        /// <summary>Calculate stereo panning based on horizontal aspect ratio variance clamped between -1.0 and 1.0.</summary>
        double panning = deltaX / MaxAudioRadius;
        panning = Math.Clamp(panning, -1.0, 1.0);

        /// <summary>Apply a linear inverse distance falloff multiplier for volume tracking calculations.</summary>
        double volume = 1.0 - (distance / MaxAudioRadius);
        volume = Math.Clamp(volume, 0.0, 1.0);

        return (panning, volume);
    }
}