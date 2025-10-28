using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Json;

namespace Po.PoDropSquare.Blazor.Services;

/// <summary>
/// Custom logger provider that sends logs to the server API
/// </summary>
public class RemoteLoggerProvider : ILoggerProvider
{
    private readonly IServiceProvider _serviceProvider;

    public RemoteLoggerProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new RemoteLogger(categoryName, _serviceProvider);
    }

    public void Dispose()
    {
        // HttpClient is managed by DI container
    }
}

/// <summary>
/// Logger that sends log entries to the server via HTTP
/// </summary>
public class RemoteLogger : ILogger
{
    private readonly string _categoryName;
    private readonly IServiceProvider _serviceProvider;

    public RemoteLogger(string categoryName, IServiceProvider serviceProvider)
    {
        _categoryName = categoryName;
        _serviceProvider = serviceProvider;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Information; // Only send Information and above to server
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        try
        {
            var message = formatter(state, exception);

            if (exception != null)
            {
                message += $" Exception: {exception}";
            }

            var logEntry = new ClientLogEntry
            {
                Level = GetLogLevelString(logLevel),
                Message = $"[{_categoryName}] {message}",
                Data = state?.ToString(),
                Timestamp = DateTime.UtcNow,
                Url = GetCurrentUrl()
            };

            // Send asynchronously without blocking
            _ = Task.Run(async () => await SendLogEntryAsync(logEntry));
        }
        catch
        {
            // Fail silently to avoid cascading failures
        }
    }

    private async Task SendLogEntryAsync(ClientLogEntry logEntry)
    {
        try
        {
            // Create a new HttpClient for this request to avoid scoping issues
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:5000/");
            var response = await httpClient.PostAsJsonAsync("/api/log/client", logEntry);
            // Don't throw on failure - logging should be fire-and-forget
        }
        catch
        {
            // Fail silently
        }
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "trace",
            LogLevel.Debug => "debug",
            LogLevel.Information => "information",
            LogLevel.Warning => "warning",
            LogLevel.Error => "error",
            LogLevel.Critical => "critical",
            _ => "information"
        };
    }

    private string GetCurrentUrl()
    {
        try
        {
            // Try to get the current URL from the navigation manager
            var navigationManager = _serviceProvider.GetService<Microsoft.AspNetCore.Components.NavigationManager>();
            return navigationManager?.Uri ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }
}

/// <summary>
/// Client log entry model matching the server-side contract
/// </summary>
public class ClientLogEntry
{
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Data { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Url { get; set; }
}

/// <summary>
/// Client error entry model matching the server-side contract
/// </summary>
public class ClientErrorEntry
{
    public string Message { get; set; } = string.Empty;
    public string? Filename { get; set; }
    public int LineNumber { get; set; }
    public int ColumnNumber { get; set; }
    public string? Stack { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Url { get; set; }
}