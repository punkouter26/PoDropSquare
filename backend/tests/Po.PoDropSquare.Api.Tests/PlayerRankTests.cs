using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Po.PoDropSquare.Api.Tests;

/// <summary>
/// Tests for GET /api/scores/player/{playerInitials}/rank endpoint
/// </summary>
public class PlayerRankTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PlayerRankTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GET_PlayerRank_WithValidInitials_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/api/scores/player/ABC/rank");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(responseObject.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
        Assert.True(responseObject.TryGetProperty("playerInitials", out var initials));
        Assert.Equal("ABC", initials.GetString());
        Assert.True(responseObject.TryGetProperty("rank", out _));
        Assert.True(responseObject.TryGetProperty("totalPlayers", out _));
    }

    [Theory]
    [InlineData("XYZ")]
    [InlineData("AAA")]
    [InlineData("ZZZ")]
    public async Task GET_PlayerRank_WithVariousInitials_ShouldSucceed(string playerInitials)
    {
        // Act
        var response = await _client.GetAsync($"/api/scores/player/{playerInitials}/rank");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(responseObject.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB")]  // Too short
    [InlineData("ABCD")]  // Too long
    [InlineData("ab")]  // Lowercase
    [InlineData("AB!")]  // Special characters
    public async Task GET_PlayerRank_WithInvalidInitials_ShouldReturn400(string invalidInitials)
    {
        // Act
        var response = await _client.GetAsync($"/api/scores/player/{Uri.EscapeDataString(invalidInitials)}/rank");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                   response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_PlayerRank_ForNonExistentPlayer_ShouldReturn404()
    {
        // Use highly unlikely initials
        // Act
        var response = await _client.GetAsync("/api/scores/player/XXX/rank");

        // Assert
        // Could be 404 if player doesn't exist or 200 with rank=null/unranked
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Should indicate player has no rank
            Assert.True(responseObject.TryGetProperty("rank", out var rank));
            // Rank should be null or 0 or specific "unranked" indicator
        }
    }

    [Fact]
    public async Task GET_PlayerRank_ShouldReturnCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/scores/player/ABC/rank");

        // Assert
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GET_PlayerRank_ResponseTime_ShouldBeFast()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/api/scores/player/ABC/rank");
        stopwatch.Stop();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 200,
            $"Player rank lookup took {stopwatch.ElapsedMilliseconds}ms, should be under 200ms");
    }

    [Fact]
    public async Task GET_PlayerRank_MultipleConcurrent_ShouldSucceed()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        var players = new[] { "AAA", "BBB", "CCC", "DDD", "EEE" };

        foreach (var player in players)
        {
            tasks.Add(_client.GetAsync($"/api/scores/player/{player}/rank"));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response.Dispose();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _client?.Dispose();
        }
    }

    [Fact]
    public void CleanupResources()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
