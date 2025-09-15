using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Po.PoDropSquare.Core.Entities;

namespace Po.PoDropSquare.Data.Repositories;

/// <summary>
/// Azure Table Storage implementation of score repository
/// </summary>
public class ScoreRepository : IScoreRepository
{
    private readonly TableClient _tableClient;
    private readonly ILogger<ScoreRepository> _logger;
    private const string TableName = "Scores";

    public ScoreRepository(TableServiceClient tableServiceClient, ILogger<ScoreRepository> logger)
    {
        _tableClient = tableServiceClient.GetTableClient(TableName);
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
            _logger.LogInformation("Scores table initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Scores table");
            throw;
        }
    }

    public async Task<ScoreEntry> SubmitScoreAsync(ScoreEntry scoreEntry, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Submitting score for player {PlayerInitials}: {SurvivalTime}s",
                scoreEntry.PlayerInitials, scoreEntry.SurvivalTime);

            var response = await _tableClient.AddEntityAsync(scoreEntry, cancellationToken);

            _logger.LogInformation("Score submitted successfully with ID {ScoreId}", scoreEntry.ScoreId);
            return scoreEntry;
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            _logger.LogWarning("Score entry already exists: {ScoreId}", scoreEntry.ScoreId);
            throw new InvalidOperationException($"Score entry {scoreEntry.ScoreId} already exists", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit score for player {PlayerInitials}", scoreEntry.PlayerInitials);
            throw;
        }
    }

    public async Task<List<ScoreEntry>> GetPlayerScoresAsync(string playerInitials, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = TableClient.CreateQueryFilter($"PartitionKey eq {playerInitials}");
            var scores = new List<ScoreEntry>();

            await foreach (var scoreEntry in _tableClient.QueryAsync<ScoreEntry>(filter, cancellationToken: cancellationToken))
            {
                scores.Add(scoreEntry);
            }

            // Sort by survival time descending (best scores first)
            scores.Sort((a, b) => b.SurvivalTime.CompareTo(a.SurvivalTime));

            _logger.LogDebug("Retrieved {Count} scores for player {PlayerInitials}", scores.Count, playerInitials);
            return scores;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve scores for player {PlayerInitials}", playerInitials);
            throw;
        }
    }

    public async Task<ScoreEntry?> GetPlayerBestScoreAsync(string playerInitials, CancellationToken cancellationToken = default)
    {
        try
        {
            var scores = await GetPlayerScoresAsync(playerInitials, cancellationToken);
            var bestScore = scores.FirstOrDefault();

            if (bestScore != null)
            {
                _logger.LogDebug("Best score for player {PlayerInitials}: {SurvivalTime}s",
                    playerInitials, bestScore.SurvivalTime);
            }

            return bestScore;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get best score for player {PlayerInitials}", playerInitials);
            throw;
        }
    }

    public async Task<List<ScoreEntry>> GetScoresByTimeRangeAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        try
        {
            var scores = new List<ScoreEntry>();

            // Azure Table Storage doesn't have efficient time range queries on non-key fields
            // For production, consider partitioning by date or using Azure Cognitive Search
            await foreach (var scoreEntry in _tableClient.QueryAsync<ScoreEntry>(cancellationToken: cancellationToken))
            {
                if (scoreEntry.SubmittedAt >= startTime && scoreEntry.SubmittedAt <= endTime)
                {
                    scores.Add(scoreEntry);
                }
            }

            _logger.LogDebug("Retrieved {Count} scores in time range {StartTime} to {EndTime}",
                scores.Count, startTime, endTime);
            return scores;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve scores by time range");
            throw;
        }
    }

    public async Task<ScoreEntry?> GetScoreByIdAsync(string scoreId, CancellationToken cancellationToken = default)
    {
        try
        {
            // For efficient lookup, we need to search across all partitions
            // In production, consider storing a lookup table or using secondary index
            await foreach (var scoreEntry in _tableClient.QueryAsync<ScoreEntry>(
                filter: TableClient.CreateQueryFilter($"RowKey eq {scoreId}"),
                cancellationToken: cancellationToken))
            {
                _logger.LogDebug("Retrieved score by ID {ScoreId}", scoreId);
                return scoreEntry;
            }

            _logger.LogDebug("Score not found: {ScoreId}", scoreId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve score by ID {ScoreId}", scoreId);
            throw;
        }
    }

    public async Task<int> GetPlayerSubmissionCountAsync(string playerInitials, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = TableClient.CreateQueryFilter($"PartitionKey eq {playerInitials}");
            var count = 0;

            await foreach (var _ in _tableClient.QueryAsync<ScoreEntry>(
                filter,
                select: new[] { "RowKey" }, // Only select row key for efficiency
                cancellationToken: cancellationToken))
            {
                count++;
            }

            _logger.LogDebug("Player {PlayerInitials} has {Count} submissions", playerInitials, count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get submission count for player {PlayerInitials}", playerInitials);
            throw;
        }
    }

    public async Task<int> CleanupOldScoresAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            var entriesToDelete = new List<ScoreEntry>();

            await foreach (var scoreEntry in _tableClient.QueryAsync<ScoreEntry>(cancellationToken: cancellationToken))
            {
                if (scoreEntry.SubmittedAt < cutoffDate)
                {
                    entriesToDelete.Add(scoreEntry);
                }
            }

            var deletedCount = 0;
            foreach (var entry in entriesToDelete)
            {
                try
                {
                    await _tableClient.DeleteEntityAsync(entry.PartitionKey, entry.RowKey, cancellationToken: cancellationToken);
                    deletedCount++;
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    // Entity already deleted, continue
                    _logger.LogDebug("Score entry {ScoreId} already deleted", entry.ScoreId);
                }
            }

            _logger.LogInformation("Cleaned up {DeletedCount} old score entries older than {RetentionDays} days",
                deletedCount, retentionDays);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old scores");
            throw;
        }
    }
}