using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Po.PoDropSquare.Core.Contracts;
using Po.PoDropSquare.Core.Entities;
using Po.PoDropSquare.Data.Repositories;

namespace Po.PoDropSquare.Services.Services;

/// <summary>
/// Service implementation for leaderboard operations with caching
/// </summary>
public class LeaderboardService : ILeaderboardService
{
    private readonly ILeaderboardRepository _leaderboardRepository;
    private readonly IScoreRepository _scoreRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LeaderboardService> _logger;

    // Cache configuration
    private const string TopLeaderboardCacheKey = "top_leaderboard_10";
    private const string LeaderboardStatsCacheKey = "leaderboard_stats";
    private static readonly TimeSpan DefaultCacheExpiry = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StatsCacheExpiry = TimeSpan.FromMinutes(10);

    public LeaderboardService(
        ILeaderboardRepository leaderboardRepository,
        IScoreRepository scoreRepository,
        IMemoryCache cache,
        ILogger<LeaderboardService> logger)
    {
        _leaderboardRepository = leaderboardRepository;
        _scoreRepository = scoreRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<LeaderboardResponse> GetTopLeaderboardAsync(int topN = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving top {TopN} leaderboard entries", topN);

            // Check cache first (only for standard top 10 requests)
            string cacheKey = topN == 10 ? TopLeaderboardCacheKey : $"top_leaderboard_{topN}";

            if (_cache.TryGetValue(cacheKey, out LeaderboardResponse? cachedResponse) && cachedResponse != null)
            {
                _logger.LogDebug("Returning cached leaderboard data");
                return cachedResponse;
            }

            // Get data from repository
            var entries = await _leaderboardRepository.GetTopEntriesAsync(topN, cancellationToken);
            var lastUpdated = await _leaderboardRepository.GetLastUpdateTimeAsync(cancellationToken);
            var totalEntries = await _leaderboardRepository.GetTotalLeaderboardEntriesAsync(cancellationToken);

            // Convert to DTOs
            var leaderboardDtos = entries.Select(entry => new LeaderboardEntryDto
            {
                Rank = entry.Rank,
                PlayerInitials = entry.PlayerInitials,
                SurvivalTime = entry.SurvivalTime,
                SubmittedAt = entry.SubmittedAt, // Already in ISO 8601 format
                ScoreId = entry.ScoreId,
                TotalSubmissions = entry.TotalSubmissions
            }).ToList();

            // Create response
            var response = new LeaderboardResponse
            {
                Success = true,
                Leaderboard = leaderboardDtos,
                LastUpdated = lastUpdated?.ToString("O") ?? DateTime.UtcNow.ToString("O"),
                TotalEntries = totalEntries,
                CacheInfo = new CacheInfo
                {
                    ETag = entries.Count == 0 ? "empty" :
                           Po.PoDropSquare.Core.Utilities.ETagGenerator.Generate(
                               entries.Select(e => $"{e.PlayerInitials}:{e.SurvivalTime}:{e.Rank}").ToArray()),
                    Expires = DateTime.UtcNow.Add(DefaultCacheExpiry),
                    MaxAge = (int)DefaultCacheExpiry.TotalSeconds
                }
            };

            // Cache the response (only for reasonable cache sizes)
            if (topN <= 50)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = DefaultCacheExpiry,
                    Priority = CacheItemPriority.Normal
                };

                _cache.Set(cacheKey, response, cacheOptions);
                _logger.LogDebug("Cached leaderboard data for {CacheKey}", cacheKey);
            }

            _logger.LogInformation("Retrieved top {TopN} leaderboard with {Count} entries", topN, leaderboardDtos.Count);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve top {TopN} leaderboard", topN);

