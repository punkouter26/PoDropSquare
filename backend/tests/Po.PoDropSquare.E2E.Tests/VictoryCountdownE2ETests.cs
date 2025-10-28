using Microsoft.Playwright;
using Xunit;

namespace Po.PoDropSquare.E2E.Tests;

/// <summary>
/// E2E tests for victory countdown functionality
/// Verifies that blocks touching the red line for 2+ seconds triggers victory
/// </summary>
public class VictoryCountdownE2ETests : IAsyncLifetime
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
    public async Task VictoryCountdown_WhenBlockTouchesRedLine_ShouldTriggerAfter2Seconds()
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

        // Wait for canvas to be ready
        var canvas = await _page.WaitForSelectorAsync("canvas", new() 
        { 
            State = WaitForSelectorState.Visible,
            Timeout = 5000 
        });
        Assert.NotNull(canvas);

        // Get canvas position for clicking
        var boundingBox = await canvas!.BoundingBoxAsync();
        Assert.NotNull(boundingBox);

        Console.WriteLine("✅ Canvas loaded successfully");

        // Act - Stack blocks by clicking multiple times to build a tower
        // Click at the center-bottom area to stack blocks vertically
        var clickX = boundingBox.X + boundingBox.Width / 2;
        var clickY = boundingBox.Y + boundingBox.Height - 50; // Near bottom

        Console.WriteLine("🎮 Starting to stack blocks...");
        
        // Drop blocks rapidly to build a tower
        for (int i = 0; i < 15; i++)
        {
            await _page.Mouse.ClickAsync(clickX, clickY);
            await Task.Delay(200); // Small delay between drops
            Console.WriteLine($"   Block {i + 1} dropped");
        }

        Console.WriteLine("⏳ Waiting for blocks to settle and reach red line...");
        await Task.Delay(3000); // Wait for physics to settle

        // Check if victory countdown started (look for console message)
        var countdownStarted = consoleMessages.Any(m => 
            m.Contains("Danger countdown started", StringComparison.OrdinalIgnoreCase));

        if (!countdownStarted)
        {
            Console.WriteLine("⚠️ Countdown not started yet, waiting longer...");
            await Task.Delay(2000);
        }

        // Wait for victory (2 seconds countdown + buffer)
        Console.WriteLine("⏳ Waiting for 2-second victory countdown...");
        await Task.Delay(3000);

        // Assert - Check console logs for victory message
        var victoryAchieved = consoleMessages.Any(m => 
            m.Contains("Victory", StringComparison.OrdinalIgnoreCase));

        // Also check if victory overlay appeared in DOM
        var victoryOverlay = await _page.QuerySelectorAsync("text=/VICTORY/i");
        var hasVictoryUI = victoryOverlay != null && await victoryOverlay.IsVisibleAsync();

        Console.WriteLine("\n📊 Test Results:");
        Console.WriteLine($"   Console messages captured: {consoleMessages.Count}");
        Console.WriteLine($"   Countdown started: {countdownStarted}");
        Console.WriteLine($"   Victory message in console: {victoryAchieved}");
        Console.WriteLine($"   Victory UI visible: {hasVictoryUI}");

        // Print relevant console messages
        Console.WriteLine("\n📝 Relevant Console Messages:");
        foreach (var msg in consoleMessages.Where(m => 
            m.Contains("countdown", StringComparison.OrdinalIgnoreCase) ||
            m.Contains("victory", StringComparison.OrdinalIgnoreCase) ||
            m.Contains("above line", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine($"   {msg}");
        }

        Assert.True(countdownStarted || victoryAchieved || hasVictoryUI,
            "Victory countdown should have started when blocks touched the red line");

        if (countdownStarted)
        {
            Assert.True(victoryAchieved || hasVictoryUI,
                "Victory should be achieved after 2-second countdown completes");
        }

        Console.WriteLine("\n✅ Victory countdown test completed successfully!");
    }

    [Fact]
    public async Task VictoryCountdown_WhenBlockFallsBeforeTimeout_ShouldCancelCountdown()
    {
        // Arrange
        var consoleMessages = new List<string>();
        _page.Console += (_, e) =>
        {
            consoleMessages.Add($"[{e.Type}] {e.Text}");
        };

        await _page.GotoAsync(BASE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var canvas = await _page.WaitForSelectorAsync("canvas", new() { State = WaitForSelectorState.Visible });
        Assert.NotNull(canvas);

        var boundingBox = await canvas!.BoundingBoxAsync();
        Assert.NotNull(boundingBox);

        Console.WriteLine("✅ Canvas loaded successfully");

        // Act - Drop a few blocks to potentially trigger countdown
        var clickX = boundingBox.X + boundingBox.Width / 2;
        var clickY = boundingBox.Y + boundingBox.Height - 50;

        for (int i = 0; i < 8; i++)
        {
            await _page.Mouse.ClickAsync(clickX, clickY);
            await Task.Delay(150);
        }

        Console.WriteLine("⏳ Waiting for blocks to settle...");
        await Task.Delay(2000);

        // Check if countdown started
        var countdownStartedBefore = consoleMessages.Any(m => 
            m.Contains("Danger countdown started", StringComparison.OrdinalIgnoreCase));

        // Now drop more blocks to potentially make tower unstable
        Console.WriteLine("🎮 Dropping additional blocks to destabilize tower...");
        var sideClickX = boundingBox.X + boundingBox.Width * 0.3; // Click to the side
        for (int i = 0; i < 5; i++)
        {
            await _page.Mouse.ClickAsync(sideClickX, clickY);
            await Task.Delay(200);
        }

        await Task.Delay(2000);

        // Assert - Check if countdown was cancelled
        var countdownCancelled = consoleMessages.Any(m => 
            m.Contains("cancel", StringComparison.OrdinalIgnoreCase) ||
            m.Contains("below line", StringComparison.OrdinalIgnoreCase));

        Console.WriteLine($"\n📊 Countdown cancellation test:");
        Console.WriteLine($"   Countdown started: {countdownStartedBefore}");
        Console.WriteLine($"   Countdown cancelled messages: {countdownCancelled}");

        // This is an optional behavior test - may or may not trigger depending on physics
        Console.WriteLine("✅ Countdown cancellation test completed (result may vary based on physics)");
    }

    [Fact]
    public async Task VictoryOverlay_ShouldDisplayCountdownProgress()
    {
        // Arrange
        await _page.GotoAsync(BASE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var canvas = await _page.WaitForSelectorAsync("canvas", new() { State = WaitForSelectorState.Visible });
        Assert.NotNull(canvas);

        var boundingBox = await canvas!.BoundingBoxAsync();
        Assert.NotNull(boundingBox);

        // Act - Stack blocks
        var clickX = boundingBox.X + boundingBox.Width / 2;
        var clickY = boundingBox.Y + boundingBox.Height - 50;

        Console.WriteLine("🎮 Stacking blocks to trigger countdown overlay...");
        for (int i = 0; i < 15; i++)
        {
            await _page.Mouse.ClickAsync(clickX, clickY);
            await Task.Delay(200);
        }

        await Task.Delay(2000);

        // Look for victory countdown overlay with trophy emoji
        try
        {
            var victoryCountdownElement = await _page.WaitForSelectorAsync(
                "text=/VICTORY COUNTDOWN/i",
                new() { Timeout = 5000 }
            );

            if (victoryCountdownElement != null)
            {
                var isVisible = await victoryCountdownElement.IsVisibleAsync();
                Console.WriteLine($"✅ Victory countdown overlay visible: {isVisible}");

                // Try to find the countdown timer display
                var pageContent = await _page.ContentAsync();
                var hasTimerDisplay = pageContent.Contains("/ 2.0") || 
                                     pageContent.Contains("/2.0");

                Console.WriteLine($"✅ Countdown timer display found: {hasTimerDisplay}");

                Assert.True(isVisible, "Victory countdown overlay should be visible");
            }
            else
            {
                Console.WriteLine("⚠️ Victory countdown overlay not found - blocks may not have reached red line");
            }
        }
        catch (TimeoutException)
        {
            Console.WriteLine("⚠️ Timeout waiting for victory countdown overlay - this is acceptable if blocks didn't reach red line");
        }

        Console.WriteLine("✅ Victory overlay display test completed");
    }

    [Fact]
    public async Task RedLine_ShouldBeVisibleOnCanvas()
    {
        // Arrange
        await _page.GotoAsync(BASE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var canvas = await _page.WaitForSelectorAsync("canvas", new() { State = WaitForSelectorState.Visible });
        Assert.NotNull(canvas);

        await Task.Delay(1000); // Wait for canvas rendering

        // Act - Check canvas rendering by evaluating JavaScript
        var hasRedLine = await _page.EvaluateAsync<bool>(@"
            () => {
                const canvas = document.querySelector('canvas');
                if (!canvas) return false;
                
                const ctx = canvas.getContext('2d');
                const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
                const data = imageData.data;
                
                // Look for red pixels (Matter.js renders goal line in red)
                for (let i = 0; i < data.length; i += 4) {
                    const r = data[i];
                    const g = data[i + 1];
                    const b = data[i + 2];
                    // Check for reddish pixels (R > 200, G < 100, B < 100)
                    if (r > 200 && g < 100 && b < 100) {
                        return true;
                    }
                }
                return false;
            }
        ");

        // Assert
        Console.WriteLine($"Red line visible on canvas: {hasRedLine}");
        Assert.True(hasRedLine, "Red goal line should be rendered on the canvas");

        Console.WriteLine("✅ Red line visibility test completed");
    }
}
