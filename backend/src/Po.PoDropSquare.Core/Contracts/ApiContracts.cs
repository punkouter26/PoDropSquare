using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Po.PoDropSquare.Core.Contracts;

/// <summary>
/// Request model for score submission API endpoint
/// </summary>
public class ScoreSubmissionRequest
{
    /// <summary>
    /// Player's initials (1-3 uppercase alphanumeric characters)
    /// </summary>
    [JsonPropertyName("playerInitials")]
    public string PlayerInitials { get; set; } = string.Empty;

    /// <summary>
    /// Survival time in seconds (precision to hundredths)
    /// Range: 0.01 - 20.00 seconds
    /// </summary>
    [JsonPropertyName("survivalTime")]
    public double SurvivalTime { get; set; }

    /// <summary>
    /// Cryptographic signature of session data for anti-cheat validation
    /// SHA-256 hash as hex string
    /// </summary>
    [JsonPropertyName("sessionSignature")]
    public string SessionSignature { get; set; } = string.Empty;

    /// <summary>
    /// Client timestamp when the score was achieved
    /// ISO 8601 format string
    /// </summary>
    [JsonPropertyName("clientTimestamp")]
    public string ClientTimestamp { get; set; } = string.Empty;

    /// <summary>
    /// Validates the score submission request
    /// </summary>
    /// <returns>Validation result with error details if invalid</returns>
    public ValidationResult Validate()
    {
        // Validate player initials
        if (string.IsNullOrEmpty(PlayerInitials))
            return ValidationResult.Invalid("Player initials are required");

        if (PlayerInitials.Length < 1 || PlayerInitials.Length > 3)
            return ValidationResult.Invalid("Player initials must be 1-3 characters");

        if (!PlayerInitials.All(char.IsLetterOrDigit))
            return ValidationResult.Invalid("Player initials must be alphanumeric");

        if (!PlayerInitials.All(char.IsUpper))
            return ValidationResult.Invalid("Player initials must be uppercase");

        // Validate survival time
        if (SurvivalTime <= 0)
            return ValidationResult.Invalid("Survival time must be greater than 0");

        if (SurvivalTime > 20.0)
            return ValidationResult.Invalid("Survival time cannot exceed 20 seconds");

        // Check precision (only allow hundredths of seconds)
        var rounded = Math.Round(SurvivalTime, 2);
        if (Math.Abs(SurvivalTime - rounded) > 0.0001)
            return ValidationResult.Invalid("Survival time precision too high");

        // Validate session signature
        if (string.IsNullOrEmpty(SessionSignature))
            return ValidationResult.Invalid("Session signature is required");

        // Validate timestamp
        if (string.IsNullOrEmpty(ClientTimestamp))
            return ValidationResult.Invalid("Client timestamp is required");

        if (!DateTime.TryParse(ClientTimestamp, out var clientTime))
            return ValidationResult.Invalid("Invalid timestamp format");

        // Timestamp validation: Reject timestamps that are too old
        var timeDifference = (DateTime.UtcNow - clientTime.ToUniversalTime()).TotalMinutes;
        if (timeDifference > 5)
            return ValidationResult.Invalid("Timestamp is too old");

        return ValidationResult.Valid();
    }
}

/// <summary>
/// Response model for successful score submission
/// </summary>
public class ScoreSubmissionResponse
{
    /// <summary>
    /// Indicates if the submission was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Unique identifier for the submitted score
    /// </summary>
    [JsonPropertyName("scoreId")]
    public string ScoreId { get; set; } = string.Empty;

    /// <summary>
    /// Current position on the leaderboard (1-based, 0 means not on leaderboard)
    /// </summary>
    [JsonPropertyName("leaderboardPosition")]
    public int LeaderboardPosition { get; set; }

    /// <summary>
    /// Whether this score qualified for the leaderboard
    /// </summary>
    [JsonPropertyName("qualifiedForLeaderboard")]
    public bool QualifiedForLeaderboard { get; set; }

    /// <summary>
    /// Human-readable message about the submission
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata about the submission
    /// </summary>
    [JsonPropertyName("metadata")]
    public ScoreSubmissionMetadata? Metadata { get; set; }
}

