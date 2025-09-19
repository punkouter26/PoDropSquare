using Serilog;
using Azure.Data.Tables;
using Po.PoDropSquare.Data.Repositories;
using Po.PoDropSquare.Services.Services;
using Po.PoDropSquare.Api.HealthChecks;
using Po.PoDropSquare.Api.Middleware;

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
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault() ?? "Unknown");
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
