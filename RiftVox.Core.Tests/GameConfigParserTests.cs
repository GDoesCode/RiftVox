using RiftVox.Core.Services;

namespace RiftVox.Core.Tests;

public class GameConfigParserTests : IDisposable
{
    private readonly string _tempFilePath;

    public GameConfigParserTests()
    {
        // Setup: Create a unique temporary file path for each test execution
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_game.cfg");
    }

    public void Dispose()
    {
        // Cleanup: Tear down the temporary file after the test finishes
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Fact]
    public void LoadConfig_ValidStandardConfig_PopulatesPropertiesCorrectly()
    {
        // Arrange: Write a fake, standard right-aligned configuration file
        string mockConfigContent = @"
            [Performance]
            Width=1920
            Height=1080
            [HUD]
            MinimapScale=1.25
            FlipMinimap=0";

        File.WriteAllText(_tempFilePath, mockConfigContent);
        var parser = new GameConfigParser();

        // Act: Run the parsing engine
        parser.LoadConfig(_tempFilePath);

        // Assert: Verify the numbers extracted match exactly what was in the file
        Assert.Equal(1920, parser.Width);
        Assert.Equal(1080, parser.Height);
        Assert.Equal(1.25, parser.MinimapScale);
        Assert.False(parser.MinimapOnLeft);
    }

    [Fact]
    public void GetMinimapBounds_FlippedMinimap_AnchorsToFarLeftEdge()
    {
        // Arrange: Write a configuration mimicking a player who flipped their map
        string mockConfigContent = @"
            Width=1920
            Height=1080
            MinimapScale=1.0
            FlipMinimap=1";

        File.WriteAllText(_tempFilePath, mockConfigContent);
        var parser = new GameConfigParser();
        parser.LoadConfig(_tempFilePath);

        // Act: Calculate our screen coordinates
        var (x, y, size) = parser.GetMinimapBounds();

        // Assert: If flipped, X coordinate must immediately drop to 0 (far left wall)
        Assert.Equal(0, x);
        // At 1080p scale 1.0, base size is 272. Y anchor should be 1080 - 272 = 808
        Assert.Equal(870, y);
        Assert.Equal(210, size);
    }
}