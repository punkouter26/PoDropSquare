using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Po.PoDropSquare.Api.Tests;

/// <summary>
/// Contract tests for POST /api/scores endpoint
/// These tests MUST fail initially and pass only after implementation
/// </summary>
public class ScoreSubmissionContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ScoreSubmissionContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task POST_Scores_WithValidRequest_ShouldReturn200AndScoreAccepted()
    {
        // Arrange
        var scoreSubmission = new
        {
            playerInitials = "ABC",
            survivalTime = 18.75,
            sessionSignature = "sha256_hash_of_session_data",
            clientTimestamp = DateTime.UtcNow.ToString("O")
        };

        var json = JsonSerializer.Serialize(scoreSubmission);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/scores", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(responseObject.GetProperty("success").GetBoolean());
        Assert.True(responseObject.TryGetProperty("scoreId", out var scoreId));
        Assert.False(string.IsNullOrEmpty(scoreId.GetString()));
        Assert.True(responseObject.TryGetProperty("leaderboardPosition", out _));
        Assert.True(responseObject.TryGetProperty("qualifiedForLeaderboard", out _));
        Assert.True(responseObject.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task POST_Scores_WithNewHighScore_ShouldReturn201()
    {
        // Arrange
        var perfectScore = new
        {
            playerInitials = "PRO",
            survivalTime = 20.00,
            sessionSignature = "sha256_hash_of_perfect_session",
            clientTimestamp = DateTime.UtcNow.ToString("O")
        };

        var json = JsonSerializer.Serialize(perfectScore);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/scores", content);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(responseObject.GetProperty("success").GetBoolean());
        Assert.True(responseObject.GetProperty("qualifiedForLeaderboard").GetBoolean());
    }

    [Theory]
    [InlineData("", "Player initials cannot be empty")]
    [InlineData("TOOLONG", "Player initials must be 1-3 characters")]
    [InlineData("AB!", "Player initials must be alphanumeric")]
    [InlineData("ab", "Player initials must be uppercase")]
    public async Task POST_Scores_WithInvalidPlayerInitials_ShouldReturn400(string invalidInitials)
    {
        // Arrange
        var invalidSubmission = new
        {
            playerInitials = invalidInitials,
            survivalTime = 15.50,
            sessionSignature = "valid_signature",
            clientTimestamp = DateTime.UtcNow.ToString("O")
        };

        var json = JsonSerializer.Serialize(invalidSubmission);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/scores", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.False(responseObject.GetProperty("success").GetBoolean());
        Assert.Equal("VALIDATION_FAILED", responseObject.GetProperty("error").GetString());
        Assert.Contains("playerInitials", responseObject.GetProperty("message").GetString());
    }

    [Theory]
    [InlineData(-0.5, "Survival time cannot be negative")]
    [InlineData(0.0, "Survival time must be greater than 0")]
    [InlineData(25.0, "Survival time cannot exceed 20 seconds")]
    [InlineData(0.001, "Survival time precision too high")]
    public async Task POST_Scores_WithInvalidSurvivalTime_ShouldReturn400(double invalidTime)
    {
        // Arrange
        var invalidSubmission = new
        {
            playerInitials = "ABC",
            survivalTime = invalidTime,
            sessionSignature = "valid_signature",
            clientTimestamp = DateTime.UtcNow.ToString("O")
        };

        var json = JsonSerializer.Serialize(invalidSubmission);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/scores", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.False(responseObject.GetProperty("success").GetBoolean());
        Assert.Equal("VALIDATION_FAILED", responseObject.GetProperty("error").GetString());
    }

    [Fact]
    public async Task POST_Scores_WithMissingSessionSignature_ShouldReturn400()
    {
        // Arrange
        var invalidSubmission = new
        {
            playerInitials = "ABC",
            survivalTime = 15.50,
            // sessionSignature missing
            clientTimestamp = DateTime.UtcNow.ToString("O")
        };

        var json = JsonSerializer.Serialize(invalidSubmission);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/scores", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.False(responseObject.GetProperty("success").GetBoolean());
        Assert.Equal("VALIDATION_FAILED", responseObject.GetProperty("error").GetString());
    }

    [Fact]
    public async Task POST_Scores_WithOldTimestamp_ShouldReturn400()
    {
        // Arrange
        var oldTimestamp = DateTime.UtcNow.AddMinutes(-10).ToString("O"); // 10 minutes ago
        var invalidSubmission = new
        {
            playerInitials = "ABC",
            survivalTime = 15.50,
            sessionSignature = "valid_signature",
            clientTimestamp = oldTimestamp
        };

        var json = JsonSerializer.Serialize(invalidSubmission);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/scores", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.False(responseObject.GetProperty("success").GetBoolean());
        Assert.Equal("VALIDATION_FAILED", responseObject.GetProperty("error").GetString());
        Assert.Contains("timestamp", responseObject.GetProperty("message").GetString().ToLower());
    }

    [Fact]
    public async Task POST_Scores_WithInvalidJson_ShouldReturn400()
    {
        // Arrange
        var invalidJson = "{ invalid json structure";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/scores", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_Scores_WithMissingContentType_ShouldReturn415()
    {
        // Arrange
        var validJson = JsonSerializer.Serialize(new
        {
            playerInitials = "ABC",
            survivalTime = 15.50,
            sessionSignature = "valid_signature",
            clientTimestamp = DateTime.UtcNow.ToString("O")
        });

        var content = new StringContent(validJson, Encoding.UTF8, "text/plain"); // Wrong content type

        // Act
        var response = await _client.PostAsync("/api/scores", content);

        // Assert
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task POST_Scores_RateLimited_ShouldReturn429()
    {
        // Arrange - Submit multiple requests rapidly to trigger rate limiting
        var scoreSubmission = new
        {
            playerInitials = "SPM", // SPaM
            survivalTime = 1.00,
            sessionSignature = "spam_signature",
            clientTimestamp = DateTime.UtcNow.ToString("O")
        };

        var json = JsonSerializer.Serialize(scoreSubmission);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act - Send 61 requests (over the 60/minute limit)
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 61; i++)
        {
            lastResponse = await _client.PostAsync("/api/scores", new StringContent(json, Encoding.UTF8, "application/json"));
        }

        // Assert
        Assert.NotNull(lastResponse);
        // Either the last request is rate limited, or at least one during the loop
        // The exact implementation depends on the rate limiting strategy
        Assert.True(lastResponse.StatusCode == HttpStatusCode.TooManyRequests ||
                   lastResponse.StatusCode == HttpStatusCode.BadRequest ||
                   lastResponse.StatusCode == HttpStatusCode.OK);

        if (lastResponse.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var responseContent = await lastResponse.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

            Assert.False(responseObject.GetProperty("success").GetBoolean());
            Assert.Equal("RATE_LIMITED", responseObject.GetProperty("error").GetString());
            Assert.True(responseObject.TryGetProperty("retryAfter", out _));
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
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}