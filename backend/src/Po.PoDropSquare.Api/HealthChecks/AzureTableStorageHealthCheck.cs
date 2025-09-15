using Azure.Data.Tables;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace Po.PoDropSquare.Api.HealthChecks;

/// <summary>
/// Health check for Azure Table Storage connectivity and performance
/// </summary>
public class AzureTableStorageHealthCheck : IHealthCheck
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly ILogger<AzureTableStorageHealthCheck> _logger;

    public AzureTableStorageHealthCheck(
        TableServiceClient tableServiceClient,
        ILogger<AzureTableStorageHealthCheck> logger)
    {
        _tableServiceClient = tableServiceClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Starting Azure Table Storage health check");

            // Check if we can connect to the service
            var properties = await _tableServiceClient.GetPropertiesAsync(cancellationToken);

            stopwatch.Stop();
            var responseTime = stopwatch.ElapsedMilliseconds;

            // Validate response time thresholds
            var status = responseTime switch
            {
                <= 100 => HealthStatus.Healthy,
                <= 500 => HealthStatus.Degraded,
                _ => HealthStatus.Unhealthy
            };

            var data = new Dictionary<string, object>
            {
                ["responseTime"] = responseTime,
                ["endpoint"] = _tableServiceClient.Uri.ToString(),
                ["connected"] = true,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            };

            var description = status switch
            {
                HealthStatus.Healthy => $"Azure Table Storage is healthy (response time: {responseTime}ms)",
                HealthStatus.Degraded => $"Azure Table Storage is degraded (slow response: {responseTime}ms)",
                _ => $"Azure Table Storage is unhealthy (timeout: {responseTime}ms)"
            };

            _logger.LogDebug(
                "Azure Table Storage health check completed: {Status} in {ResponseTime}ms",
                status,
                responseTime);

            return new HealthCheckResult(status, description, data: data);
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("Azure Table Storage health check was cancelled after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return new HealthCheckResult(
                HealthStatus.Unhealthy,
                "Azure Table Storage health check was cancelled",
                data: new Dictionary<string, object>
                {
                    ["responseTime"] = stopwatch.ElapsedMilliseconds,
                    ["error"] = "Operation was cancelled"
                });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Azure Table Storage health check failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return new HealthCheckResult(
                HealthStatus.Unhealthy,
                $"Azure Table Storage is unavailable: {ex.Message}",
                ex,
                data: new Dictionary<string, object>
                {
                    ["responseTime"] = stopwatch.ElapsedMilliseconds,
                    ["error"] = ex.Message,
                    ["exceptionType"] = ex.GetType().Name
                });
        }
    }
}