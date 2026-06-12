namespace RiftVox.Core.Abstractions;

/// <summary>
/// Defines a platform-agnostic contract for manipulating live audio channel properties 
/// based on relative spatial coordinate calculations.
/// </summary>
public interface ISpatialAudioMixer
{
    /// <summary>
    /// Updates the hardware audio positioning matrix for a specific voice participant.
    /// </summary>
    /// <param name="playerId">The unique network identifier token assigned to the player tracking channel.</param>
    /// <param name="panning">The stereo balance shift parameter ranging from -1.0 (Full Left) to 1.0 (Full Right).</param>
    /// <param name="volume">The channel gain multiplier ranging from 0.0 (Completely Muted) to 1.0 (Maximum Volume).</param>
    void UpdateChannelSpatialization(string playerId, double panning, double volume);
}