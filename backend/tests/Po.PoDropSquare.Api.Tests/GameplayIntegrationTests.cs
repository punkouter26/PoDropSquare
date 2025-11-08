using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Po.PoDropSquare.Api.Tests;

/// <summary>
/// Integration tests for complete gameplay sessions, validating end-to-end game flow.
/// These tests ensure proper coordination between game state, scoring, and session management.
/// All tests should FAIL until the actual gameplay endpoints are implemented.
/// </summary>
public class GameplayIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GameplayIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "GameplayIntegration")]
    public async Task CompleteGameplaySession_WithValidFlow_ShouldSucceed()
    {
        // Act: Start a new game session
        var startResponse = await _client.PostAsync("/api/game/start", null);

        // Assert: Game session should be created
        Assert.Equal(System.Net.HttpStatusCode.Created, startResponse.StatusCode);

        var sessionData = await startResponse.Content.ReadAsStringAsync();
        var session = JsonSerializer.Deserialize<GameSession>(sessionData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.NotNull(session);
        Assert.NotEmpty(session.SessionId);
        Assert.Equal("InProgress", session.Status);
        Assert.True(session.StartTime <= DateTime.UtcNow);
        Assert.Null(session.EndTime);
        Assert.Equal(0, session.Score);
        Assert.Equal(30, session.TimeRemaining); // 30-second sessions

        // Act: Submit block placement during game
        var blockPlacement = new
        {
            SessionId = session.SessionId,
            BlockType = "Square",
            Position = new { X = 400, Y = 100 },
            Rotation = 0,
            Timestamp = DateTime.UtcNow
        };

        var placementResponse = await _client.PostAsJsonAsync("/api/game/place-block", blockPlacement);

        // Assert: Block placement should be accepted
        Assert.Equal(System.Net.HttpStatusCode.OK, placementResponse.StatusCode);

        var placementResult = await placementResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BlockPlacementResult>(placementResult, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(result.Score >= session.Score);
        Assert.False(result.GameOver);

        // Act: End the game session
        var endGameRequest = new
        {
            SessionId = session.SessionId,
            FinalScore = result.Score,
            SurvivalTime = 15.5, // Lasted 15.5 seconds
            PlayerInitials = "ABC"
        };

        var endResponse = await _client.PostAsJsonAsync("/api/game/end", endGameRequest);

        // Assert: Game should end successfully
        Assert.Equal(System.Net.HttpStatusCode.OK, endResponse.StatusCode);

        var endResult = await endResponse.Content.ReadAsStringAsync();
        var gameResult = JsonSerializer.Deserialize<GameEndResult>(endResult, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.NotNull(gameResult);
        Assert.True(gameResult.Success);
        Assert.Equal(endGameRequest.FinalScore, gameResult.FinalScore);
        Assert.Equal(endGameRequest.SurvivalTime, gameResult.SurvivalTime);
        Assert.True(gameResult.ScoreSubmitted);
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "GameplayIntegration")]
    public async Task GameSession_WithTimeout_ShouldEndAutomatically()
    {
        // Act: Start a game session
        var startResponse = await _client.PostAsync("/api/game/start", null);
        var sessionData = await startResponse.Content.ReadAsStringAsync();
        var session = JsonSerializer.Deserialize<GameSession>(sessionData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Act: Get session status after timeout (simulate)
        var statusResponse = await _client.GetAsync($"/api/game/status/{session!.SessionId}");

        // Assert: Session should have timeout handling
        Assert.Equal(System.Net.HttpStatusCode.OK, statusResponse.StatusCode);

        var statusData = await statusResponse.Content.ReadAsStringAsync();
        Assert.Contains("timeRemaining", statusData.ToLower());
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "GameplayIntegration")]
    public async Task MultipleBlockPlacements_WithPhysicsValidation_ShouldMaintainConsistency()
    {
        // Arrange: Start game session
        var startResponse = await _client.PostAsync("/api/game/start", null);
        var sessionData = await startResponse.Content.ReadAsStringAsync();
        var session = JsonSerializer.Deserialize<GameSession>(sessionData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var placements = new[]
        {
            new { BlockType = "Square", Position = new { X = 400, Y = 500 }, Rotation = 0 },
            new { BlockType = "Triangle", Position = new { X = 400, Y = 450 }, Rotation = 0 },
            new { BlockType = "Circle", Position = new { X = 400, Y = 400 }, Rotation = 0 }
        };

        var scores = new List<int>();

        foreach (var placement in placements)
        {
            // Act: Place block
            var blockRequest = new
            {
                SessionId = session!.SessionId,
                BlockType = placement.BlockType,
                Position = placement.Position,
                Rotation = placement.Rotation,
                Timestamp = DateTime.UtcNow
            };

            var response = await _client.PostAsJsonAsync("/api/game/place-block", blockRequest);

            // Assert: Each placement should succeed
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            var resultData = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<BlockPlacementResult>(resultData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            Assert.NotNull(result);
            Assert.True(result.Success);
            scores.Add(result.Score);
        }

        // Assert: Score should increase or stay same (never decrease)
        for (int i = 1; i < scores.Count; i++)
        {
            Assert.True(scores[i] >= scores[i - 1], "Score should not decrease during gameplay");
        }
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "GameplayIntegration")]
    public async Task GameOverCondition_WhenBlocksReachTopLine_ShouldEndGame()
    {
        // Arrange: Start game session
        var startResponse = await _client.PostAsync("/api/game/start", null);
        var sessionData = await startResponse.Content.ReadAsStringAsync();
        var session = JsonSerializer.Deserialize<GameSession>(sessionData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Act: Place blocks near top line to trigger game over
        var dangerousPlacement = new
        {
            SessionId = session!.SessionId,
            BlockType = "Square",
            Position = new { X = 400, Y = 50 }, // Very high position
            Rotation = 0,
            Timestamp = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync("/api/game/place-block", dangerousPlacement);

        // Assert: Game over should be detected
        var resultData = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BlockPlacementResult>(resultData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.NotNull(result);
        // Game over detection should work properly
        Assert.True(result.GameOver || !result.Success, "High block placement should trigger game over detection");
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "GameplayIntegration")]
    public async Task ConcurrentGameSessions_ShouldBeIsolated()
    {
        // Act: Start two game sessions simultaneously
        var session1Task = _client.PostAsync("/api/game/start", null);
        var session2Task = _client.PostAsync("/api/game/start", null);

        var responses = await Task.WhenAll(session1Task, session2Task);

        var session1Data = await responses[0].Content.ReadAsStringAsync();
        var session2Data = await responses[1].Content.ReadAsStringAsync();

        var session1 = JsonSerializer.Deserialize<GameSession>(session1Data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var session2 = JsonSerializer.Deserialize<GameSession>(session2Data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Assert: Sessions should be independent
        Assert.NotEqual(session1!.SessionId, session2!.SessionId);

        // Act: Place blocks in both sessions
        var placement1 = new
        {
            SessionId = session1.SessionId,
            BlockType = "Square",
            Position = new { X = 400, Y = 400 },
            Rotation = 0,
            Timestamp = DateTime.UtcNow
        };

        var placement2 = new
        {
            SessionId = session2.SessionId,
            BlockType = "Triangle",
            Position = new { X = 300, Y = 300 },
            Rotation = 45,
            Timestamp = DateTime.UtcNow
        };

        var result1Task = _client.PostAsJsonAsync("/api/game/place-block", placement1);
        var result2Task = _client.PostAsJsonAsync("/api/game/place-block", placement2);

        var results = await Task.WhenAll(result1Task, result2Task);

        // Assert: Both sessions should handle requests independently
        Assert.Equal(System.Net.HttpStatusCode.OK, results[0].StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, results[1].StatusCode);
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "GameplayIntegration")]
    public async Task InvalidSessionId_ShouldReturnNotFound()
    {
        // Act: Try to place block with invalid session ID
        var invalidPlacement = new
        {
            SessionId = "invalid-session-id",
            BlockType = "Square",
            Position = new { X = 400, Y = 400 },
            Rotation = 0,
            Timestamp = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync("/api/game/place-block", invalidPlacement);

        // Assert: Should return 404 for invalid session
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "GameplayIntegration")]
    public async Task ExpiredSession_ShouldNotAcceptNewBlocks()
    {
        // This test would require time manipulation or shorter session duration
        // For now, we'll test the contract for expired session handling

        // Act: Try to get status of expired session
        var response = await _client.GetAsync("/api/game/status/expired-session-id");

        // Assert: Should handle expired sessions gracefully
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "GameplayIntegration")]
    public async Task GameStatistics_ShouldTrackAccurateMetrics()
    {
        // Act: Get game statistics
        var response = await _client.GetAsync("/api/game/statistics");

        // Assert: Statistics endpoint should exist and return proper structure
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var statsData = await response.Content.ReadAsStringAsync();
        var stats = JsonSerializer.Deserialize<GameStatistics>(statsData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.NotNull(stats);
        Assert.True(stats.TotalGamesPlayed >= 0);
        Assert.True(stats.AverageGameDuration >= 0);
        Assert.True(stats.HighestScore >= 0);
        Assert.True(stats.TotalBlocksPlaced >= 0);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    // Data models for test validation
    private class GameSession
    {
        public string SessionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Score { get; set; }
        public int TimeRemaining { get; set; }
    }

    private class BlockPlacementResult
    {
        public bool Success { get; set; }
        public int Score { get; set; }
        public bool GameOver { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    private class GameEndResult
    {
        public bool Success { get; set; }
        public int FinalScore { get; set; }
        public double SurvivalTime { get; set; }
        public bool ScoreSubmitted { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    private class GameStatistics
    {
        public int TotalGamesPlayed { get; set; }
        public double AverageGameDuration { get; set; }
        public int HighestScore { get; set; }
        public int TotalBlocksPlaced { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}