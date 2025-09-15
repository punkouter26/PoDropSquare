using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Po.PoDropSquare.Core.Contracts;

namespace Po.PoDropSquare.Api.Middleware;

/// <summary>
/// Rate limiting middleware to prevent abuse and spam submissions.
/// Implements sliding window rate limiting with IP-based tracking.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitOptions _options;

    // Thread-safe dictionary to track request counts per IP
    private static readonly ConcurrentDictionary<string, ClientRequestInfo> _clientRequests = new();

    // Timer to periodically clean up expired entries
    private readonly Timer _cleanupTimer;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _options = configuration.GetSection("RateLimit").Get<RateLimitOptions>() ?? new RateLimitOptions();

        // Clean up expired entries every minute
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply rate limiting to score submission endpoints
        if (!ShouldApplyRateLimit(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        var now = DateTime.UtcNow;

        // Get or create client request info
        var clientInfo = _clientRequests.GetOrAdd(clientId, _ => new ClientRequestInfo
        {
            FirstRequestTime = now,
            RequestTimes = new List<DateTime>()
        });

        bool rateLimitExceeded;
        int remainingRequests;

        lock (clientInfo.RequestTimes)
        {
            // Remove requests outside the sliding window
            clientInfo.RequestTimes.RemoveAll(time => now - time > _options.WindowDuration);

            // Check if rate limit is exceeded
            rateLimitExceeded = clientInfo.RequestTimes.Count >= _options.MaxRequests;
            remainingRequests = Math.Max(0, _options.MaxRequests - clientInfo.RequestTimes.Count);

            if (!rateLimitExceeded)
            {
                // Add current request
                clientInfo.RequestTimes.Add(now);
                clientInfo.LastRequestTime = now;
            }
        }

        if (rateLimitExceeded)
        {
            _logger.LogWarning(
                "Rate limit exceeded for client {ClientId}. {RequestCount} requests in {Window} seconds",
                clientId, clientInfo.RequestTimes.Count, _options.WindowDuration.TotalSeconds);

            await HandleRateLimitExceeded(context, clientInfo);
            return;
        }

        // Add rate limit headers
        AddRateLimitHeaders(context, clientInfo);

        // Continue to next middleware
        await _next(context);
    }

    private bool ShouldApplyRateLimit(PathString path)
    {
        // Apply rate limiting to score submission endpoints
        return path.StartsWithSegments("/api/scores", StringComparison.OrdinalIgnoreCase) &&
               !path.StartsWithSegments("/api/scores/top", StringComparison.OrdinalIgnoreCase);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Use X-Forwarded-For header if available (for load balancers/proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP in the chain
            var ip = forwardedFor.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip))
            {
                return ip;
            }
        }

        // Fall back to direct connection IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private async Task HandleRateLimitExceeded(HttpContext context, ClientRequestInfo clientInfo)
    {
        var retryAfter = CalculateRetryAfter(clientInfo);

        context.Response.StatusCode = 429; // Too Many Requests
        context.Response.ContentType = "application/json";
        context.Response.Headers["Retry-After"] = retryAfter.ToString();
        context.Response.Headers["X-RateLimit-Limit"] = _options.MaxRequests.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = "0";
        context.Response.Headers["X-RateLimit-Reset"] = GetResetTime(clientInfo).ToString();

        var errorResponse = new ErrorResponse
        {
            Error = "Rate limit exceeded",
            Message = $"Too many requests. Maximum {_options.MaxRequests} requests allowed per {_options.WindowDuration.TotalSeconds} seconds.",
            Details = $"Retry after {retryAfter} seconds. Window: {_options.WindowDuration.TotalSeconds}s, Max: {_options.MaxRequests}"
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json, Encoding.UTF8);
    }

    private void AddRateLimitHeaders(HttpContext context, ClientRequestInfo clientInfo)
    {
        var remaining = Math.Max(0, _options.MaxRequests - clientInfo.RequestTimes.Count);
        var resetTime = GetResetTime(clientInfo);

        context.Response.Headers["X-RateLimit-Limit"] = _options.MaxRequests.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = resetTime.ToString();
    }

    private int CalculateRetryAfter(ClientRequestInfo clientInfo)
    {
        if (clientInfo.RequestTimes.Count == 0)
            return (int)_options.WindowDuration.TotalSeconds;

        var oldestRequest = clientInfo.RequestTimes.Min();
        var timeUntilExpiry = oldestRequest.Add(_options.WindowDuration) - DateTime.UtcNow;

        return Math.Max(1, (int)timeUntilExpiry.TotalSeconds);
    }

    private long GetResetTime(ClientRequestInfo clientInfo)
    {
        if (clientInfo.RequestTimes.Count == 0)
            return DateTimeOffset.UtcNow.Add(_options.WindowDuration).ToUnixTimeSeconds();

        var oldestRequest = clientInfo.RequestTimes.Min();
        var resetTime = oldestRequest.Add(_options.WindowDuration);

        return new DateTimeOffset(resetTime).ToUnixTimeSeconds();
    }

    private void CleanupExpiredEntries(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = new List<string>();

        foreach (var kvp in _clientRequests)
        {
            var clientInfo = kvp.Value;

            // Clean up if no recent activity
            if (now - clientInfo.LastRequestTime > _options.WindowDuration.Add(TimeSpan.FromMinutes(5)))
            {
                expiredKeys.Add(kvp.Key);
            }
            else
            {
                // Clean up old request times within active clients
                lock (clientInfo.RequestTimes)
                {
                    clientInfo.RequestTimes.RemoveAll(time => now - time > _options.WindowDuration);
                }
            }
        }

        // Remove expired client entries
        foreach (var key in expiredKeys)
        {
            _clientRequests.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} expired rate limit entries", expiredKeys.Count);
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}

/// <summary>
/// Configuration options for rate limiting.
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Maximum number of requests allowed within the time window.
    /// </summary>
    public int MaxRequests { get; set; } = 10;

    /// <summary>
    /// Time window for rate limiting (sliding window).
    /// </summary>
    public TimeSpan WindowDuration { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Tracks request information for a specific client.
/// </summary>
internal class ClientRequestInfo
{
    public DateTime FirstRequestTime { get; set; }
    public DateTime LastRequestTime { get; set; }
    public List<DateTime> RequestTimes { get; set; } = new();
}