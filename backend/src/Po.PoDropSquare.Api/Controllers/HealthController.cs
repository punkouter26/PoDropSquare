using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Po.PoDropSquare.Core.Contracts;
using System.Diagnostics;

namespace Po.PoDropSquare.Api.Controllers;

/// <summary>
/// Health check controller providing system status and dependency health information
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Route("health")] // Keep backward compatibility
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(HealthCheckService healthCheckService, ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the overall health status of the application and its dependencies
    /// </summary>
    /// <returns>Health status with detailed dependency information</returns>
    /// <response code="200">System is healthy</response>
    /// <response code="503">System or dependencies are unhealthy</response>
    [HttpGet]
    [HttpHead]
    [HttpOptions]
    [ProducesResponseType(typeof(HealthCheckResponse), 200)]
    [ProducesResponseType(typeof(HealthCheckResponse), 503)]
    public async Task<IActionResult> GetHealth()
    {
        var stopwatch = Stopwatch.StartNew();

        // OPTIONS requests should return allowed methods
        if (HttpContext.Request.Method == "OPTIONS")
        {
            Response.Headers["Allow"] = "GET,HEAD,OPTIONS";
            Response.Headers["Cache-Control"] = "no-cache";
            return Ok(new { message = "Health check endpoint supports GET, HEAD, and OPTIONS methods" });
        }

        try
        {
            _logger.LogDebug("Starting health check from IP {RemoteIpAddress}",
                HttpContext.Connection.RemoteIpAddress);

            // Run all registered health checks
            var healthReport = await _healthCheckService.CheckHealthAsync();

            stopwatch.Stop();

            var response = new HealthCheckResponse
            {
                Status = MapHealthStatus(healthReport.Status),
                Version = GetApplicationVersion(),
                Timestamp = DateTime.UtcNow,
                Dependencies = healthReport.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => new DependencyHealth
                    {
                        Status = MapHealthStatus(entry.Value.Status),
                        ResponseTime = (int)entry.Value.Duration.TotalMilliseconds,
                        Details = entry.Value.Exception?.Message ?? entry.Value.Description
                    }
                ),
                Uptime = Environment.TickCount64,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            };

            // Set appropriate cache headers
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["X-Health-Check-Duration"] = stopwatch.ElapsedMilliseconds.ToString();

            // HEAD requests should return status with no body
            if (HttpContext.Request.Method == "HEAD")
            {
                var statusCode = healthReport.Status == HealthStatus.Healthy ? 200 : 503;
                return StatusCode(statusCode);
            }

            // Log the health check result
            _logger.LogInformation(
                "Health check completed: {Status} in {Duration}ms with {CheckCount} checks",
                response.Status,
                stopwatch.ElapsedMilliseconds,
                response.Dependencies.Count);

            // Return appropriate status code based on health
            if (healthReport.Status == HealthStatus.Healthy)
            {
                return Ok(response);
            }
            else
            {
                _logger.LogWarning(
                    "Health check failed: {Status}. Failed checks: {FailedChecks}",
                    response.Status,
                    string.Join(", ", response.Dependencies.Where(e => e.Value.Status != "Healthy").Select(e => e.Key)));

                return StatusCode(503, response);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Health check failed with exception after {Duration}ms", stopwatch.ElapsedMilliseconds);

            var errorResponse = new HealthCheckResponse
            {
                Status = "Critical",
                Version = GetApplicationVersion(),
                Timestamp = DateTime.UtcNow,
                Dependencies = new Dictionary<string, DependencyHealth>
                {
                    ["System"] = new DependencyHealth
                    {
                        Status = "Critical",
                        ResponseTime = (int)stopwatch.ElapsedMilliseconds,
                        Details = $"Health check system failure: {ex.Message}"
                    }
                },
                Uptime = Environment.TickCount64,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            };

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["X-Health-Check-Duration"] = stopwatch.ElapsedMilliseconds.ToString();

            return StatusCode(503, errorResponse);
        }
    }

    /// <summary>
    /// Gets a simplified health status for load balancers and monitoring tools
    /// </summary>
    /// <returns>Simple OK or Error status</returns>
    [HttpGet("simple")]
    public async Task<IActionResult> GetSimpleHealth()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();

            if (healthReport.Status == HealthStatus.Healthy)
            {
                return Ok("OK");
            }
            else
            {
                return StatusCode(503, "Error");
            }
        }
        catch
        {
            return StatusCode(503, "Error");
        }
    }

    /// <summary>
    /// Maps HealthStatus enum to string
    /// </summary>
    private static string MapHealthStatus(HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Healthy => "Healthy",
            HealthStatus.Degraded => "Degraded",
            HealthStatus.Unhealthy => "Unhealthy",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets application version information
    /// </summary>
    private static string GetApplicationVersion()
    {
        var assembly = typeof(HealthController).Assembly;
        var version = assembly.GetName().Version;
        var informationalVersion = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .FirstOrDefault() as System.Reflection.AssemblyInformationalVersionAttribute;

        return informationalVersion?.InformationalVersion ?? version?.ToString() ?? "unknown";
    }
}
