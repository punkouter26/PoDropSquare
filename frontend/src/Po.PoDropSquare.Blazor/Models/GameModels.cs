namespace Po.PoDropSquare.Blazor.Models;

/// <summary>
/// Represents the current state of the game
/// </summary>
public enum GameState
{
    NotStarted,
    Loading,
    Ready,
    Playing,
    Paused,
    GameOver,
    Error
}

/// <summary>
/// Types of game events that can occur
/// </summary>
public enum GameEventType
{
    GameStarted,
    GamePaused,
    GameResumed,
    GameOver,
    BlockLanded,
    LineCleared,
    GoalReached,
    ScoreChanged
}

/// <summary>
/// Arguments for game events
/// </summary>
public class GameEventArgs
{
    public GameEventType EventType { get; set; }
    public int Score { get; set; }
    public object? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Arguments for score change events
/// </summary>
public class ScoreChangedArgs
{
    public int Score { get; set; }
    public TimeSpan GameTime { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Player score submission data
/// </summary>
public class PlayerScore
{
    public string PlayerName { get; set; } = string.Empty;
    public int Score { get; set; }
    public TimeSpan GameDuration { get; set; }
    public DateTime AchievedAt { get; set; } = DateTime.UtcNow;
    public string? SessionId { get; set; }
}

/// <summary>
/// Leaderboard entry
/// </summary>
public class LeaderboardEntry
{
    public int Rank { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int Score { get; set; }
    public TimeSpan GameDuration { get; set; }
    public DateTime AchievedAt { get; set; }
    public bool IsCurrentPlayer { get; set; }
}

/// <summary>
/// Game configuration settings
/// </summary>
public class GameConfig
{
    public int CanvasWidth { get; set; } = 800;
    public int CanvasHeight { get; set; } = 600;
    public int GoalLineY { get; set; } = 100;
    public int MaxGameTime { get; set; } = 300; // 5 minutes in seconds
    public bool EnableSound { get; set; } = true;
    public bool ShowParticleEffects { get; set; } = true;
    public string ApiBaseUrl { get; set; } = "/api";
}

/// <summary>
/// Timer display configuration
/// </summary>
public class TimerConfig
{
    public int TotalSeconds { get; set; } = 300; // 5 minutes
    public int WarningThreshold { get; set; } = 30; // Show warning when 30 seconds left
    public int CriticalThreshold { get; set; } = 10; // Show critical warning when 10 seconds left
    public bool ShowMilliseconds { get; set; } = false;
}

/// <summary>
/// API response wrapper
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Leaderboard API response
/// </summary>
public class LeaderboardApiResponse
{
    public bool Success { get; set; }
    public List<LeaderboardEntry>? Leaderboard { get; set; }
    public DateTime LastUpdated { get; set; }
    public int TotalEntries { get; set; }
}

/// <summary>
/// Score submission result from API
/// </summary>
public class ScoreSubmissionResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int? Rank { get; set; }
    public bool IsNewHighScore { get; set; }
}