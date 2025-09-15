using Po.PoDropSquare.Core.Contracts;
using Po.PoDropSquare.Core.Entities;

namespace Po.PoDropSquare.Services.Services;

/// <summary>
/// Service interface for score submission and validation
/// </summary>
public interface IScoreService
{
    /// <summary>
    /// Submits a score with full validation and anti-cheat checks
    /// </summary>
    /// <param name="request">Score submission request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Score submission response with leaderboard information</returns>
    Task<object> SubmitScoreAsync(ScoreSubmissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a score submission request
    /// </summary>
    /// <param name="request">Score submission request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with error details if invalid</returns>
    Task<ValidationResult> ValidateScoreSubmissionAsync(ScoreSubmissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all scores for a specific player
    /// </summary>
    /// <param name="playerInitials">Player's initials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of scores for the player</returns>
    Task<List<ScoreEntry>> GetPlayerScoresAsync(string playerInitials, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the best score for a specific player
    /// </summary>
    /// <param name="playerInitials">Player's initials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The player's best score, or null if no scores exist</returns>
    Task<ScoreEntry?> GetPlayerBestScoreAsync(string playerInitials, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific score by its identifier
    /// </summary>
    /// <param name="scoreId">Unique score identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The score entry, or null if not found</returns>
    Task<ScoreEntry?> GetScoreByIdAsync(string scoreId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs cleanup of old score entries
    /// </summary>
    /// <param name="retentionDays">Number of days to retain scores</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of deleted entries</returns>
    Task<int> CleanupOldScoresAsync(int retentionDays, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for leaderboard operations
/// </summary>
public interface ILeaderboardService
{
    /// <summary>
    /// Gets the top N entries from the leaderboard
    /// </summary>
    /// <param name="topN">Number of top entries to retrieve (default 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Leaderboard response with top entries</returns>
    Task<LeaderboardResponse> GetTopLeaderboardAsync(int topN = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a player's position on the leaderboard
    /// </summary>
    /// <param name="playerInitials">Player's initials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The player's leaderboard entry, or null if not on leaderboard</returns>
    Task<LeaderboardEntryDto?> GetPlayerLeaderboardPositionAsync(string playerInitials, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a score would qualify for the leaderboard
    /// </summary>
    /// <param name="survivalTime">The survival time to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the score would qualify for top 10</returns>
    Task<bool> WouldQualifyForLeaderboardAsync(double survivalTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds the entire leaderboard from score data
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of leaderboard entries created</returns>
    Task<int> RebuildLeaderboardAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets leaderboard statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Leaderboard statistics including total entries and last update time</returns>
    Task<LeaderboardStats> GetLeaderboardStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Leaderboard statistics data
/// </summary>
public class LeaderboardStats
{
    public int TotalEntries { get; set; }
    public DateTime? LastUpdated { get; set; }
    public double? HighestScore { get; set; }
    public double? LowestScore { get; set; }
    public double? AverageScore { get; set; }
}