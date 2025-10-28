using Serilog;
using Azure.Data.Tables;
using Po.PoDropSquare.Data.Repositories;
using Po.PoDropSquare.Services.Services;
using Po.PoDropSquare.Api.HealthChecks;
using Po.PoDropSquare.Api.Middleware;

/*
 * ┌─────────────────────────────────────────────────────────────────────────────┐
 * │ APPLICATION INSIGHTS KQL QUERIES                                            │
 * │ For detailed monitoring queries, see: /docs/KQL-QUERIES.md                 │
 * └─────────────────────────────────────────────────────────────────────────────┘
 * 
 * Quick Reference - Run these in Application Insights → Logs:
 * 
 * 1. TOP 10 SLOWEST API REQUESTS (Last 24h)
 *    requests
 *    | where timestamp > ago(24h)
 *    | summarize P95Duration = percentile(duration, 95), Count = count() by name
 *    | order by P95Duration desc | take 10
 * 
 * 2. ERROR RATE BY HOUR
 *    requests
 *    | where timestamp > ago(24h)
 *    | summarize Total = count(), Failed = countif(success == false) by bin(timestamp, 1h)
 *    | extend ErrorRate = (Failed * 100.0) / Total
 *    | render timechart
 * 
 * 3. ACTIVE USERS
 *    requests
 *    | where timestamp > ago(24h)
 *    | summarize ActiveUsers = dcount(user_Id) by bin(timestamp, 1h)
 *    | render timechart
 * 
 * 4. JAVASCRIPT ERRORS (Client-side)
 *    traces
 *    | where timestamp > ago(24h)
 *    | where customDimensions.CategoryName contains "ClientError"
 *    | project timestamp, message, severityLevel
 *    | order by timestamp desc | take 100
 * 
 * 5. HEALTH CHECK MONITORING
 *    requests
 *    | where timestamp > ago(24h) and name contains "health"
 *    | summarize Failures = countif(success == false) by bin(timestamp, 5m)
 *    | render timechart
 * 
 * See /docs/KQL-QUERIES.md for 31 comprehensive queries covering:
 * - User Activity & Engagement (DAU, sessions, geographic)
 * - Performance Monitoring (latency, dependencies, client-side)
 * - Error Tracking (exceptions, failed requests, JS errors)
 * - Game Metrics (scores, leaderboard, player ranks)
 * - Business Intelligence (retention, conversion funnel)
 * - Alerts (error rate, slow performance, dependency failures)
 */

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog from configuration
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

try
{
    Log.Information("Starting PoDropSquare API");

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add memory caching
    builder.Services.AddMemoryCache();

    // Add response caching
    builder.Services.AddResponseCaching();

    // Configure Azure Table Storage
    var tableStorageConnectionString = builder.Configuration.GetConnectionString("AzureTableStorage")
        ?? "UseDevelopmentStorage=true"; // Default to Azurite for development

    builder.Services.AddSingleton(serviceProvider =>
    {
        return new TableServiceClient(tableStorageConnectionString);
    });

    // Register repositories
    builder.Services.AddScoped<IScoreRepository, ScoreRepository>();
    builder.Services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();

    // Register services
    builder.Services.AddScoped<IScoreService, ScoreService>();
    builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();

    // Add comprehensive health checks
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running"))
        .AddCheck<AzureTableStorageHealthCheck>("azure-table-storage")
        .AddCheck("memory", () =>
        {
            var workingSet = Environment.WorkingSet / 1024 / 1024; // MB
            return workingSet < 500
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"Working set: {workingSet} MB")
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded($"High memory usage: {workingSet} MB");
        });

    // Add HTTP logging
    builder.Services.AddHttpLogging(logging =>
    {
        logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPropertiesAndHeaders |
                               Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponsePropertiesAndHeaders |
                               Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestBody |
                               Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponseBody;
        logging.RequestBodyLogLimit = 4096;
        logging.ResponseBodyLogLimit = 4096;
        logging.RequestHeaders.Add("User-Agent");
        logging.RequestHeaders.Add("Referer");
        logging.MediaTypeOptions.AddText("application/json");
    });

    var app = builder.Build();

    // Initialize repositories (ensure tables exist)
    using (var scope = app.Services.CreateScope())
    {
        var scoreRepository = scope.ServiceProvider.GetRequiredService<IScoreRepository>();
        var leaderboardRepository = scope.ServiceProvider.GetRequiredService<ILeaderboardRepository>();

        if (scoreRepository is ScoreRepository scoreRepo)
        {
            await scoreRepo.InitializeAsync();
        }

        if (leaderboardRepository is LeaderboardRepository leaderboardRepo)
        {
            await leaderboardRepo.InitializeAsync();
        }

        Log.Information("Azure Table Storage repositories initialized");
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Add global error handling middleware (must be early in pipeline)
    app.UseMiddleware<ErrorHandlingMiddleware>();

    // Add HTTP logging middleware (after error handling for proper context)
    app.UseHttpLogging();

    // Add rate limiting middleware (after error handling, before routing)
    app.UseMiddleware<RateLimitingMiddleware>();

    app.UseHttpsRedirection();

    // Configure static files for Blazor WASM
    app.UseBlazorFrameworkFiles();
    app.UseStaticFiles();

    // Add response caching middleware
    app.UseResponseCaching();

    // Add Serilog request logging (after error handling for proper context)
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            var requestHost = httpContext.Request.Host.Value ?? "Unknown";
            diagnosticContext.Set("RequestHost", requestHost);
            var userAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault() ?? "Unknown";
            diagnosticContext.Set("UserAgent", userAgent);
            diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
        };
    });

    app.UseRouting();
    app.MapControllers();
    app.MapHealthChecks("/health");

    // Fallback route for Blazor WASM client-side routing
    app.MapFallbackToFile("index.html");

    Log.Information("PoDropSquare API configured successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible to tests
public partial class Program { }
