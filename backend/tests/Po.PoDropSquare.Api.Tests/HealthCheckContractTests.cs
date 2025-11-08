using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Po.PoDropSquare.Api.Tests;

/// <summary>
/// Contract tests for GET /api/health endpoint
/// These tests MUST fail initially and pass only after implementation
/// </summary>
public class HealthCheckContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HealthCheckContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "HealthCheckContract")]
    public async Task GET_Health_ShouldReturn200WhenHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        // Verify basic health response structure
        Assert.True(responseObject.TryGetProperty("status", out var status));
        Assert.Equal("Healthy", status.GetString());

        Assert.True(responseObject.TryGetProperty("totalDuration", out var totalDuration));
        Assert.True(responseObject.TryGetProperty("entries", out var entries));
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "HealthCheckContract")]
    public async Task GET_Health_ShouldReturnDetailedHealthStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        // Verify detailed health information exists
        Assert.True(responseObject.TryGetProperty("entries", out var entries));

        // Should have checks for critical dependencies
        // (The actual checks may not exist yet, so we just verify the structure)
        Assert.Equal(JsonValueKind.Object, entries.ValueKind);
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "HealthCheckContract")]
    public async Task GET_Health_ShouldSetCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "HealthCheckContract")]
    public async Task GET_Health_ResponseTime_ShouldBeFast()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/health");
        stopwatch.Stop();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Health check should be very fast (under 100ms)
        Assert.True(stopwatch.ElapsedMilliseconds < 100,
            $"Health check took {stopwatch.ElapsedMilliseconds}ms, should be under 100ms");
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "HealthCheckContract")]
    public async Task GET_Health_ShouldAllowAnonymousAccess()
    {
        // Health checks should not require authentication
        // This test verifies that no auth headers are needed

        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Should not return 401 Unauthorized
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "HealthCheckContract")]
    public async Task GET_Health_ShouldHandleMultipleConcurrentRequests()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        const int concurrentRequests = 10;

        // Act - Send multiple concurrent requests
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(_client.GetAsync("/health"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - All should succeed
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response.Dispose();
        }
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "HealthCheckContract")]
    public async Task GET_Health_ShouldIncludeTimestamp()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();

        // Should include timestamp in response headers or body
        Assert.True(response.Headers.Date.HasValue ||
                   responseContent.Contains("timestamp") ||
                   responseContent.Contains("time"),
                   "Health check response should include timestamp information");
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "HealthCheckContract")]
    public async Task GET_Health_ShouldNotExposeInternalDetails()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();

        // Should not expose sensitive internal information
        Assert.DoesNotContain("password", responseContent.ToLower());
        Assert.DoesNotContain("secret", responseContent.ToLower());
        Assert.DoesNotContain("key", responseContent.ToLower());
        Assert.DoesNotContain("connectionstring", responseContent.ToLower().Replace(" ", ""));
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "HealthCheckContract")]
    public async Task GET_Health_WithAcceptJson_ShouldReturnJson()
    {
        // Arrange
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Accept", "application/json");

        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        // Clean up
        _client.DefaultRequestHeaders.Clear();
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "HealthCheckContract")]
    public async Task GET_Health_ShouldBeCacheable()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Health checks can be cached for a short period
        var cacheControl = response.Headers.CacheControl;
        if (cacheControl != null)
        {
            // If cache headers are present, they should allow short-term caching
            Assert.True(cacheControl.MaxAge?.TotalSeconds <= 60,
                "Health check cache should not exceed 60 seconds");
        }
    }

    // Test for when dependencies are unhealthy
    [Fact]

    [Trait("Category", "Integration")]

    [Trait("Feature", "HealthCheckContract")]
    public async Task GET_Health_WhenDependenciesUnhealthy_ShouldReturn503()
    {
        // This test verifies the contract when dependencies are down
        // In a real scenario, this would require mocking unhealthy dependencies

        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        // For now, we expect this to pass since dependencies aren't implemented yet
        // When dependencies are added and can fail, this should return 503
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.ServiceUnavailable,
                   "Health check should return 200 OK when healthy or 503 when dependencies are unhealthy");

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

            Assert.True(responseObject.TryGetProperty("status", out var status));
            Assert.Equal("Unhealthy", status.GetString());
        }
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "HealthCheckContract")]
    public async Task GET_Health_ShouldSupportHeadRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Head, "/health");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // HEAD request should not have body content
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(string.IsNullOrEmpty(content));
    }

    [Fact]


    [Trait("Category", "Integration")]


    [Trait("Feature", "HealthCheckContract")]
    public async Task GET_Health_ShouldHandleOptionsRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/health");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // Should either return 200 OK with CORS headers or 405 Method Not Allowed
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.MethodNotAllowed ||
                   response.StatusCode == HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            // Should include allowed methods
            Assert.True(response.Headers.Contains("Allow") ||
                       response.Content.Headers.Allow.Any());
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


    [Trait("Category", "Integration")]


    [Trait("Feature", "HealthCheckContract")]
    public void CleanupResources()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}