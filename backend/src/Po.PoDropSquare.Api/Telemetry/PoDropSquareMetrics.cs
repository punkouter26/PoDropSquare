using System.Diagnostics.Metrics;

namespace Po.PoDropSquare.Api.Telemetry;

/// <summary>
/// Provides custom metrics (Meter) for business intelligence.
/// Use this to track counters, histograms, and gauges for game metrics.
/// </summary>
public class PoDropSquareMetrics
{
    private readonly Meter _meter;

    // Counters
    private readonly Counter<long> _scoresSubmitted;
    private readonly Counter<long> _scoresRejected;
    private readonly Counter<long> _leaderboardRequests;
    private readonly Counter<long> _rankLookups;
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly Counter<long> _validationErrors;

    // Histograms
    private readonly Histogram<double> _survivalTimeDistribution;
    private readonly Histogram<int> _scoreDistribution;
    private readonly Histogram<double> _requestDuration;

    // ObservableGauges (updated via callbacks)
    private long _activePlayers;
    private long _totalScores;

    public PoDropSquareMetrics()
    {
        _meter = new Meter(PoDropSquareTelemetry.ServiceName, PoDropSquareTelemetry.ServiceVersion);

        // Counters - monotonically increasing values
        _scoresSubmitted = _meter.CreateCounter<long>(
            "game.scores.submitted",
            unit: "scores",
            description: "Total number of scores submitted");

        _scoresRejected = _meter.CreateCounter<long>(
            "game.scores.rejected",
            unit: "scores",
            description: "Total number of scores rejected (validation failures)");

        _leaderboardRequests = _meter.CreateCounter<long>(
            "game.leaderboard.requests",
            unit: "requests",
            description: "Total number of leaderboard requests");

        _rankLookups = _meter.CreateCounter<long>(
            "game.rank.lookups",
            unit: "requests",
            description: "Total number of player rank lookups");

        _cacheHits = _meter.CreateCounter<long>(
            "cache.hits",
            unit: "hits",
            description: "Total number of cache hits");

        _cacheMisses = _meter.CreateCounter<long>(
            "cache.misses",
            unit: "misses",
            description: "Total number of cache misses");

        _validationErrors = _meter.CreateCounter<long>(
            "validation.errors",
            unit: "errors",
            description: "Total number of validation errors");

        // Histograms - value distributions
        _survivalTimeDistribution = _meter.CreateHistogram<double>(
            "game.survival_time",
            unit: "seconds",
            description: "Distribution of player survival times");

        _scoreDistribution = _meter.CreateHistogram<int>(
            "game.score.value",
            unit: "points",
            description: "Distribution of calculated scores");

        _requestDuration = _meter.CreateHistogram<double>(
            "http.request.duration",
            unit: "milliseconds",
            description: "HTTP request duration");

        // Observable Gauges - current values (updated via callbacks)
        _meter.CreateObservableGauge(
            "game.players.active",
            () => _activePlayers,
            unit: "players",
            description: "Current number of active players");

        _meter.CreateObservableGauge(
            "game.scores.total",
            () => _totalScores,
            unit: "scores",
            description: "Total number of scores in the system");
    }

    // Counter methods
    public void RecordScoreSubmitted(string playerInitials, double survivalTime, int calculatedScore)
    {
        _scoresSubmitted.Add(1, new KeyValuePair<string, object?>("player.initials", playerInitials));
        _survivalTimeDistribution.Record(survivalTime);
        _scoreDistribution.Record(calculatedScore);
    }

    public void RecordScoreRejected(string reason)
    {
        _scoresRejected.Add(1, new KeyValuePair<string, object?>("rejection.reason", reason));
    }

    public void RecordLeaderboardRequest(int count)
    {
        _leaderboardRequests.Add(1, new KeyValuePair<string, object?>("leaderboard.size", count));
    }

    public void RecordRankLookup(string playerInitials, bool found)
    {
        _rankLookups.Add(1,
            new KeyValuePair<string, object?>("player.initials", playerInitials),
            new KeyValuePair<string, object?>("found", found));
    }

    public void RecordCacheHit(string cacheKey)
    {
        _cacheHits.Add(1, new KeyValuePair<string, object?>("cache.key", cacheKey));
    }

    public void RecordCacheMiss(string cacheKey)
    {
        _cacheMisses.Add(1, new KeyValuePair<string, object?>("cache.key", cacheKey));
    }

    public void RecordValidationError(string field, string error)
    {
        _validationErrors.Add(1,
            new KeyValuePair<string, object?>("validation.field", field),
            new KeyValuePair<string, object?>("validation.error", error));
    }

    public void RecordRequestDuration(double durationMs, string endpoint, int statusCode)
    {
        _requestDuration.Record(durationMs,
            new KeyValuePair<string, object?>("http.endpoint", endpoint),
            new KeyValuePair<string, object?>("http.status_code", statusCode));
    }

    // Observable gauge updaters
    public void UpdateActivePlayers(long count)
    {
        _activePlayers = count;
    }

    public void UpdateTotalScores(long count)
    {
        _totalScores = count;
    }
}
