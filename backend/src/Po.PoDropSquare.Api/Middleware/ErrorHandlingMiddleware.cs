using System.Net;
using System.Text.Json;
using Po.PoDropSquare.Core.Contracts;

namespace Po.PoDropSquare.Api.Middleware;

/// <summary>
/// Global error handling middleware that provides consistent error responses and structured logging
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Log the exception with detailed context
        _logger.LogError(exception,
            "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}, IP: {RemoteIp}, UserAgent: {UserAgent}",
            correlationId, requestPath, requestMethod, remoteIp, userAgent);

        // Determine response based on exception type
        var (statusCode, errorCode, message, details) = GetErrorResponse(exception);

        // Create error response
        var errorResponse = new ErrorResponse
        {
            Error = errorCode,
            Message = message,
            Details = _environment.IsDevelopment() ? details : null,
            Timestamp = DateTime.UtcNow.ToString("O")
        };

        // Add correlation ID for tracking
        if (!context.Response.Headers.ContainsKey("X-Correlation-ID"))
        {
            context.Response.Headers["X-Correlation-ID"] = correlationId;
        }

        // Set response properties
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        // Ensure response hasn't been started
        if (!context.Response.HasStarted)
        {
            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    private (HttpStatusCode statusCode, string errorCode, string message, string? details) GetErrorResponse(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException nullEx => (
                HttpStatusCode.BadRequest,
                "MissingParameter",
                "Required parameter is missing",
                nullEx.Message
            ),

            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                "InvalidArgument",
                "Invalid request parameters",
                argEx.Message
            ),

            InvalidOperationException opEx => (
                HttpStatusCode.Conflict,
                "InvalidOperation",
                "The requested operation is not valid in the current state",
                opEx.Message
            ),

            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "Access denied",
                "Authentication required or insufficient permissions"
            ),

            TimeoutException timeoutEx => (
                HttpStatusCode.RequestTimeout,
                "Timeout",
                "The request timed out",
                timeoutEx.Message
            ),

            TaskCanceledException cancelEx when cancelEx.CancellationToken.IsCancellationRequested => (
                HttpStatusCode.RequestTimeout,
                "RequestCancelled",
                "The request was cancelled",
                "Operation was cancelled due to timeout or client disconnect"
            ),

            HttpRequestException httpEx => (
                HttpStatusCode.BadGateway,
                "ExternalServiceError",
                "External service is unavailable",
                httpEx.Message
            ),

            JsonException jsonEx => (
                HttpStatusCode.BadRequest,
                "InvalidJson",
                "Invalid JSON format in request",
                jsonEx.Message
            ),

            NotImplementedException => (
                HttpStatusCode.NotImplemented,
                "NotImplemented",
                "This feature is not yet implemented",
                "The requested functionality is under development"
            ),

            // Azure-specific exceptions
            Azure.RequestFailedException azureEx => GetAzureErrorResponse(azureEx),

            // Generic fallback
            _ => (
                HttpStatusCode.InternalServerError,
                "InternalServerError",
                "An unexpected error occurred",
                _environment.IsDevelopment() ? exception.ToString() : "Please try again later or contact support"
            )
        };
    }

    private (HttpStatusCode statusCode, string errorCode, string message, string? details) GetAzureErrorResponse(Azure.RequestFailedException azureException)
    {
        return azureException.Status switch
        {
            400 => (HttpStatusCode.BadRequest, "AzureBadRequest", "Invalid request to Azure service", azureException.Message),
            401 => (HttpStatusCode.Unauthorized, "AzureUnauthorized", "Azure service authorization failed", azureException.Message),
            403 => (HttpStatusCode.Forbidden, "AzureForbidden", "Access to Azure service forbidden", azureException.Message),
            404 => (HttpStatusCode.NotFound, "AzureNotFound", "Azure resource not found", azureException.Message),
            409 => (HttpStatusCode.Conflict, "AzureConflict", "Azure resource conflict", azureException.Message),
            429 => (HttpStatusCode.TooManyRequests, "AzureRateLimit", "Azure service rate limit exceeded", azureException.Message),
            500 => (HttpStatusCode.BadGateway, "AzureServerError", "Azure service error", azureException.Message),
            503 => (HttpStatusCode.ServiceUnavailable, "AzureUnavailable", "Azure service temporarily unavailable", azureException.Message),
            _ => (HttpStatusCode.BadGateway, "AzureError", "Azure service error", azureException.Message)
        };
    }
}