using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using RiftVox.Core.Abstractions;

namespace RiftVox.Core.Platforms;

/// <summary>
/// Formulates a Windows-native display capture pipeline utilizing GDI+ graphics engine contexts.
/// </summary>
public class WindowsScreenCapturer : IScreenCapturer
{
    /// <summary>
    /// Synchronously grabs raw pixel streams directly from the Windows desktop subsystem.
    /// </summary>
    /// <param name="x">The absolute horizontal pixel starting coordinate anchor.</param>
    /// <param name="y">The absolute vertical pixel starting coordinate anchor.</param>
    /// <param name="size">The square width and height dimensions of the target capture area.</param>
    /// <returns>A raw byte array containing the uncompressed image byte stream data.</returns>
    public byte[] CaptureRegion(int x, int y, int size)
    {
        if (size <= 0) return Array.Empty<byte>();

        /// <summary>Allocate memory arrays specifically mapped to Windows display configurations.</summary>
        using var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);

        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(x, y, 0, 0, new Size(size, size), CopyPixelOperation.SourceCopy);
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Bmp);
        return stream.ToArray();
    }
}