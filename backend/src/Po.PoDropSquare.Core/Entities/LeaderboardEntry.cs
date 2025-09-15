using Azure;
using Azure.Data.Tables;

namespace Po.PoDropSquare.Core.Entities;

/// <summary>
/// Azure Table Storage entity for leaderboard entries.
/// Uses "LEADERBOARD" as PartitionKey and rank-based RowKey for efficient top-N queries.
/// </summary>
public class LeaderboardEntry : ITableEntity
{
    /// <summary>
    /// Constant value for partition key
    /// </summary>
    public const string PartitionKeyValue = "LEADERBOARD";

    /// <summary>
    /// Azure Table Storage partition key - using "LEADERBOARD" for all entries
    /// This ensures all leaderboard entries are in the same partition for efficient queries
    /// </summary>
    public string PartitionKey { get; set; } = PartitionKeyValue;

    /// <summary>
    /// Azure Table Storage row key - using rank for natural ordering
    /// Format: RANK_{rank:D3} (e.g., "RANK_001", "RANK_002")
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
    /// Current rank on the leaderboard (1-based)
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Player's initials (1-3 uppercase alphanumeric characters)
    /// </summary>
    public string PlayerInitials { get; set; } = string.Empty;

    /// <summary>
    /// Best survival time in seconds (precision to hundredths)
    /// </summary>
    public double SurvivalTime { get; set; }

    /// <summary>
    /// Reference to the original score entry ID
    /// </summary>
    public string ScoreId { get; set; } = string.Empty;

    /// <summary>
    /// When this score was originally achieved
    /// ISO 8601 format string
    /// </summary>
    public string SubmittedAt { get; set; } = string.Empty;

    /// <summary>
    /// When this entry was last updated on the leaderboard
    /// ISO 8601 format string
    /// </summary>
    public string UpdatedAt { get; set; } = string.Empty;

    /// <summary>
    /// Number of total scores submitted by this player
    /// </summary>
    public int TotalSubmissions { get; set; }

    /// <summary>
    /// Hash of session data for verification
    /// </summary>
    public string SessionSignature { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new LeaderboardEntry with proper Azure Table Storage keys
    /// </summary>
    /// <param name="playerInitials">Player initials</param>
    /// <param name="survivalTime">Survival time in seconds</param>
    /// <param name="scoreId">Reference to original score ID</param>
    /// <param name="rank">Position on leaderboard (1-based)</param>
    /// <param name="totalSubmissions">Total submissions by this player</param>
    /// <param name="submittedAt">When the score was submitted</param>
    /// <returns>LeaderboardEntry with configured keys</returns>
    public static LeaderboardEntry Create(string playerInitials, double survivalTime, string scoreId, int rank, int totalSubmissions, DateTime submittedAt)
    {
        return new LeaderboardEntry
        {
            PartitionKey = PartitionKeyValue,
            RowKey = $"RANK_{rank:D3}",
            Rank = rank,
            PlayerInitials = playerInitials,
            SurvivalTime = survivalTime,
            ScoreId = scoreId,
            SubmittedAt = submittedAt.ToString("O"),
            UpdatedAt = DateTime.UtcNow.ToString("O"),
            TotalSubmissions = totalSubmissions
        };
    }

    /// <summary>
    /// Updates this leaderboard entry with a new score
    /// </summary>
    /// <param name="newRank">New rank position</param>
    /// <param name="scoreEntry">New score entry</param>
    public void UpdateWith(int newRank, ScoreEntry scoreEntry)
    {
        // Update rank and row key if position changed
        if (Rank != newRank)
        {
            Rank = newRank;
            RowKey = $"RANK_{newRank:D3}";
        }

        // Update if this is a better score
        if (scoreEntry.SurvivalTime > SurvivalTime)
        {
            SurvivalTime = scoreEntry.SurvivalTime;
            ScoreId = scoreEntry.ScoreId;
            SubmittedAt = scoreEntry.ClientTimestamp;
            SessionSignature = scoreEntry.SessionSignature;
        }

        UpdatedAt = DateTime.UtcNow.ToString("O");
        TotalSubmissions++;
    }

    /// <summary>
    /// Validates the leaderboard entry data
    /// </summary>
    /// <returns>Validation result with error details if invalid</returns>
    public Contracts.ValidationResult Validate()
    {
        // Validate rank
        if (Rank < 1)
            return Contracts.ValidationResult.Invalid("Rank must be greater than 0");

        if (Rank > 1000) // Reasonable upper limit
            return Contracts.ValidationResult.Invalid("Rank cannot exceed 1000");

        // Validate player initials
        if (string.IsNullOrEmpty(PlayerInitials))
            return Contracts.ValidationResult.Invalid("Player initials are required");

        if (PlayerInitials.Length < 1 || PlayerInitials.Length > 3)
            return Contracts.ValidationResult.Invalid("Player initials must be 1-3 characters");

        // Validate survival time
        if (SurvivalTime <= 0)
            return Contracts.ValidationResult.Invalid("Survival time must be greater than 0");

        if (SurvivalTime > 20.0)
            return Contracts.ValidationResult.Invalid("Survival time cannot exceed 20 seconds");

        // Validate required fields
        if (string.IsNullOrEmpty(ScoreId))
            return Contracts.ValidationResult.Invalid("Score ID is required");

        return Contracts.ValidationResult.Valid();
    }

    /// <summary>
    /// Converts to API response format
    /// </summary>
    /// <returns>Leaderboard entry for API response</returns>
    public object ToApiResponse()
    {
        return new
        {
            Rank = Rank,
            PlayerInitials = PlayerInitials,
            SurvivalTime = SurvivalTime,
            SubmittedAt = SubmittedAt,
            ScoreId = ScoreId,
            TotalSubmissions = TotalSubmissions
        };
    }
}