using Microsoft.AspNetCore.Mvc;
using Po.PoDropSquare.Core.Contracts;
using System.ComponentModel.DataAnnotations;

namespace Po.PoDropSquare.Api.Controllers;

/// <summary>
/// Logging controller for receiving client-side logs
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LogController : ControllerBase
{
    private readonly ILogger<LogController> _logger;
    private readonly ILogger _clientLogger;

    public LogController(ILogger<LogController> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        // Create a separate logger for client logs
        _clientLogger = loggerFactory.CreateLogger("ClientLogger");
    }

    /// <summary>
    /// Receives log messages from the client and writes them using the server's logger
    /// </summary>
    /// <param name="clientLogEntry">The log entry from the client</param>
    /// <returns>Success response</returns>
    [HttpPost("client")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> LogClientMessage([FromBody] ClientLogEntry clientLogEntry)
    {
        if (clientLogEntry == null)
        {
            return BadRequest("Log entry cannot be null");
        }

        try
        {
            // Parse the log level
            var logLevel = ParseLogLevel(clientLogEntry.Level);

            // Create structured log message with client context
            using (_clientLogger.BeginScope(new Dictionary<string, object>
            {
                ["ClientTimestamp"] = clientLogEntry.Timestamp,
                ["UserAgent"] = Request.Headers.UserAgent.ToString(),
                ["RemoteIP"] = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                ["Url"] = clientLogEntry.Url ?? string.Empty,
                ["Source"] = "Client"
            }))
            {
                _clientLogger.Log(logLevel, "[CLIENT] {Message} {Data}",
                    clientLogEntry.Message,
                    clientLogEntry.Data ?? "");
            }

            // Also write to a dedicated client log file
            await WriteToClientLogFile(clientLogEntry);

            return Ok(new { success = true, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process client log entry: {Message}", clientLogEntry.Message);
            return StatusCode(500, "Failed to process log entry");
        }
    }

    /// <summary>
    /// Receives JavaScript errors from the client
    /// </summary>
    /// <param name="errorEntry">The error details from the client</param>
    /// <returns>Success response</returns>
    [HttpPost("error")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> LogClientError([FromBody] ClientErrorEntry errorEntry)
    {
        if (errorEntry == null)
        {
            return BadRequest("Error entry cannot be null");
        }

        try
        {
            // Log the error with additional context
            using (_clientLogger.BeginScope(new Dictionary<string, object>
            {
                ["ClientTimestamp"] = errorEntry.Timestamp,
                ["UserAgent"] = Request.Headers.UserAgent.ToString(),
                ["RemoteIP"] = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                ["ErrorType"] = "JavaScriptError",
                ["Source"] = "Client",
                ["Filename"] = errorEntry.Filename ?? string.Empty,
                ["LineNumber"] = errorEntry.LineNumber,
                ["ColumnNumber"] = errorEntry.ColumnNumber,
                ["Stack"] = errorEntry.Stack ?? string.Empty
            }))
            {
                _clientLogger.LogError("[CLIENT ERROR] {Message} at {Filename}:{LineNumber}:{ColumnNumber}",
                    errorEntry.Message,
                    errorEntry.Filename,
                    errorEntry.LineNumber,
                    errorEntry.ColumnNumber);
            }

            // Also write to the client log file
            await WriteClientErrorToFile(errorEntry);

            return Ok(new { success = true, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process client error entry: {Message}", errorEntry.Message);
            return StatusCode(500, "Failed to process error entry");
        }
    }

    private static LogLevel ParseLogLevel(string level)
    {
        return level?.ToLowerInvariant() switch
        {
            "trace" => LogLevel.Trace,
            "debug" => LogLevel.Debug,
            "information" or "info" => LogLevel.Information,
            "warning" or "warn" => LogLevel.Warning,
            "error" => LogLevel.Error,
            "critical" => LogLevel.Critical,
            _ => LogLevel.Information
        };
    }

    private async Task WriteToClientLogFile(ClientLogEntry entry)
    {
        try
        {
            var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "DEBUG");
            Directory.CreateDirectory(logDirectory);

            var logFile = Path.Combine(logDirectory, "client_log.txt");

            var logLine = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC] [{entry.Level.ToUpperInvariant()}] [CLIENT] {entry.Message}";

            if (!string.IsNullOrEmpty(entry.Data))
            {
                logLine += $" | Data: {entry.Data}";
            }

            if (!string.IsNullOrEmpty(entry.Url))
            {
                logLine += $" | URL: {entry.Url}";
            }

            logLine += Environment.NewLine;

            await System.IO.File.AppendAllTextAsync(logFile, logLine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write client log to file");
        }
    }

    private async Task WriteClientErrorToFile(ClientErrorEntry entry)
    {
        try
        {
            var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "DEBUG");
            Directory.CreateDirectory(logDirectory);

            var logFile = Path.Combine(logDirectory, "client_log.txt");

            var logLine = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC] [ERROR] [CLIENT] JavaScript Error: {entry.Message}";

            if (!string.IsNullOrEmpty(entry.Filename))
            {
                logLine += $" | File: {entry.Filename}:{entry.LineNumber}:{entry.ColumnNumber}";
            }

            if (!string.IsNullOrEmpty(entry.Stack))
            {
                logLine += $" | Stack: {entry.Stack}";
            }

            logLine += Environment.NewLine;

            await System.IO.File.AppendAllTextAsync(logFile, logLine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write client error to file");
        }
    }
}