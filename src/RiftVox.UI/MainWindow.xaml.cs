using System.Windows;

// 1. Tell the UI layer to look inside your Core project
using RiftVox.Core;

namespace RiftVox.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 2. Instantiate and test your new config parser on startup!
        TestConfigParser();
    }

    private void TestConfigParser()
    {
        var parser = new GameConfigParser();

        // 1. Point this to your configuration path
        string mockPath = @"C:\Riot Games\League of Legends\Config\game.cfg";
        parser.LoadConfig(mockPath);

        // 2. CALL THE METHOD HERE (This was the missing piece!)
        var (x, y, size) = parser.GetMinimapBounds();

        // 3. Now 'bounds' exists, so your string interpolation works flawlessly
        string uiOutput = $"--- RIFTVOX CALIBRATION METRICS ---\n\n" +
                          $"Detected Game Resolution : {parser.Width}x{parser.Height}\n" +
                          $"Detected Minimap Scale   : {parser.MinimapScale}\n" +
                          $"Minimap Side Alignment   : {(parser.MinimapOnLeft ? "LEFT" : "RIGHT")}\n\n" +
                          $"Target Capture Window    : X={x}, Y={y} ({size}x{size}px)";

        // 4. Send it to your UI text box
        ConfigDisplayTextBox.Text = uiOutput;
    }
}