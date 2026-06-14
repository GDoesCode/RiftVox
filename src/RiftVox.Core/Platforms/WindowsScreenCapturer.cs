using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using RiftVox.Core.Abstractions;

namespace RiftVox.Core.Platforms;

/// <summary>
/// Formulates a Windows-native display capture pipeline targeting specific application windows.
/// </summary>
public class WindowsScreenCapturer : IScreenCapturer
{
    // Win32 API Imports for background window capture
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    // PW_RENDERFULLCONTENT flag ensures modern hardware-accelerated windows render correctly
    private const uint PW_RENDERFULLCONTENT = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    /// <summary>
    /// Synchronously grabs raw pixel streams directly from the League of Legends window subsystem.
    /// </summary>
    public byte[] CaptureRegion(int x, int y, int size)
    {
        if (size <= 0) return [];

        // 1. Locate the League of Legends process window
        // Note: The actual game client process is usually "League of Legends" (not the Riot Client)
        Process[] processes = Process.GetProcessesByName("League of Legends");
        if (processes.Length == 0)
        {
            // Fallback or log that the game isn't running
            return [];
        }

        IntPtr hWnd = processes[0].MainWindowHandle;
        if (hWnd == IntPtr.Zero) return [];

        // 2. Determine game window boundaries
        if (!GetWindowRect(hWnd, out RECT rect)) return [];

        int winWidth = rect.Right - rect.Left;
        int winHeight = rect.Bottom - rect.Top;

        // 3. Create a bitmap matching the actual window size to render into
        using var fullWindowBitmap = new Bitmap(winWidth, winHeight, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(fullWindowBitmap))
        {
            IntPtr hdc = graphics.GetHdc();
            try
            {
                // PrintWindow captures the window even if it is obscured/behind other layers
                PrintWindow(hWnd, hdc, PW_RENDERFULLCONTENT);
            }
            finally
            {
                graphics.ReleaseHdc(hdc);
            }
        }

        // 4. Crop the specific region (x, y, size) out of the full window capture
        // x and y are now relative coordinates inside the game window, rather than absolute screen coordinates
        using var croppedBitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var croppedGraphics = Graphics.FromImage(croppedBitmap))
        {
            croppedGraphics.DrawImage(fullWindowBitmap,
                new Rectangle(0, 0, size, size),
                new Rectangle(x, y, size, size),
                GraphicsUnit.Pixel);
        }

        // 5. Serialize and return stream
        using var stream = new MemoryStream();
        croppedBitmap.Save(stream, ImageFormat.Bmp);

        return stream.ToArray();
    }
}