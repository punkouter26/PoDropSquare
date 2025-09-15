using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Po.PoDropSquare.Core.Entities;

namespace Po.PoDropSquare.Data.Repositories;

/// <summary>
/// Azure Table Storage implementation of leaderboard repository
/// </summary>
public class LeaderboardRepository : ILeaderboardRepository
{
    private readonly TableClient _tableClient;
    private readonly IScoreRepository _scoreRepository;
    private readonly ILogger<LeaderboardRepository> _logger;
    private const string TableName = "Leaderboard";
    private const int MaxLeaderboardSize = 10;

    public LeaderboardRepository(
        TableServiceClient tableServiceClient,
        IScoreRepository scoreRepository,
        ILogger<LeaderboardRepository> logger)
    {
        _tableClient = tableServiceClient.GetTableClient(TableName);
        _scoreRepository = scoreRepository;
        _logger = logger;
    }

    /// <summary>
    /// Ensures the table exists
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await _tableClient.CreateIfNotExistsAsync();
            _logger.LogInformation("Leaderboard table initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Leaderboard table");
            throw;
        }
    }

    public async Task<LeaderboardEntry?> UpdateLeaderboardAsync(ScoreEntry scoreEntry, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating leaderboard with score {SurvivalTime}s from player {PlayerInitials}",
                scoreEntry.SurvivalTime, scoreEntry.PlayerInitials);

            // Get current leaderboard
            var currentLeaderboard = await GetTopEntriesAsync(MaxLeaderboardSize, cancellationToken);

            // Check if player already exists on leaderboard
            var existingEntry = currentLeaderboard.FirstOrDefault(e => e.PlayerInitials == scoreEntry.PlayerInitials);

            // Determine if this score qualifies
            bool qualifies = false;
            int newRank = 0;

            if (existingEntry != null)
            {
                // Player already on leaderboard - check if this is better
                if (scoreEntry.SurvivalTime > existingEntry.SurvivalTime)
                {
                    qualifies = true;
                    // Remove existing entry to recalculate rank
                    currentLeaderboard.Remove(existingEntry);
                    await _tableClient.DeleteEntityAsync(existingEntry.PartitionKey, existingEntry.RowKey, cancellationToken: cancellationToken);
                }
                else
                {
                    _logger.LogDebug("Score {SurvivalTime}s does not improve player {PlayerInitials} best of {BestTime}s",
                        scoreEntry.SurvivalTime, scoreEntry.PlayerInitials, existingEntry.SurvivalTime);
                    return null;
                }
            }
            else if (currentLeaderboard.Count < MaxLeaderboardSize)
            {
                // Leaderboard not full - automatically qualifies
                qualifies = true;
            }
            else
            {
                // Check if score beats the worst on leaderboard
                var worstEntry = currentLeaderboard.LastOrDefault();
                if (worstEntry != null && scoreEntry.SurvivalTime > worstEntry.SurvivalTime)
                {
                    qualifies = true;
                    // Remove worst entry
                    currentLeaderboard.Remove(worstEntry);
                    await _tableClient.DeleteEntityAsync(worstEntry.PartitionKey, worstEntry.RowKey, cancellationToken: cancellationToken);
                }
            }

            if (!qualifies)
            {
                _logger.LogDebug("Score {SurvivalTime}s from player {PlayerInitials} does not qualify for leaderboard",
                    scoreEntry.SurvivalTime, scoreEntry.PlayerInitials);
                return null;
            }

            // Calculate new rank
            newRank = currentLeaderboard.Count(e => e.SurvivalTime > scoreEntry.SurvivalTime) + 1;

            // Get player submission count
            var submissionCount = await _scoreRepository.GetPlayerSubmissionCountAsync(scoreEntry.PlayerInitials, cancellationToken);

            // Create new leaderboard entry
            var leaderboardEntry = LeaderboardEntry.Create(
                scoreEntry.PlayerInitials,
                scoreEntry.SurvivalTime,
                scoreEntry.ScoreId,
                newRank,
                submissionCount,
                scoreEntry.SubmittedAt
            );

            // Add new entry
            await _tableClient.AddEntityAsync(leaderboardEntry, cancellationToken);

            // Update ranks for other entries if necessary
            await UpdateRanksAsync(cancellationToken);

            _logger.LogInformation("Player {PlayerInitials} added to leaderboard at rank {Rank} with score {SurvivalTime}s",
                scoreEntry.PlayerInitials, newRank, scoreEntry.SurvivalTime);

            return leaderboardEntry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update leaderboard for player {PlayerInitials}", scoreEntry.PlayerInitials);
            throw;
        }
    }

    public async Task<List<LeaderboardEntry>> GetTopEntriesAsync(int topN = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = new List<LeaderboardEntry>();

            await foreach (var entry in _tableClient.QueryAsync<LeaderboardEntry>(
                filter: TableClient.CreateQueryFilter($"PartitionKey eq {LeaderboardEntry.PartitionKeyValue}"),
                cancellationToken: cancellationToken))
            {
                entries.Add(entry);
            }

            // Sort by rank (RowKey is already rank-based, but ensure correct order)
            entries.Sort((a, b) => a.Rank.CompareTo(b.Rank));

            var result = entries.Take(topN).ToList();

            _logger.LogDebug("Retrieved {Count} leaderboard entries (requested top {TopN})", result.Count, topN);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve top {TopN} leaderboard entries", topN);
            throw;
        }
    }

    public async Task<LeaderboardEntry?> GetPlayerLeaderboardEntryAsync(string playerInitials, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = TableClient.CreateQueryFilter($"PartitionKey eq {LeaderboardEntry.PartitionKeyValue} and PlayerInitials eq {playerInitials}");

            await foreach (var entry in _tableClient.QueryAsync<LeaderboardEntry>(filter, cancellationToken: cancellationToken))
            {
                _logger.LogDebug("Found leaderboard entry for player {PlayerInitials} at rank {Rank}",
                    playerInitials, entry.Rank);
                return entry;
            }

            _logger.LogDebug("Player {PlayerInitials} not found on leaderboard", playerInitials);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get leaderboard entry for player {PlayerInitials}", playerInitials);
            throw;
        }
    }

    public async Task<bool> WouldQualifyForLeaderboardAsync(double survivalTime, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentLeaderboard = await GetTopEntriesAsync(MaxLeaderboardSize, cancellationToken);

            // If leaderboard isn't full, any score qualifies
            if (currentLeaderboard.Count < MaxLeaderboardSize)
            {
                return true;
            }

            // Check if score beats the worst entry
            var worstEntry = currentLeaderboard.LastOrDefault();
            var qualifies = worstEntry == null || survivalTime > worstEntry.SurvivalTime;

            _logger.LogDebug("Score {SurvivalTime}s would {Qualify} for leaderboard",
                survivalTime, qualifies ? "qualify" : "not qualify");

            return qualifies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if score {SurvivalTime} would qualify for leaderboard", survivalTime);
            throw;
        }
    }

    public async Task<int> GetTotalLeaderboardEntriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var count = 0;
            var filter = TableClient.CreateQueryFilter($"PartitionKey eq {LeaderboardEntry.PartitionKeyValue}");

            await foreach (var _ in _tableClient.QueryAsync<LeaderboardEntry>(
                filter,
                select: new[] { "RowKey" }, // Only select row key for efficiency
                cancellationToken: cancellationToken))
            {
                count++;
            }

            _logger.LogDebug("Total leaderboard entries: {Count}", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get total leaderboard entries count");
            throw;
        }
    }

    public async Task<int> RebuildLeaderboardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting leaderboard rebuild");

            // Clear existing leaderboard
            await ClearLeaderboardAsync(cancellationToken);

            // Get all player best scores
            var playerBestScores = new Dictionary<string, ScoreEntry>();

            // This is inefficient for large datasets - in production, consider batch processing
            await foreach (var scoreEntry in _tableClient.QueryAsync<ScoreEntry>(cancellationToken: cancellationToken))
            {
                if (!playerBestScores.TryGetValue(scoreEntry.PlayerInitials, out var existingBest) ||
                    scoreEntry.SurvivalTime > existingBest.SurvivalTime)
                {
                    playerBestScores[scoreEntry.PlayerInitials] = scoreEntry;
                }
            }

            // Sort by survival time and take top 10
            var topScores = playerBestScores.Values
                .OrderByDescending(s => s.SurvivalTime)
                .Take(MaxLeaderboardSize)
                .ToList();

            // Create leaderboard entries
            var entriesCreated = 0;
            for (int i = 0; i < topScores.Count; i++)
            {
                var score = topScores[i];
                var rank = i + 1;
                var submissionCount = await _scoreRepository.GetPlayerSubmissionCountAsync(score.PlayerInitials, cancellationToken);

                var leaderboardEntry = LeaderboardEntry.Create(
                    score.PlayerInitials,
                    score.SurvivalTime,
                    score.ScoreId,
                    rank,
                    submissionCount,
                    score.SubmittedAt
                );

                await _tableClient.AddEntityAsync(leaderboardEntry, cancellationToken);
                entriesCreated++;
            }

            _logger.LogInformation("Leaderboard rebuild completed: {EntriesCreated} entries created", entriesCreated);
            return entriesCreated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rebuild leaderboard");
            throw;
        }
    }

    public async Task<DateTime?> GetLastUpdateTimeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await GetTopEntriesAsync(1, cancellationToken);
            var latestEntry = entries.FirstOrDefault();

            if (latestEntry?.Timestamp != null)
            {
                var lastUpdate = latestEntry.Timestamp.Value.DateTime;
                _logger.LogDebug("Leaderboard last updated: {LastUpdate}", lastUpdate);
                return lastUpdate;
            }

            _logger.LogDebug("Leaderboard has no entries - no last update time");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get leaderboard last update time");
            throw;
        }
    }

    /// <summary>
    /// Updates ranks for all leaderboard entries to ensure consistency
    /// </summary>
    private async Task UpdateRanksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await GetTopEntriesAsync(MaxLeaderboardSize, cancellationToken);

            // Sort by survival time descending to get correct ranks
            entries.Sort((a, b) => b.SurvivalTime.CompareTo(a.SurvivalTime));

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var correctRank = i + 1;

                if (entry.Rank != correctRank)
                {
                    // Delete old entry and create new one with correct rank
                    await _tableClient.DeleteEntityAsync(entry.PartitionKey, entry.RowKey, cancellationToken: cancellationToken);

                    var updatedEntry = LeaderboardEntry.Create(
                        entry.PlayerInitials,
                        entry.SurvivalTime,
                        entry.ScoreId,
                        correctRank,
                        entry.TotalSubmissions,
                        DateTime.Parse(entry.SubmittedAt)
                    );

                    await _tableClient.AddEntityAsync(updatedEntry, cancellationToken);

                    _logger.LogDebug("Updated rank for player {PlayerInitials} from {OldRank} to {NewRank}",
                        entry.PlayerInitials, entry.Rank, correctRank);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update leaderboard ranks");
            throw;
        }
    }

    /// <summary>
    /// Clears all entries from the leaderboard
    /// </summary>
    private async Task ClearLeaderboardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = TableClient.CreateQueryFilter($"PartitionKey eq {LeaderboardEntry.PartitionKeyValue}");
            var entriesToDelete = new List<LeaderboardEntry>();

            await foreach (var entry in _tableClient.QueryAsync<LeaderboardEntry>(filter, cancellationToken: cancellationToken))
            {
                entriesToDelete.Add(entry);
            }

            foreach (var entry in entriesToDelete)
            {
                await _tableClient.DeleteEntityAsync(entry.PartitionKey, entry.RowKey, cancellationToken: cancellationToken);
            }

            _logger.LogDebug("Cleared {Count} entries from leaderboard", entriesToDelete.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear leaderboard");
            throw;
        }
    }
}