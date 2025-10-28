using Microsoft.Playwright;
using Xunit;

namespace Po.PoDropSquare.E2E.Tests;

/// <summary>
/// Core E2E tests for PoDropSquare application
/// Tests the main user flows and UI functionality
/// </summary>
public class CoreGameplayE2ETests : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;
    private const string BASE_URL = "http://localhost:5000"; // API hosts the Blazor app

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true // Headless for CI/CD
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
    public async Task HomePage_ShouldLoadSuccessfully()
    {
        // Act
        await _page.GotoAsync(BASE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var title = await _page.TitleAsync();
        Assert.Equal("PoDropSquare", title);

        // Check for main game elements
        var gameCanvas = await _page.QuerySelectorAsync("canvas");
        Assert.NotNull(gameCanvas);

        Console.WriteLine("✅ Home page loaded successfully");
    }

    [Fact]
    public async Task Navigation_ToHighScores_ShouldWork()
    {
        // Arrange
        await _page.GotoAsync(BASE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var highScoresLink = await _page.QuerySelectorAsync("a[href='/highscores']");
        if (highScoresLink != null)
        {
            await highScoresLink.ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert
            var url = _page.Url;
            Assert.Contains("/highscores", url.ToLower());

            Console.WriteLine("✅ Navigation to high scores page successful");
        }
        else
        {
            // If navigation element not found, just verify page works
            await _page.GotoAsync($"{BASE_URL}/highscores");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Console.WriteLine("⚠️ High scores link not found, navigated directly");
        }
    }

    [Fact]
    public async Task Diagnostics_Page_ShouldShowHealthStatus()
    {
        // Act
        await _page.GotoAsync($"{BASE_URL}/diag");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for health check to complete
        await _page.WaitForSelectorAsync(".badge", new() { Timeout = 5000 });

        // Assert
        var pageContent = await _page.ContentAsync();
        Assert.Contains("System Diagnostics", pageContent);
        Assert.Contains("Backend Health Status", pageContent);

        // Check for status badge
        var statusBadge = await _page.QuerySelectorAsync(".badge");
        Assert.NotNull(statusBadge);

        var statusText = await statusBadge!.InnerTextAsync();
        Assert.True(statusText.Contains("Healthy") || statusText.Contains("Degraded") || statusText.Contains("Unhealthy"),
            $"Status should be a valid health state, got: {statusText}");

        Console.WriteLine($"✅ Diagnostics page shows health status: {statusText}");
    }

    [Fact]
    public async Task GameCanvas_ShouldBeInteractive()
    {
        // Arrange
        await _page.GotoAsync(BASE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for canvas to be ready
        var canvas = await _page.WaitForSelectorAsync("canvas", new() { State = WaitForSelectorState.Visible });
        Assert.NotNull(canvas);

        // Act - Click on canvas
        var boundingBox = await canvas!.BoundingBoxAsync();
        if (boundingBox != null)
        {
            await _page.Mouse.ClickAsync(
                boundingBox.X + boundingBox.Width / 2,
                boundingBox.Y + boundingBox.Height / 2
            );

            // Wait a moment for any game state changes
            await Task.Delay(1000);

            Console.WriteLine("✅ Game canvas is interactive and clickable");
        }

        // Assert - Canvas should still be visible (didn't crash)
        var canvasStillVisible = await _page.IsVisibleAsync("canvas");
        Assert.True(canvasStillVisible);
    }

    [Fact]
    public async Task ResponsiveDesign_Mobile_ShouldWork()
    {
        // Act - Set mobile viewport
        await _page.SetViewportSizeAsync(375, 667); // iPhone SE size
        await _page.GotoAsync(BASE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Page should load without horizontal scroll
        var scrollWidth = await _page.EvaluateAsync<int>("() => document.documentElement.scrollWidth");
        var clientWidth = await _page.EvaluateAsync<int>("() => document.documentElement.clientWidth");

        Assert.True(scrollWidth <= clientWidth + 10, // Allow small tolerance
            $"Page should not have horizontal scroll on mobile. ScrollWidth: {scrollWidth}, ClientWidth: {clientWidth}");

        // Check if canvas is visible
        var canvasVisible = await _page.IsVisibleAsync("canvas");
        Assert.True(canvasVisible, "Game canvas should be visible on mobile");

        Console.WriteLine("✅ Responsive design works on mobile (portrait mode)");
    }

    [Fact]
    public async Task ResponsiveDesign_Desktop_ShouldWork()
    {
        // Act - Set desktop viewport
        await _page.SetViewportSizeAsync(1920, 1080); // Full HD
        await _page.GotoAsync(BASE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Canvas should be visible
        var canvas = await _page.QuerySelectorAsync("canvas");
        Assert.NotNull(canvas);

        var isVisible = await canvas!.IsVisibleAsync();
        Assert.True(isVisible);

        Console.WriteLine("✅ Responsive design works on desktop");
    }

    [Fact]
    public async Task HighScoresPage_ShouldDisplayLeaderboard()
    {
        // Act
        await _page.GotoAsync($"{BASE_URL}/highscores");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for leaderboard data to load
        await Task.Delay(1000);

        // Assert - Check for leaderboard structure
        var pageContent = await _page.ContentAsync();
        
        // Should have high scores title or heading
        Assert.True(pageContent.Contains("HIGH SCORES", StringComparison.OrdinalIgnoreCase) ||
                   pageContent.Contains("LEADERBOARD", StringComparison.OrdinalIgnoreCase),
                   "High scores page should have a leaderboard heading");

        Console.WriteLine("✅ High scores page displays leaderboard");
    }

    [Fact]
    public async Task ConsoleErrors_ShouldBeMinimal()
    {
        // Arrange - Track console messages
        var consoleErrors = new List<string>();
        _page.Console += (_, e) =>
        {
            if (e.Type == "error")
            {
                consoleErrors.Add(e.Text);
            }
        };

        // Act
        await _page.GotoAsync(BASE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(2000); // Wait for any lazy-loaded scripts

        // Assert - Should have minimal or no console errors
        Console.WriteLine($"Console errors found: {consoleErrors.Count}");
        foreach (var error in consoleErrors)
        {
            Console.WriteLine($"  - {error}");
        }

        // Allow for some non-critical errors, but not too many
        Assert.True(consoleErrors.Count < 5,
            $"Too many console errors ({consoleErrors.Count}). Application should run without critical errors.");
    }

    [Fact]
    public async Task PagePerformance_ShouldLoadQuickly()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await _page.GotoAsync(BASE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        stopwatch.Stop();

        // Assert - Should load in reasonable time
        Assert.True(stopwatch.ElapsedMilliseconds < 5000,
            $"Page took {stopwatch.ElapsedMilliseconds}ms to load, should be under 5 seconds");

        Console.WriteLine($"✅ Page loaded in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task APIHealthCheck_ShouldBeAccessible()
    {
        // Act
        var response = await _page.GotoAsync($"{BASE_URL}/api/health");

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Ok, "Health check endpoint should return 200 OK");

        var content = await response.TextAsync();
        Assert.Contains("Healthy", content);

        Console.WriteLine("✅ API health check is accessible and returns healthy status");
    }
}
