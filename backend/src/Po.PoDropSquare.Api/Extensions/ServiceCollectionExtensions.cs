using Azure.Data.Tables;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Po.PoDropSquare.Api.HealthChecks;
using Po.PoDropSquare.Api.Telemetry;
using Po.PoDropSquare.Data.Repositories;
using Po.PoDropSquare.Services.Services;

namespace Po.PoDropSquare.Api.Extensions;

/// <summary>
/// Extension methods for configuring application services in DI container
/// Follows SOLID principles by extracting service configuration from Program.cs
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core application services (business logic layer)
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IScoreService, ScoreService>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddMemoryCache();
        services.AddResponseCaching();

        return services;
    }

    /// <summary>
    /// Adds data repositories and Azure Table Storage client
    /// </summary>
    public static IServiceCollection AddDataRepositories(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(_ => new TableServiceClient(connectionString));
        services.AddScoped<IScoreRepository, ScoreRepository>();
        services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();

        return services;
    }

    /// <summary>
    /// Adds telemetry services (Application Insights + OpenTelemetry)
    /// </summary>
    public static IServiceCollection AddTelemetryServices(this IServiceCollection services, IConfiguration configuration)
    {
        var appInsightsConnectionString = configuration.GetConnectionString("ApplicationInsights");

        // Register custom metrics
        services.AddSingleton<PoDropSquareMetrics>();

        if (!string.IsNullOrEmpty(appInsightsConnectionString))
        {
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
                options.EnableAdaptiveSampling = true;
                options.EnableQuickPulseMetricStream = true; // Live Metrics
                options.EnablePerformanceCounterCollectionModule = true;
                options.EnableDependencyTrackingTelemetryModule = true;
            });

            // Register custom telemetry initializer
            services.AddSingleton<ITelemetryInitializer, PoDropSquareTelemetryInitializer>();
        }

        return services;
    }

    /// <summary>
    /// Adds health check services
    /// </summary>
    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running"))
            .AddCheck<AzureTableStorageHealthCheck>("azure-table-storage")
            .AddCheck("memory", () =>
            {
                var workingSet = Environment.WorkingSet / 1024 / 1024; // MB
                return workingSet < 500
                    ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"Working set: {workingSet} MB")
                    : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded($"High memory usage: {workingSet} MB");
            });

        return services;
    }
}