/// <summary>
/// Response model for score submission errors
/// </summary>
public class ScoreSubmissionErrorResponse
{
    /// <summary>
    /// Always false for error responses
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; } = false;

    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional error details
    /// </summary>
    [JsonPropertyName("details")]
    public object? Details { get; set; }

    /// <summary>
    /// Retry-after header value for rate limiting (seconds)
    /// </summary>
    [JsonPropertyName("retryAfter")]
    public int? RetryAfter { get; set; }
}

/// <summary>
/// Response model for leaderboard data
/// </summary>
public class LeaderboardResponse
{
    /// <summary>
    /// Indicates if the request was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Array of top leaderboard entries
    /// </summary>
    [JsonPropertyName("leaderboard")]
    public List<LeaderboardEntryDto> Leaderboard { get; set; } = new();

    /// <summary>
    /// When the leaderboard was last updated
    /// ISO 8601 format string
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public string LastUpdated { get; set; } = string.Empty;

    /// <summary>
    /// Total number of entries in the full leaderboard
    /// </summary>
    [JsonPropertyName("totalEntries")]
    public int TotalEntries { get; set; }

    /// <summary>
    /// Cache control information
    /// </summary>
    [JsonPropertyName("cacheInfo")]
    public CacheInfo? CacheInfo { get; set; }
}

/// <summary>
/// Individual leaderboard entry for API responses
/// </summary>
public class LeaderboardEntryDto
{
    /// <summary>
    /// Position on the leaderboard (1-based)
    /// </summary>
    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    /// <summary>
    /// Player's initials
    /// </summary>
    [JsonPropertyName("playerInitials")]
    public string PlayerInitials { get; set; } = string.Empty;

    /// <summary>
    /// Best survival time in seconds
    /// </summary>
    [JsonPropertyName("survivalTime")]
    public double SurvivalTime { get; set; }

    /// <summary>
    /// When this score was originally achieved
    /// ISO 8601 format string
    /// </summary>
    [JsonPropertyName("submittedAt")]
    public string SubmittedAt { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier for the score entry
    /// </summary>
    [JsonPropertyName("scoreId")]
    public string ScoreId { get; set; } = string.Empty;

    /// <summary>
    /// Number of total submissions by this player
    /// </summary>
    [JsonPropertyName("totalSubmissions")]
    public int TotalSubmissions { get; set; }
}

/// <summary>
/// Health check response model
/// </summary>
public class HealthCheckResponse
{
    /// <summary>
    /// Overall health status
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Application version
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Server timestamp
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Health check for individual dependencies
    /// </summary>
    [JsonPropertyName("dependencies")]
    public Dictionary<string, DependencyHealth> Dependencies { get; set; } = new();

    /// <summary>
    /// Server uptime in milliseconds
    /// </summary>
    [JsonPropertyName("uptime")]
    public long Uptime { get; set; }

    /// <summary>
    /// Environment information
    /// </summary>
    [JsonPropertyName("environment")]
    public string Environment { get; set; } = string.Empty;
}

/// <summary>
/// Health check information for individual dependencies
/// </summary>
public class DependencyHealth
{
    /// <summary>
    /// Dependency status (healthy, unhealthy, degraded)
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Response time for this dependency in milliseconds
    /// </summary>
    [JsonPropertyName("responseTime")]
    public int ResponseTime { get; set; }

    /// <summary>
    /// Additional details about the dependency
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details { get; set; }
}

/// <summary>
/// Cache control information for API responses
/// </summary>
public class CacheInfo
{
    /// <summary>
    /// ETag for conditional requests
    /// </summary>
    [JsonPropertyName("etag")]
    public string? ETag { get; set; }

    /// <summary>
    /// Cache expiry time
    /// </summary>
    [JsonPropertyName("expires")]
    public DateTime? Expires { get; set; }

    /// <summary>
    /// Maximum age in seconds
    /// </summary>
    [JsonPropertyName("maxAge")]
    public int MaxAge { get; set; }
}

/// <summary>
/// Additional metadata for score submissions
/// </summary>
public class ScoreSubmissionMetadata
{
    /// <summary>
    /// Whether this was a new personal best
    /// </summary>
    [JsonPropertyName("isPersonalBest")]
    public bool IsPersonalBest { get; set; }

    /// <summary>
    /// Previous best score for this player
    /// </summary>
    [JsonPropertyName("previousBest")]
    public double? PreviousBest { get; set; }

