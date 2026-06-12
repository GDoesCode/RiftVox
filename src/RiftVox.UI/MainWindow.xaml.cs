using System.Windows;
using RiftVox.Core;

namespace RiftVox.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Runs on startup
        TestConfigParser();
    }

    private void TestConfigParser()
    {
        var parser = new GameConfigParser();

        // Point this to your configuration path
        string mockPath = @"C:\Riot Games\League of Legends\Config\game.cfg";
        parser.LoadConfig(mockPath);

        var (x, y, size) = parser.GetMinimapBounds();

        string uiOutput = $"--- RIFTVOX CALIBRATION METRICS ---\n\n" +
                          $"Detected Game Resolution : {parser.Width}x{parser.Height}\n" +
                          $"Detected Minimap Scale   : {parser.MinimapScale}\n" +
                          $"Minimap Side Alignment   : {(parser.MinimapOnLeft ? "LEFT" : "RIGHT")}\n\n" +
                          $"Target Capture Window    : X={x}, Y={y} ({size}x{size}px)";

        // Send it to UI text box
        ConfigDisplayTextBox.Text = uiOutput;
    }
}