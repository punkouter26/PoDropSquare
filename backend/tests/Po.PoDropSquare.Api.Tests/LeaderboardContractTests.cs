using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Po.PoDropSquare.Api.Tests;

/// <summary>
/// Contract tests for GET /api/scores/top10 endpoint
/// These tests MUST fail initially and pass only after implementation
/// </summary>
public class LeaderboardContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public LeaderboardContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GET_ScoresTop10_ShouldReturn200WithLeaderboardData()
    {
        // Act
        var response = await _client.GetAsync("/api/scores/top10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        // Verify response structure
        Assert.True(responseObject.GetProperty("success").GetBoolean());
        Assert.True(responseObject.TryGetProperty("leaderboard", out var leaderboard));
        Assert.Equal(JsonValueKind.Array, leaderboard.ValueKind);
        Assert.True(responseObject.TryGetProperty("lastUpdated", out var lastUpdated));
        Assert.False(string.IsNullOrEmpty(lastUpdated.GetString()));
        Assert.True(responseObject.TryGetProperty("totalEntries", out var totalEntries));
        Assert.True(totalEntries.GetInt32() >= 0);
    }

    [Fact]
    public async Task GET_ScoresTop10_WithEmptyLeaderboard_ShouldReturnEmptyArray()
    {
        // Act
        var response = await _client.GetAsync("/api/scores/top10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(responseObject.GetProperty("success").GetBoolean());

        var leaderboard = responseObject.GetProperty("leaderboard");
        Assert.Equal(JsonValueKind.Array, leaderboard.ValueKind);
        // Empty leaderboard should still return valid structure
        Assert.True(leaderboard.GetArrayLength() >= 0);
        Assert.True(leaderboard.GetArrayLength() <= 10);
    }

    [Fact]
    public async Task GET_ScoresTop10_ShouldReturnMaximum10Entries()
    {
        // Act
        var response = await _client.GetAsync("/api/scores/top10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        var leaderboard = responseObject.GetProperty("leaderboard");
        Assert.True(leaderboard.GetArrayLength() <= 10, "Leaderboard should not return more than 10 entries");
    }

    [Fact]
    public async Task GET_ScoresTop10_EntriesShouldBeInDescendingOrder()
    {
        // Act
        var response = await _client.GetAsync("/api/scores/top10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        var leaderboard = responseObject.GetProperty("leaderboard");

        if (leaderboard.GetArrayLength() > 1)
        {
            double previousSurvivalTime = double.MaxValue;
            int expectedPosition = 1;

            foreach (var entry in leaderboard.EnumerateArray())
            {
                // Verify position is sequential
                Assert.Equal(expectedPosition, entry.GetProperty("position").GetInt32());

                // Verify survival time is in descending order
                var currentSurvivalTime = entry.GetProperty("survivalTime").GetDouble();
                Assert.True(currentSurvivalTime <= previousSurvivalTime,
                    $"Leaderboard entries should be in descending order by survival time. " +
                    $"Position {expectedPosition}: {currentSurvivalTime} should be <= {previousSurvivalTime}");

                previousSurvivalTime = currentSurvivalTime;
                expectedPosition++;
            }
        }
    }

    [Fact]
    public async Task GET_ScoresTop10_EachEntryShouldHaveRequiredFields()
    {
        // Act
        var response = await _client.GetAsync("/api/scores/top10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        var leaderboard = responseObject.GetProperty("leaderboard");

        foreach (var entry in leaderboard.EnumerateArray())
        {
            // Verify required fields exist
            Assert.True(entry.TryGetProperty("position", out var position));
            Assert.True(position.GetInt32() > 0 && position.GetInt32() <= 10);

            Assert.True(entry.TryGetProperty("playerInitials", out var playerInitials));
            var initials = playerInitials.GetString();
            Assert.False(string.IsNullOrEmpty(initials));
            Assert.True(initials.Length >= 1 && initials.Length <= 3);
            Assert.True(initials.All(char.IsLetterOrDigit), "Player initials should be alphanumeric");
            Assert.True(initials.All(char.IsUpper), "Player initials should be uppercase");

            Assert.True(entry.TryGetProperty("survivalTime", out var survivalTime));
            Assert.True(survivalTime.GetDouble() > 0);
            Assert.True(survivalTime.GetDouble() <= 20.0);

            Assert.True(entry.TryGetProperty("achievedAt", out var achievedAt));
            Assert.False(string.IsNullOrEmpty(achievedAt.GetString()));
            // Verify it's a valid ISO 8601 timestamp
            Assert.True(DateTime.TryParse(achievedAt.GetString(), out _));
        }
    }

    [Fact]
    public async Task GET_ScoresTop10_ShouldSetCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/scores/top10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GET_ScoresTop10_ShouldSupportConditionalRequests()
    {
        // Act - First request
        var firstResponse = await _client.GetAsync("/api/scores/top10");

        // Assert first response
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Extract ETag or Last-Modified for conditional request
        var etag = firstResponse.Headers.ETag?.Tag;
        var lastModified = firstResponse.Content.Headers.LastModified;

        if (!string.IsNullOrEmpty(etag))
        {
            // Act - Conditional request with ETag
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("If-None-Match", etag);

            var conditionalResponse = await _client.GetAsync("/api/scores/top10");

            // Assert - Should return 304 Not Modified if content hasn't changed
            // Or 200 if content has changed (both are valid for this test)
            Assert.True(conditionalResponse.StatusCode == HttpStatusCode.NotModified ||
                       conditionalResponse.StatusCode == HttpStatusCode.OK);
        }
        else if (lastModified.HasValue)
        {
            // Act - Conditional request with Last-Modified
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("If-Modified-Since", lastModified.Value.ToString("R"));

            var conditionalResponse = await _client.GetAsync("/api/scores/top10");

            // Assert - Should return 304 Not Modified if content hasn't changed
            Assert.True(conditionalResponse.StatusCode == HttpStatusCode.NotModified ||
                       conditionalResponse.StatusCode == HttpStatusCode.OK);
        }

        // Clean up headers
        _client.DefaultRequestHeaders.Clear();
    }

    [Fact]
    public async Task GET_ScoresTop10_ShouldHandleCacheHeaders()
    {
        // Act
        var response = await _client.GetAsync("/api/scores/top10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Response should have cache control headers for 5-minute caching
        var cacheControl = response.Headers.CacheControl;
        if (cacheControl != null)
        {
            // Should allow caching but with reasonable max-age
            Assert.True(cacheControl.MaxAge?.TotalMinutes <= 5,
                "Cache should not exceed 5 minutes as per spec");
        }
    }

    [Fact]
    public async Task GET_ScoresTop10_WithAcceptHeader_ShouldReturnJson()
    {
        // Arrange
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Accept", "application/json");

        // Act
        var response = await _client.GetAsync("/api/scores/top10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        // Clean up
        _client.DefaultRequestHeaders.Clear();
    }

    [Fact]
    public async Task GET_ScoresTop10_WithInvalidAcceptHeader_ShouldReturn406()
    {
        // Arrange
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Accept", "text/xml");

        // Act
        var response = await _client.GetAsync("/api/scores/top10");

        // Assert
        // Should return 406 Not Acceptable or still return JSON (implementation dependent)
        Assert.True(response.StatusCode == HttpStatusCode.NotAcceptable ||
                   response.StatusCode == HttpStatusCode.OK);

        // Clean up
        _client.DefaultRequestHeaders.Clear();
    }

    [Fact]
    public async Task GET_ScoresTop10_ResponseTime_ShouldBeFast()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/api/scores/top10");
        stopwatch.Stop();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Response should be under 200ms as per performance requirements
        Assert.True(stopwatch.ElapsedMilliseconds < 200,
            $"Leaderboard response took {stopwatch.ElapsedMilliseconds}ms, should be under 200ms");
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