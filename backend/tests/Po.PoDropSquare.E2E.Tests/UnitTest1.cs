using Microsoft.Playwright;

namespace Po.PoDropSquare.E2E.Tests;

public class PoDropSquareE2ETests : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false // Set to true for headless mode
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
    public async Task BrowseApplication_HomePageLoads()
    {
        // Navigate to the home page
        await _page.GotoAsync("http://localhost:5173");

        // Wait for the page to load
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Take a screenshot
        await _page.ScreenshotAsync(new() { Path = "homepage.png" });

        // Verify the page title
        var title = await _page.TitleAsync();
        Assert.Equal("Po.PoDropSquare.Blazor", title);

        Console.WriteLine("✅ Home page loaded successfully");
    }

    [Fact]
    public async Task BrowseApplication_GamePageLoads()
    {
        // Navigate to the game page
        await _page.GotoAsync("http://localhost:5173/game");

        // Wait for the page to load
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for the canvas to be visible
        await _page.WaitForSelectorAsync("canvas", new() { State = WaitForSelectorState.Visible });

        // Take a screenshot
        await _page.ScreenshotAsync(new() { Path = "gameload.png" });

        // Check that the game canvas is present
        var canvas = await _page.QuerySelectorAsync("canvas");
        Assert.NotNull(canvas);

        // Check for game UI elements
        var scoreDisplay = await _page.QuerySelectorAsync(".score-display");
        var gameArea = await _page.QuerySelectorAsync(".game-area");

        Console.WriteLine("✅ Game page loaded successfully");
        Console.WriteLine($"✅ Canvas found: {canvas != null}");
        Console.WriteLine($"✅ Score display found: {scoreDisplay != null}");
        Console.WriteLine($"✅ Game area found: {gameArea != null}");
    }

    [Fact]
    public async Task BrowseApplication_InteractWithGame()
    {
        // Navigate to the game page
        await _page.GotoAsync("http://localhost:5173/game");

        // Wait for the page to load completely
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for the canvas to be ready
        await _page.WaitForSelectorAsync("canvas", new() { State = WaitForSelectorState.Visible });

        // Wait a bit for JavaScript initialization
        await _page.WaitForTimeoutAsync(2000);

        // Try to start the game by pressing space
        await _page.Keyboard.PressAsync("Space");

        // Take a screenshot after trying to start
        await _page.ScreenshotAsync(new() { Path = "game-started.png" });

        // Try clicking on the canvas (to drop a block)
        var canvas = await _page.QuerySelectorAsync("canvas");
        if (canvas != null)
        {
            var boundingBox = await canvas.BoundingBoxAsync();
            if (boundingBox != null)
            {
                // Click in the center of the canvas
                await _page.Mouse.ClickAsync(
                    boundingBox.X + boundingBox.Width / 2,
                    boundingBox.Y + boundingBox.Height / 2
                );

                Console.WriteLine("✅ Clicked on game canvas");
            }
        }

        // Wait a moment for any game state changes
        await _page.WaitForTimeoutAsync(1000);

        // Take a final screenshot
        await _page.ScreenshotAsync(new() { Path = "game-interaction.png" });

        Console.WriteLine("✅ Game interaction test completed");
    }

    [Fact]
    public async Task BrowseApplication_CheckConsoleMessages()
    {
        var consoleMessages = new List<string>();

        // Listen for console messages
        _page.Console += (_, e) => consoleMessages.Add($"{e.Type}: {e.Text}");

        // Navigate to the game page
        await _page.GotoAsync("http://localhost:5173/game");

        // Wait for the page to load
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.WaitForTimeoutAsync(3000); // Wait for JavaScript initialization

        // Log all console messages
        Console.WriteLine("=== Console Messages ===");
        foreach (var message in consoleMessages)
        {
            Console.WriteLine(message);
        }

        // Check for specific expected messages
        var hasPhysicsInit = consoleMessages.Any(m => m.Contains("initiali") || m.Contains("Initializing"));
        var hasErrors = consoleMessages.Any(m => m.ToLower().Contains("error"));

        Console.WriteLine($"✅ Physics initialization messages found: {hasPhysicsInit}");
        Console.WriteLine($"✅ Error messages found: {hasErrors}");
        Console.WriteLine($"✅ Total console messages: {consoleMessages.Count}");
    }
}