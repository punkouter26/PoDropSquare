using Po.PoDropSquare.Core.Entities;

namespace Po.PoDropSquare.Data.Repositories;

/// <summary>
/// Repository interface for score data operations
/// </summary>
public interface IScoreRepository
{
    /// <summary>
    /// Submits a new score entry to storage
    /// </summary>
    /// <param name="scoreEntry">The score entry to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The stored score entry with updated metadata</returns>
    Task<ScoreEntry> SubmitScoreAsync(ScoreEntry scoreEntry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all scores for a specific player
    /// </summary>
    /// <param name="playerInitials">Player's initials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of scores for the player, ordered by survival time descending</returns>
    Task<List<ScoreEntry>> GetPlayerScoresAsync(string playerInitials, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the best score for a specific player
    /// </summary>
    /// <param name="playerInitials">Player's initials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The player's best score, or null if no scores exist</returns>
    Task<ScoreEntry?> GetPlayerBestScoreAsync(string playerInitials, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all scores within a time range
    /// </summary>
    /// <param name="startTime">Start of time range</param>
    /// <param name="endTime">End of time range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of scores within the time range</returns>
    Task<List<ScoreEntry>> GetScoresByTimeRangeAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific score by its unique identifier
    /// </summary>
    /// <param name="scoreId">Unique score identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The score entry, or null if not found</returns>
    Task<ScoreEntry?> GetScoreByIdAsync(string scoreId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of scores submitted by a player
    /// </summary>
    /// <param name="playerInitials">Player's initials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total number of submissions</returns>
    Task<int> GetPlayerSubmissionCountAsync(string playerInitials, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old score entries based on retention policy
    /// </summary>
    /// <param name="retentionDays">Number of days to retain scores</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of deleted entries</returns>
    Task<int> CleanupOldScoresAsync(int retentionDays, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for leaderboard data operations
/// </summary>
public interface ILeaderboardRepository
{
    /// <summary>
    /// Updates the leaderboard with a new score
    /// </summary>
    /// <param name="scoreEntry">The score entry to potentially add to leaderboard</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The leaderboard entry if it qualified, or null if it didn't make the top 10</returns>
    Task<LeaderboardEntry?> UpdateLeaderboardAsync(ScoreEntry scoreEntry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the top N entries from the leaderboard
    /// </summary>
    /// <param name="topN">Number of top entries to retrieve (default 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of top leaderboard entries, ordered by rank</returns>
    Task<List<LeaderboardEntry>> GetTopEntriesAsync(int topN = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a player's position on the leaderboard
    /// </summary>
    /// <param name="playerInitials">Player's initials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The player's leaderboard entry, or null if not on leaderboard</returns>
    Task<LeaderboardEntry?> GetPlayerLeaderboardEntryAsync(string playerInitials, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a score would qualify for the leaderboard
    /// </summary>
    /// <param name="survivalTime">The survival time to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the score would qualify for top 10</returns>
    Task<bool> WouldQualifyForLeaderboardAsync(double survivalTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of unique players on the leaderboard
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of unique players</returns>
    Task<int> GetTotalLeaderboardEntriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds the entire leaderboard from score data
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of leaderboard entries created</returns>
    Task<int> RebuildLeaderboardAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the timestamp when the leaderboard was last updated
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Last update timestamp, or null if never updated</returns>
    Task<DateTime?> GetLastUpdateTimeAsync(CancellationToken cancellationToken = default);
}