    /// <summary>
    /// Improvement over previous best
    /// </summary>
    [JsonPropertyName("improvement")]
    public double? Improvement { get; set; }

    /// <summary>
    /// Total number of submissions by this player
    /// </summary>
    [JsonPropertyName("totalSubmissions")]
    public int TotalSubmissions { get; set; }

    /// <summary>
    /// Server processing time in milliseconds
    /// </summary>
    [JsonPropertyName("processingTime")]
    public int ProcessingTime { get; set; }
}

/// <summary>
/// Validation result for contract validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }

    public static ValidationResult Valid() => new() { IsValid = true };
    public static ValidationResult Invalid(string message) => new() { IsValid = false, ErrorMessage = message };
}

/// <summary>
/// Enhanced score submission result with detailed information
/// </summary>
public class ScoreSubmissionResult
{
    /// <summary>
    /// Indicates if the submission was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Human-readable message about the submission
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Detailed score information
    /// </summary>
    [JsonPropertyName("scoreDetails")]
    public ScoreDetails? ScoreDetails { get; set; }

    /// <summary>
    /// Validation information for transparency
    /// </summary>
    [JsonPropertyName("validationInfo")]
    public ValidationInfo? ValidationInfo { get; set; }
}

/// <summary>
/// Detailed score information
/// </summary>
public class ScoreDetails
{
    /// <summary>
    /// Unique identifier for the score
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Player's initials
    /// </summary>
    [JsonPropertyName("playerInitials")]
    public string PlayerInitials { get; set; } = string.Empty;

    /// <summary>
    /// Score value (survival time)
    /// </summary>
    [JsonPropertyName("score")]
    public double Score { get; set; }

    /// <summary>
    /// When the score was submitted
    /// </summary>
    [JsonPropertyName("submittedAt")]
    public string SubmittedAt { get; set; } = string.Empty;

    /// <summary>
    /// Current rank on leaderboard
    /// </summary>
    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    /// <summary>
    /// Whether this is a new personal best
    /// </summary>
    [JsonPropertyName("isNewPersonalBest")]
    public bool IsNewPersonalBest { get; set; }

    /// <summary>
    /// Whether this score qualified for the leaderboard
    /// </summary>
    [JsonPropertyName("isLeaderboardQualified")]
    public bool IsLeaderboardQualified { get; set; }
}

/// <summary>
/// Validation information for score submissions
/// </summary>
public class ValidationInfo
{
    /// <summary>
    /// Whether session validation passed
    /// </summary>
    [JsonPropertyName("sessionValidated")]
    public bool SessionValidated { get; set; }

    /// <summary>
    /// Whether timing validation passed
    /// </summary>
    [JsonPropertyName("timingValidated")]
    public bool TimingValidated { get; set; }

    /// <summary>
    /// Whether duplicate check passed
    /// </summary>
    [JsonPropertyName("duplicateCheck")]
    public bool DuplicateCheck { get; set; }
}

/// <summary>
/// Generic error response model
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional error details
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details { get; set; }

    /// <summary>
    /// Timestamp of when the error occurred
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");
}

/// <summary>
/// Model for client-side log entries
/// </summary>
public class ClientLogEntry
{
    /// <summary>
    /// Log level (trace, debug, information, warning, error, critical)
    /// </summary>
    [JsonPropertyName("level")]
    [Required]
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// Log message
    /// </summary>
    [JsonPropertyName("message")]
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional additional data (JSON string)
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }

    /// <summary>
    /// Client timestamp when the log was created
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// URL where the log was generated
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Model for client-side error entries
/// </summary>
public class ClientErrorEntry
{
    /// <summary>
    /// Error message
    /// </summary>
    [JsonPropertyName("message")]
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Source filename where the error occurred
    /// </summary>
    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    /// <summary>
    /// Line number where the error occurred
    /// </summary>
    [JsonPropertyName("lineNumber")]
    public int LineNumber { get; set; }

    /// <summary>
    /// Column number where the error occurred
    /// </summary>
    [JsonPropertyName("columnNumber")]
    public int ColumnNumber { get; set; }

    /// <summary>
    /// Error stack trace
    /// </summary>
    [JsonPropertyName("stack")]
    public string? Stack { get; set; }

    /// <summary>
    /// Client timestamp when the error occurred
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// URL where the error occurred
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}