using System.Diagnostics;

namespace Po.PoDropSquare.Api.Telemetry;

/// <summary>
/// Provides ActivitySource for distributed tracing with custom spans.
/// Use this to create Activities (spans) for important business operations.
/// </summary>
public static class PoDropSquareTelemetry
{
    /// <summary>
    /// Service name for telemetry (matches Application Insights cloud_RoleName)
    /// </summary>
    public const string ServiceName = "PoDropSquare.Api";

    /// <summary>
    /// Service version for telemetry
    /// </summary>
    public const string ServiceVersion = "1.0.0";

    /// <summary>
    /// ActivitySource for creating custom spans
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ServiceName, ServiceVersion);

    /// <summary>
    /// Common activity tags
    /// </summary>
    public static class Tags
    {
        public const string PlayerInitials = "game.player.initials";
        public const string Score = "game.score";
        public const string SurvivalTime = "game.survival_time";
        public const string Rank = "game.rank";
        public const string LeaderboardSize = "game.leaderboard.size";
        public const string Operation = "game.operation";
        public const string CacheHit = "cache.hit";
        public const string CacheMiss = "cache.miss";
        public const string ValidationError = "validation.error";
    }

    /// <summary>
    /// Common activity event names
    /// </summary>
    public static class Events
    {
        public const string ScoreSubmitted = "score.submitted";
        public const string ScoreValidationFailed = "score.validation_failed";
        public const string LeaderboardRequested = "leaderboard.requested";
        public const string RankCalculated = "rank.calculated";
        public const string CacheHit = "cache.hit";
        public const string CacheMiss = "cache.miss";
    }
}
