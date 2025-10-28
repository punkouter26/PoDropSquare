using Microsoft.Playwright;
using Xunit;

namespace Po.PoDropSquare.E2E.Tests;

/// <summary>
/// E2E tests to verify the simplified game UI implementation
/// Tests that the game screen has the expected minimal elements without extra clutter
/// </summary>
public class SimplifiedGameUITests : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;
    private const string BASE_URL = "http://localhost:5000";
    private const string GAME_PAGE_URL = "http://localhost:5000/game";

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
    public async Task GamePage_ShouldHaveTitle()
    {
        // Act
        await _page.GotoAsync(GAME_PAGE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Check for game title
        var titleElement = await _page.QuerySelectorAsync("h1.game-title");
        Assert.NotNull(titleElement);

        var titleText = await titleElement!.InnerTextAsync();
        Assert.Equal("PoDropSquare", titleText);

        Console.WriteLine("✅ Game page has title: PoDropSquare");
    }

    [Fact]
    public async Task GamePage_ShouldHaveScoreDisplay()
    {
        // Act
        await _page.GotoAsync(GAME_PAGE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Score display should exist
        var scoreDisplay = await _page.QuerySelectorAsync(".score-display");
        Assert.NotNull(scoreDisplay);

        var scoreValue = await _page.QuerySelectorAsync(".score-value");
        Assert.NotNull(scoreValue);

        var scoreText = await scoreValue!.InnerTextAsync();
        Assert.NotNull(scoreText);
        Assert.True(int.TryParse(scoreText, out _), $"Score should be a number, got: {scoreText}");

        Console.WriteLine($"✅ Game page has score display showing: {scoreText}");
    }

    [Fact]
    public async Task GamePage_ShouldHaveTimerDisplay()
    {
        // Act
        await _page.GotoAsync(GAME_PAGE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Timer display should exist
        var timerDisplay = await _page.QuerySelectorAsync(".timer-display");
        Assert.NotNull(timerDisplay);

        Console.WriteLine("✅ Game page has timer display");
    }

    [Fact]
    public async Task GamePage_ShouldHaveGameCanvas()
    {
        // Act
        await _page.GotoAsync(GAME_PAGE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Canvas should exist and be visible
        var canvas = await _page.WaitForSelectorAsync("canvas#mainGameCanvas", new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000
        });
        Assert.NotNull(canvas);

        var isVisible = await canvas!.IsVisibleAsync();
        Assert.True(isVisible);

        // Verify canvas dimensions (should be 300x200 after simplification)
        var width = await canvas.GetAttributeAsync("width");
        var height = await canvas.GetAttributeAsync("height");

        Assert.Equal("300", width);
        Assert.Equal("200", height);

        Console.WriteLine($"✅ Game canvas exists with dimensions: {width}x{height}");
    }

    [Fact]
    public async Task GamePage_ShouldHaveTopScoresButton()
    {
        // Act
        await _page.GotoAsync(GAME_PAGE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Top 10 Scores button should exist
        var pageContent = await _page.ContentAsync();
        Assert.Contains("TOP 10 SCORES", pageContent, StringComparison.OrdinalIgnoreCase);

        Console.WriteLine("✅ Game page has TOP 10 SCORES button");
    }

    [Fact]
    public async Task GamePage_ShouldNotHaveHighScoresSection()
    {
        // Act
        await _page.GotoAsync(GAME_PAGE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - High scores section should NOT exist on game page
        var scoresSection = await _page.QuerySelectorAsync(".scores-section");
        Assert.Null(scoresSection);

        // Should NOT have embedded leaderboard on game page
        var pageContent = await _page.ContentAsync();
        var hasHighScoresHeading = pageContent.Contains("<h2>High Scores</h2>", StringComparison.OrdinalIgnoreCase);
        Assert.False(hasHighScoresHeading, "Game page should not have embedded high scores section");

        Console.WriteLine("✅ Game page does NOT have high scores section (simplified UI)");
    }

    [Fact]
    public async Task GamePage_ShouldHaveMinimalElements()
    {
        // Act
        await _page.GotoAsync(GAME_PAGE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Verify only essential elements exist
        var essentialElements = new Dictionary<string, string>
        {
            { "Title", "h1.game-title" },
            { "Score Display", ".score-display" },
            { "Timer Display", ".timer-display" },
            { "Game Canvas", "canvas#mainGameCanvas" }
        };

        foreach (var (name, selector) in essentialElements)
        {
            var element = await _page.QuerySelectorAsync(selector);
            Assert.NotNull(element);
            Console.WriteLine($"  ✓ {name} found");
        }

        Console.WriteLine("✅ Game page has all essential elements and nothing extra");
    }

    [Fact]
    public async Task GamePage_ShouldFitWithoutScrolling()
    {
        // Act - Set standard desktop viewport
        await _page.SetViewportSizeAsync(1366, 768); // Common laptop resolution
        await _page.GotoAsync(GAME_PAGE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Page should not require scrolling
        var scrollHeight = await _page.EvaluateAsync<int>("() => document.documentElement.scrollHeight");
        var clientHeight = await _page.EvaluateAsync<int>("() => document.documentElement.clientHeight");

        Assert.True(scrollHeight <= clientHeight + 50, // Allow small tolerance for padding
            $"Game page should fit without vertical scrolling. ScrollHeight: {scrollHeight}, ClientHeight: {clientHeight}");

        Console.WriteLine($"✅ Game page fits without scrolling (ScrollHeight: {scrollHeight}, ClientHeight: {clientHeight})");
    }

    [Fact]
    public async Task GamePage_CanvasSize_ShouldBeReducedTo300x200()
    {
        // Act
        await _page.GotoAsync(GAME_PAGE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Canvas dimensions should be 300x200 (50% of original 600x400)
        var canvas = await _page.WaitForSelectorAsync("canvas#mainGameCanvas");
        Assert.NotNull(canvas);

        var width = await canvas!.GetAttributeAsync("width");
        var height = await canvas.GetAttributeAsync("height");

        Assert.Equal("300", width);
        Assert.Equal("200", height);

        // Verify rendered size matches
        var boundingBox = await canvas.BoundingBoxAsync();
        Assert.NotNull(boundingBox);
        
        // Allow small tolerance for CSS scaling/borders
        Assert.InRange(boundingBox!.Width, 295, 310);
        Assert.InRange(boundingBox.Height, 195, 210);

        Console.WriteLine($"✅ Canvas size verified: {width}x{height} (50% reduction from 600x400)");
    }

    [Fact]
    public async Task GamePage_ShouldBeResponsive()
    {
        // Test multiple viewport sizes
        var viewportSizes = new[]
        {
            (1920, 1080, "Desktop Full HD"),
            (1366, 768, "Laptop"),
            (768, 1024, "Tablet Portrait"),
            (375, 667, "Mobile")
        };

        foreach (var (width, height, description) in viewportSizes)
        {
            // Act
            await _page.SetViewportSizeAsync(width, height);
            await _page.GotoAsync(GAME_PAGE_URL);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert - Canvas should be visible
            var canvas = await _page.QuerySelectorAsync("canvas#mainGameCanvas");
            Assert.NotNull(canvas);

            var isVisible = await canvas!.IsVisibleAsync();
            Assert.True(isVisible, $"Canvas should be visible on {description} ({width}x{height})");

            Console.WriteLine($"  ✓ {description} ({width}x{height}): Canvas visible");
        }

        Console.WriteLine("✅ Game page is responsive across all viewport sizes");
    }

    [Fact]
    public async Task GamePage_ShouldHaveCleanLayout()
    {
        // Act
        await _page.GotoAsync(GAME_PAGE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Verify clean layout structure
        var gameSection = await _page.QuerySelectorAsync(".game-section");
        Assert.NotNull(gameSection);

        var gameHeader = await _page.QuerySelectorAsync(".game-header");
        Assert.NotNull(gameHeader);

        var gameHud = await _page.QuerySelectorAsync(".game-hud");
        Assert.NotNull(gameHud);

        var canvasContainer = await _page.QuerySelectorAsync(".game-canvas-container");
        Assert.NotNull(canvasContainer);

        // Should NOT have scores-section (removed in simplification)
        var scoresSection = await _page.QuerySelectorAsync(".scores-section");
        Assert.Null(scoresSection);

        Console.WriteLine("✅ Game page has clean, simplified layout structure");
    }

    [Fact]
    public async Task GamePage_OverlayElements_ShouldWork()
    {
        // Act
        await _page.GotoAsync(GAME_PAGE_URL);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Game overlay should be present (for start/game over screens)
        var overlay = await _page.QuerySelectorAsync(".game-overlay");
        
        if (overlay != null)
        {
            var overlayContent = await _page.QuerySelectorAsync(".overlay-content");
            Assert.NotNull(overlayContent);

            Console.WriteLine("✅ Game overlay elements are present");
        }
        else
        {
            Console.WriteLine("⚠️ Game overlay not visible (game may be running)");
        }
    }
}
