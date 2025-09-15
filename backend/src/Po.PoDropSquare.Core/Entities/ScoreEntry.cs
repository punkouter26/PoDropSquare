using Azure;
using Azure.Data.Tables;
using System.Text.Json.Serialization;

namespace Po.PoDropSquare.Core.Entities;

/// <summary>
/// Azure Table Storage entity for individual score entries.
/// Uses player initials as PartitionKey and score timestamp as RowKey for optimal performance.
/// </summary>
public class ScoreEntry : ITableEntity
{
    /// <summary>
    /// Azure Table Storage partition key - using player initials for distribution
    /// </summary>
    public string PartitionKey { get; set; } = string.Empty;

    /// <summary>
    /// Azure Table Storage row key - using inverted timestamp for descending order
    /// Format: 9999999999999999999 - ticks to get newest scores first
    /// </summary>
    public string RowKey { get; set; } = string.Empty;

    /// <summary>
    /// Azure Table Storage timestamp - automatically managed
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Azure Table Storage ETag for optimistic concurrency
    /// </summary>
    public ETag ETag { get; set; }

    /// <summary>
    /// Player's initials (1-3 uppercase alphanumeric characters)
    /// </summary>
    public string PlayerInitials { get; set; } = string.Empty;

    /// <summary>
    /// Survival time in seconds (precision to hundredths)
    /// Range: 0.01 - 20.00 seconds
    /// </summary>
    public double SurvivalTime { get; set; }

    /// <summary>
    /// Unique identifier for this score entry
    /// </summary>
    public string ScoreId { get; set; } = string.Empty;

    /// <summary>
    /// Session signature for anti-cheat validation
    /// SHA-256 hash of session data
    /// </summary>
    public string SessionSignature { get; set; } = string.Empty;

    /// <summary>
    /// Client timestamp when the score was achieved
    /// ISO 8601 format string
    /// </summary>
    public string ClientTimestamp { get; set; } = string.Empty;

    /// <summary>
    /// Server timestamp when the score was submitted
    /// ISO 8601 format string
    /// </summary>
    public string ServerTimestamp { get; set; } = string.Empty;

    /// <summary>
    /// When the score was submitted (DateTime)
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Client IP address for tracking (hashed for privacy)
    /// </summary>
    public string ClientIpHash { get; set; } = string.Empty;

    /// <summary>
    /// User agent hash for device fingerprinting
    /// </summary>
    public string UserAgentHash { get; set; } = string.Empty;

    /// <summary>
    /// Flag indicating if this score qualified for the leaderboard
    /// </summary>
    public bool QualifiedForLeaderboard { get; set; }

    /// <summary>
    /// Position on the leaderboard when submitted (if qualified)
    /// 0 means not on leaderboard
    /// </summary>
    public int LeaderboardPosition { get; set; }

    /// <summary>
    /// Creates a new ScoreEntry with proper Azure Table Storage keys
    /// </summary>
    /// <param name="playerInitials">Player initials</param>
    /// <param name="survivalTime">Survival time in seconds</param>
    /// <param name="sessionSignature">Session signature for anti-cheat</param>
    /// <param name="clientTimestamp">Client timestamp</param>
    /// <returns>ScoreEntry with configured keys and data</returns>
    public static ScoreEntry Create(string playerInitials, double survivalTime, string sessionSignature, DateTime clientTimestamp)
    {
        var scoreId = Guid.NewGuid().ToString();
        var invertedTicks = DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks;
        var submittedAt = DateTime.UtcNow;

        return new ScoreEntry
        {
            PartitionKey = playerInitials.ToUpperInvariant(),
            RowKey = $"{invertedTicks:D19}_{scoreId}",
            ScoreId = scoreId,
            PlayerInitials = playerInitials.ToUpperInvariant(),
            SurvivalTime = survivalTime,
            SessionSignature = sessionSignature,
            ClientTimestamp = clientTimestamp.ToString("O"),
            ServerTimestamp = submittedAt.ToString("O"),
            SubmittedAt = submittedAt
        };
    }

    /// <summary>
    /// Validates the score entry data
    /// </summary>
    /// <returns>Validation result with error details if invalid</returns>
    public Contracts.ValidationResult Validate()
    {
        // Validate player initials
        if (string.IsNullOrEmpty(PlayerInitials))
            return Contracts.ValidationResult.Invalid("Player initials are required");

        if (PlayerInitials.Length < 1 || PlayerInitials.Length > 3)
            return Contracts.ValidationResult.Invalid("Player initials must be 1-3 characters");

        if (!PlayerInitials.All(char.IsLetterOrDigit))
            return Contracts.ValidationResult.Invalid("Player initials must be alphanumeric");

        if (!PlayerInitials.All(char.IsUpper))
            return Contracts.ValidationResult.Invalid("Player initials must be uppercase");

        // Validate survival time
        if (SurvivalTime <= 0)
            return Contracts.ValidationResult.Invalid("Survival time must be greater than 0");

        if (SurvivalTime > 20.0)
            return Contracts.ValidationResult.Invalid("Survival time cannot exceed 20 seconds");

        // Check precision (only allow hundredths of seconds)
        var rounded = Math.Round(SurvivalTime, 2);
        if (Math.Abs(SurvivalTime - rounded) > 0.0001)
            return Contracts.ValidationResult.Invalid("Survival time precision too high");

        // Validate required fields
        if (string.IsNullOrEmpty(ScoreId))
            return Contracts.ValidationResult.Invalid("Score ID is required");

        if (string.IsNullOrEmpty(SessionSignature))
            return Contracts.ValidationResult.Invalid("Session signature is required");

        return Contracts.ValidationResult.Valid();
    }
}