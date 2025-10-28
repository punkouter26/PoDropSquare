using Microsoft.Playwright;
using Xunit;

namespace Po.PoDropSquare.E2E.Tests;

/// <summary>
/// Simplified victory test - directly manipulates block position to test victory logic
/// </summary>
public class SimplifiedVictoryTest : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;
    private const string BASE_URL = "http://localhost:5000";

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        _page = await _browser.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    [Fact]
    public async Task Victory_WhenBlockManuallyPlacedAboveRedLine_ShouldTrigger()
    {
        // Arrange
        var consoleMessages = new List<string>();
        _page.Console += (_, e) =>
        {
            consoleMessages.Add($"[{e.Type}] {e.Text}");
            Console.WriteLine($"Browser Console: [{e.Type}] {e.Text}");
        };

        await _page.GotoAsync(BASE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var canvas = await _page.WaitForSelectorAsync("canvas", new() 
        { 
            State = WaitForSelectorState.Visible,
            Timeout = 5000 
        });
        Assert.NotNull(canvas);

        var boundingBox = await canvas!.BoundingBoxAsync();
        Assert.NotNull(boundingBox);

        Console.WriteLine("✅ Canvas loaded successfully");

        // Drop one block first
        var clickX = boundingBox.X + boundingBox.Width / 2;
        var clickY = boundingBox.Y + boundingBox.Height - 50;
        
        await _page.Mouse.ClickAsync(clickX, clickY);
        Console.WriteLine("🎮 Block dropped, waiting for it to be added to physics engine...");
        await Task.Delay(3000); // Wait longer for block to be fully created and settled

        // Check if block exists before trying to move it
        var blockExists = await _page.EvaluateAsync<bool>(@"
            () => typeof window.gameBlocks !== 'undefined' && window.gameBlocks.length > 0
        ");
        
        Console.WriteLine($"Block exists check: {blockExists}");
        if (!blockExists)
        {
            Console.WriteLine("❌ No blocks in physics engine - cannot proceed with test");
            Assert.Fail("Block should exist in physics engine after clicking");
        }

        Console.WriteLine("✅ Block exists, now moving it above red line using JavaScript...");

        // Use JavaScript to move the block above the red line (Y=30 is above Y=40 goal line)
        var moveResult = await _page.EvaluateAsync<object>(@"
            () => {
                try {
                    // Access the physics engine's gameBlocks array
                    if (typeof window.gameBlocks !== 'undefined' && window.gameBlocks.length > 0) {
                        const block = window.gameBlocks[0];
                        const oldY = block.position.y;
                        // Set block position to Y=30 (above goal line at Y=40)
                        Matter.Body.setPosition(block, { x: block.position.x, y: 30 });
                        const newY = block.position.y;
                        console.log(`Block moved from Y=${oldY} to Y=${newY} (goal line at Y=40)`);
                        return { success: true, oldY, newY };
                    }
                    console.error('No blocks found in gameBlocks array');
                    return { success: false, error: 'No blocks found' };
                } catch (error) {
                    console.error('Failed to move block:', error);
                    return { success: false, error: error.toString() };
                }
            }
        ");

        Console.WriteLine($"Move result: {moveResult}");
        Assert.NotNull(moveResult);
        
        // Parse result
        var resultDict = moveResult as System.Collections.Generic.IDictionary<string, object>;
        var success = resultDict != null && resultDict.ContainsKey("success") && (bool)resultDict["success"];
        Assert.True(success, "Should have successfully moved block above red line");

        Console.WriteLine("⏳ Waiting for 3+ seconds for victory countdown to complete...");
        await Task.Delay(4000);

        // Check for victory messages
        var victoryInConsole = consoleMessages.Any(m => 
            m.Contains("Victory!", StringComparison.OrdinalIgnoreCase));
        
        var dangerCountdownStarted = consoleMessages.Any(m => 
            m.Contains("Danger countdown started", StringComparison.OrdinalIgnoreCase));

        Console.WriteLine($"\n📊 Test Results:");
        Console.WriteLine($"   Danger countdown started: {dangerCountdownStarted}");
        Console.WriteLine($"   Victory message in console: {victoryInConsole}");

        // Print relevant console messages
        Console.WriteLine("\n📝 Victory-related Console Messages:");
        foreach (var msg in consoleMessages.Where(m => 
            m.Contains("Victory", StringComparison.OrdinalIgnoreCase) ||
            m.Contains("Danger", StringComparison.OrdinalIgnoreCase) ||
            m.Contains("countdown", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine($"   {msg}");
        }

        Assert.True(dangerCountdownStarted, "Danger countdown should have started when block was moved above red line");
        Assert.True(victoryInConsole, "Victory should have been achieved after 2-second countdown");

        Console.WriteLine("\n✅ Simplified victory test completed successfully!");
    }
}