            return new LeaderboardResponse
            {
                Success = false,
                Leaderboard = new List<LeaderboardEntryDto>(),
                LastUpdated = DateTime.UtcNow.ToString("O"),
                TotalEntries = 0
            };
        }
    }

    public async Task<LeaderboardEntryDto?> GetPlayerLeaderboardPositionAsync(string playerInitials, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting leaderboard position for player {PlayerInitials}", playerInitials);

            var entry = await _leaderboardRepository.GetPlayerLeaderboardEntryAsync(playerInitials, cancellationToken);

            if (entry == null)
            {
                _logger.LogDebug("Player {PlayerInitials} not found on leaderboard", playerInitials);
                return null;
            }

            var dto = new LeaderboardEntryDto
            {
                Rank = entry.Rank,
                PlayerInitials = entry.PlayerInitials,
                SurvivalTime = entry.SurvivalTime,
                SubmittedAt = entry.SubmittedAt, // Already in ISO 8601 format
                ScoreId = entry.ScoreId,
                TotalSubmissions = entry.TotalSubmissions
            };

            _logger.LogDebug("Player {PlayerInitials} is at rank {Rank} with score {SurvivalTime}s",
                playerInitials, entry.Rank, entry.SurvivalTime);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get leaderboard position for player {PlayerInitials}", playerInitials);
            throw;
        }
    }

    public async Task<bool> WouldQualifyForLeaderboardAsync(double survivalTime, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _leaderboardRepository.WouldQualifyForLeaderboardAsync(survivalTime, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if score {SurvivalTime} would qualify for leaderboard", survivalTime);
            throw;
        }
    }

    public async Task<int> RebuildLeaderboardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting leaderboard rebuild");

            var entriesCreated = await _leaderboardRepository.RebuildLeaderboardAsync(cancellationToken);

            // Clear relevant caches
            ClearLeaderboardCaches();

            _logger.LogInformation("Leaderboard rebuild completed: {EntriesCreated} entries created", entriesCreated);
            return entriesCreated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rebuild leaderboard");
            throw;
        }
    }

    public async Task<LeaderboardStats> GetLeaderboardStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            if (_cache.TryGetValue(LeaderboardStatsCacheKey, out LeaderboardStats? cachedStats) && cachedStats != null)
            {
                _logger.LogDebug("Returning cached leaderboard stats");
                return cachedStats;
            }

            _logger.LogDebug("Calculating leaderboard statistics");

            var totalEntries = await _leaderboardRepository.GetTotalLeaderboardEntriesAsync(cancellationToken);
            var lastUpdated = await _leaderboardRepository.GetLastUpdateTimeAsync(cancellationToken);
            var topEntries = await _leaderboardRepository.GetTopEntriesAsync(10, cancellationToken);

            var stats = new LeaderboardStats
            {
                TotalEntries = totalEntries,
                LastUpdated = lastUpdated
            };

            if (topEntries.Count > 0)
            {
                stats.HighestScore = topEntries.First().SurvivalTime;
                stats.LowestScore = topEntries.Last().SurvivalTime;
                stats.AverageScore = topEntries.Average(e => e.SurvivalTime);
            }

            // Cache the stats
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = StatsCacheExpiry,
                Priority = CacheItemPriority.Low
            };

            _cache.Set(LeaderboardStatsCacheKey, stats, cacheOptions);
            _logger.LogDebug("Cached leaderboard statistics");

            _logger.LogDebug("Leaderboard stats: {TotalEntries} entries, highest: {HighestScore}s, lowest: {LowestScore}s",
                stats.TotalEntries, stats.HighestScore, stats.LowestScore);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get leaderboard statistics");
            throw;
        }
    }

    /// <summary>
    /// Invalidates leaderboard caches when data changes
    /// </summary>
    public void InvalidateLeaderboardCache()
    {
        try
        {
            ClearLeaderboardCaches();
            _logger.LogDebug("Leaderboard caches invalidated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate leaderboard cache");
        }
    }

    /// <summary>
    /// Gets the cache expiry time for leaderboard data
    /// </summary>
    public TimeSpan GetCacheExpiry() => DefaultCacheExpiry;

    /// <summary>
    /// Clears all leaderboard-related caches
    /// </summary>
    private void ClearLeaderboardCaches()
    {
        var cacheKeysToRemove = new[]
        {
            TopLeaderboardCacheKey,
            LeaderboardStatsCacheKey
        };

        foreach (var key in cacheKeysToRemove)
        {
            _cache.Remove(key);
        }

        // Remove any custom top N caches (this is a simplified approach)
        // In production, you might want to use a more sophisticated cache invalidation strategy
        for (int i = 1; i <= 100; i++)
        {
            _cache.Remove($"top_leaderboard_{i}");
        }
    }
}