using Microsoft.Playwright;

namespace Po.PoDropSquare.E2E.Tests;

public class PlaywrightDemo : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false // Set to false to see the browser in action
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
    public async Task Demo_BrowseGitHub()
    {
        Console.WriteLine("🚀 Starting Playwright browser demo...");

        // Navigate to GitHub
        await _page.GotoAsync("https://github.com");

        // Wait for the page to load
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Take a screenshot
        await _page.ScreenshotAsync(new() { Path = "github-homepage.png" });

        // Get the page title
        var title = await _page.TitleAsync();
        Console.WriteLine($"✅ Page title: {title}");

        // Search for a repository
        var searchBox = _page.Locator("input[placeholder*='Search']").First;
        await searchBox.ClickAsync();
        await searchBox.FillAsync("microsoft/playwright");
        await searchBox.PressAsync("Enter");

        // Wait for search results
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Take a screenshot of search results
        await _page.ScreenshotAsync(new() { Path = "github-search.png" });

        Console.WriteLine("✅ Successfully browsed GitHub and performed search");
        Console.WriteLine("✅ Screenshots saved: github-homepage.png and github-search.png");

        // Verify we're on a GitHub page
        Assert.Contains("GitHub", title);
    }

    [Fact]
    public async Task Demo_BrowseLocalhost()
    {
        Console.WriteLine("🌐 Testing localhost connectivity...");

        try
        {
            // First try to navigate to localhost:5173
            await _page.GotoAsync("http://localhost:5173", new() { Timeout = 5000 });

            var title = await _page.TitleAsync();
            Console.WriteLine($"✅ Connected to localhost:5173, title: {title}");

            // Take a screenshot
            await _page.ScreenshotAsync(new() { Path = "localhost-homepage.png" });

            // Try to navigate to the game page
            await _page.GotoAsync("http://localhost:5173/game");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Look for game elements
            var canvas = await _page.QuerySelectorAsync("canvas");

            Console.WriteLine($"✅ Game page loaded, canvas found: {canvas != null}");

            // Take a screenshot of the game page
            await _page.ScreenshotAsync(new() { Path = "localhost-game.png" });

            Console.WriteLine("✅ Successfully browsed local application");
        }
        catch (TimeoutException)
        {
            Console.WriteLine("⚠️ Localhost:5173 is not accessible - application may not be running");
            Console.WriteLine("💡 To test with the local app, run: dotnet run --project frontend/src/Po.PoDropSquare.Blazor/Po.PoDropSquare.Blazor.csproj");

            // Skip assertion in this case
            return;
        }
    }

    [Fact]
    public async Task Demo_InteractiveFeatures()
    {
        Console.WriteLine("🎯 Demonstrating interactive browser automation...");

        // Navigate to a demo site
        await _page.GotoAsync("https://example.com");

        // Wait for load
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Get page content
        var content = await _page.TextContentAsync("body");
        Console.WriteLine($"✅ Page content length: {content?.Length} characters");

        // Check for specific elements
        var links = await _page.QuerySelectorAllAsync("a");
        Console.WriteLine($"✅ Found {links.Count} links on the page");

        // Take a screenshot
        await _page.ScreenshotAsync(new() { Path = "example-site.png" });

        // Demonstrate JavaScript execution
        var pageHeight = await _page.EvaluateAsync<int>("() => document.body.scrollHeight");
        Console.WriteLine($"✅ Page height: {pageHeight}px");

        Console.WriteLine("✅ Interactive features demo completed");

        Assert.True(pageHeight > 0);
    }
}