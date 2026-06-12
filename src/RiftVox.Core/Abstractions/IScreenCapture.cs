using System;

namespace RiftVox.Core.Abstractions;

/// <summary>
/// Defines a platform-agnostic contract for capturing targeted regions of the host display.
/// </summary>
public interface IScreenCapturer
{
    /// <summary>
    /// Grabs a localized square pixel snapshot from the host operating system's graphics subsystem.
    /// </summary>
    /// <param name="x">The absolute horizontal pixel starting coordinate anchor.</param>
    /// <param name="y">The absolute vertical pixel starting coordinate anchor.</param>
    /// <param name="size">The square width and height dimensions of the target capture area.</param>
    /// <returns>A raw byte array containing the uncompressed image pixel matrix data.</returns>
    byte[] CaptureRegion(int x, int y, int size);
}