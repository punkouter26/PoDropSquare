using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Po.PoDropSquare.Api.Tests;

/// <summary>
/// Tests for client-side logging endpoints (/api/log/client and /api/log/error)
/// </summary>
public class LogControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public LogControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task POST_LogClient_WithValidMessage_ShouldReturn200()
    {
        // Arrange
        var logEntry = new
        {
            level = "info",
            message = "Test log message from client",
            timestamp = DateTime.UtcNow.ToString("O"),
            url = "http://localhost/test",
            data = "Additional context data"
        };

        var json = JsonSerializer.Serialize(logEntry);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/log/client", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(responseObject.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
        Assert.True(responseObject.TryGetProperty("timestamp", out _));
    }

    [Theory]
    [InlineData("trace")]
    [InlineData("debug")]
    [InlineData("information")]
    [InlineData("warning")]
    [InlineData("error")]
    [InlineData("critical")]
    public async Task POST_LogClient_WithDifferentLogLevels_ShouldSucceed(string logLevel)
    {
        // Arrange
        var logEntry = new
        {
            level = logLevel,
            message = $"Test {logLevel} message",
            timestamp = DateTime.UtcNow.ToString("O"),
            url = "http://localhost/test"
        };

        var json = JsonSerializer.Serialize(logEntry);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/log/client", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task POST_LogClient_WithNullData_ShouldReturn400()
    {
        // Arrange
        var content = new StringContent("null", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/log/client", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_LogError_WithValidError_ShouldReturn200()
    {
        // Arrange
        var errorEntry = new
        {
            message = "Uncaught TypeError: Cannot read property 'x' of null",
            filename = "app.js",
            lineNumber = 42,
            columnNumber = 15,
            stack = "Error: Test error\n    at Object.<anonymous> (app.js:42:15)",
            timestamp = DateTime.UtcNow.ToString("O"),
            url = "http://localhost/test"
        };

        var json = JsonSerializer.Serialize(errorEntry);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/log/error", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(responseObject.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task POST_LogError_WithMinimalData_ShouldSucceed()
    {
        // Arrange
        var errorEntry = new
        {
            message = "Simple error",
            filename = "",
            lineNumber = 0,
            columnNumber = 0,
            stack = "",
            timestamp = DateTime.UtcNow.ToString("O"),
            url = "http://localhost/test"
        };

        var json = JsonSerializer.Serialize(errorEntry);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/log/error", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task POST_LogError_WithNullData_ShouldReturn400()
    {
        // Arrange
        var content = new StringContent("null", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/log/error", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_LogClient_ShouldHandleUserAgentHeader()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 Test Browser");

        var logEntry = new
        {
            level = "info",
            message = "Test with user agent",
            timestamp = DateTime.UtcNow.ToString("O"),
            url = "http://localhost/test"
        };

        var json = JsonSerializer.Serialize(logEntry);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/log/client", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Clean up
        _client.DefaultRequestHeaders.Clear();
    }

    [Fact]
    public async Task POST_LogClient_MultipleConcurrent_ShouldAllSucceed()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < 10; i++)
        {
            var logEntry = new
            {
                level = "info",
                message = $"Concurrent log message {i}",
                timestamp = DateTime.UtcNow.ToString("O"),
                url = "http://localhost/test"
            };

            var json = JsonSerializer.Serialize(logEntry);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            tasks.Add(_client.PostAsync("/api/log/client", content));
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

    [Fact]
    public async Task POST_LogClient_ResponseTime_ShouldBeFast()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var logEntry = new
        {
            level = "info",
            message = "Performance test log",
            timestamp = DateTime.UtcNow.ToString("O"),
            url = "http://localhost/test"
        };

        var json = JsonSerializer.Serialize(logEntry);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/log/client", content);
        stopwatch.Stop();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 100,
            $"Log endpoint took {stopwatch.ElapsedMilliseconds}ms, should be under 100ms");
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
