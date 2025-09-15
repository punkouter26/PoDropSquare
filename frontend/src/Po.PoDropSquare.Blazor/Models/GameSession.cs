using System.Text.Json.Serialization;

namespace Po.PoDropSquare.Blazor.Models;

/// <summary>
/// Represents the current state of a game session on the client side.
/// Tracks gameplay progress, physics state, and session validation data.
/// </summary>
public class GameSession
{
    /// <summary>
    /// Unique identifier for this game session
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Current state of the game
    /// </summary>
    public GameState State { get; set; } = GameState.NotStarted;

    /// <summary>
    /// Player's initials (1-3 uppercase alphanumeric characters)
    /// </summary>
    public string PlayerInitials { get; set; } = string.Empty;

    /// <summary>
    /// Current survival time in seconds
    /// </summary>
    public double CurrentSurvivalTime { get; set; }

    /// <summary>
    /// Maximum survival time achieved in this session
    /// </summary>
    public double BestSurvivalTime { get; set; }

    /// <summary>
    /// When the current game started
    /// </summary>
    public DateTime GameStartTime { get; set; }

    /// <summary>
    /// When the current game ended (if applicable)
    /// </summary>
    public DateTime? GameEndTime { get; set; }

    /// <summary>
    /// Total number of blocks placed in this session
    /// </summary>
    public int BlocksPlaced { get; set; }

    /// <summary>
    /// Total number of games played in this session
    /// </summary>
    public int GamesPlayed { get; set; }

    /// <summary>
    /// Collection of blocks currently in the game world
    /// </summary>
    public List<Block> ActiveBlocks { get; set; } = new();

    /// <summary>
    /// Physics engine state for session validation
    /// </summary>
    public PhysicsState Physics { get; set; } = new();

    /// <summary>
    /// Input events recorded during the session for anti-cheat validation
    /// </summary>
    public List<InputEvent> InputHistory { get; set; } = new();

    /// <summary>
    /// Performance metrics for this session
    /// </summary>
    public SessionMetrics Metrics { get; set; } = new();

    /// <summary>
    /// Cryptographic signature of session data for validation
    /// </summary>
    public string SessionSignature { get; set; } = string.Empty;

    /// <summary>
    /// Starts a new game within this session
    /// </summary>
    public void StartGame()
    {
        State = GameState.Playing;
        GameStartTime = DateTime.UtcNow;
        GameEndTime = null;
        CurrentSurvivalTime = 0;
        BlocksPlaced = 0;
        ActiveBlocks.Clear();
        InputHistory.Clear();
        Physics.Reset();
        GamesPlayed++;
    }

    /// <summary>
    /// Ends the current game and updates session statistics
    /// </summary>
    /// <param name="finalSurvivalTime">Final survival time achieved</param>
    public void EndGame(double finalSurvivalTime)
    {
        State = GameState.GameOver;
        GameEndTime = DateTime.UtcNow;
        CurrentSurvivalTime = finalSurvivalTime;

        if (finalSurvivalTime > BestSurvivalTime)
        {
            BestSurvivalTime = finalSurvivalTime;
        }

        UpdateSessionSignature();
    }

    /// <summary>
    /// Adds a block to the active game
    /// </summary>
    /// <param name="block">Block to add</param>
    public void AddBlock(Block block)
    {
        ActiveBlocks.Add(block);
        BlocksPlaced++;
        Physics.UpdatePhysicsState(block);
    }

    /// <summary>
    /// Records an input event for validation
    /// </summary>
    /// <param name="inputEvent">Input event to record</param>
    public void RecordInput(InputEvent inputEvent)
    {
        InputHistory.Add(inputEvent);

        // Limit history size to prevent memory issues
        if (InputHistory.Count > 1000)
        {
            InputHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// Updates the session signature based on current state
    /// </summary>
    private void UpdateSessionSignature()
    {
        var sessionData = $"{SessionId}|{CurrentSurvivalTime}|{BlocksPlaced}|{GameStartTime:O}|{Physics.GetChecksum()}";
        SessionSignature = ComputeSignature(sessionData);
    }

    /// <summary>
    /// Computes a cryptographic signature for the given data
    /// </summary>
    /// <param name="data">Data to sign</param>
    /// <returns>SHA-256 hash as hex string</returns>
    private static string ComputeSignature(string data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Validates the current session state
    /// </summary>
    /// <returns>Validation result</returns>
    public ValidationResult Validate()
    {
        // Validate player initials
        if (string.IsNullOrEmpty(PlayerInitials))
            return ValidationResult.Invalid("Player initials are required");

        if (PlayerInitials.Length < 1 || PlayerInitials.Length > 3)
            return ValidationResult.Invalid("Player initials must be 1-3 characters");

        if (!PlayerInitials.All(char.IsLetterOrDigit))
            return ValidationResult.Invalid("Player initials must be alphanumeric");

        if (!PlayerInitials.All(char.IsUpper))
            return ValidationResult.Invalid("Player initials must be uppercase");

        // Validate survival time
        if (CurrentSurvivalTime < 0)
            return ValidationResult.Invalid("Survival time cannot be negative");

        if (CurrentSurvivalTime > 20.0)
            return ValidationResult.Invalid("Survival time cannot exceed 20 seconds");

        // Validate game timing
        if (State == GameState.Playing && GameStartTime > DateTime.UtcNow)
            return ValidationResult.Invalid("Game start time cannot be in the future");

        if (GameEndTime.HasValue && GameEndTime < GameStartTime)
            return ValidationResult.Invalid("Game end time cannot be before start time");

        return ValidationResult.Valid();
    }

    /// <summary>
    /// Creates a score submission request from this session
    /// </summary>
    /// <returns>Score submission request</returns>
    public object ToScoreSubmissionRequest()
    {
        UpdateSessionSignature();

        return new
        {
            playerInitials = PlayerInitials,
            survivalTime = Math.Round(CurrentSurvivalTime, 2),
            sessionSignature = SessionSignature,
            clientTimestamp = DateTime.UtcNow.ToString("O")
        };
    }
}

/// <summary>
/// Physics state for session validation
/// </summary>
public class PhysicsState
{
    public double WorldGravity { get; set; } = -9.81;
    public int PhysicsSteps { get; set; }
    public double TotalPhysicsTime { get; set; }
    public List<string> CollisionEvents { get; set; } = new();

    public void Reset()
    {
        PhysicsSteps = 0;
        TotalPhysicsTime = 0;
        CollisionEvents.Clear();
    }

    public void UpdatePhysicsState(Block block)
    {
        PhysicsSteps++;
        TotalPhysicsTime += 0.016; // Assuming 60 FPS
    }

    public string GetChecksum()
    {
        var data = $"{PhysicsSteps}|{TotalPhysicsTime:F3}|{CollisionEvents.Count}";
        return data.GetHashCode().ToString("X");
    }
}

/// <summary>
/// Input event for session validation
/// </summary>
public class InputEvent
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public InputType Type { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public string? Key { get; set; }
}

/// <summary>
/// Input type enumeration
/// </summary>
public enum InputType
{
    MouseClick,
    MouseMove,
    KeyPress,
    Touch
}

/// <summary>
/// Session performance metrics
/// </summary>
public class SessionMetrics
{
    public double AverageFrameRate { get; set; }
    public double PeakFrameRate { get; set; }
    public double MinFrameRate { get; set; } = 60;
    public int TotalFrames { get; set; }
    public double SessionDuration { get; set; }

    public void UpdateFrameRate(double currentFps)
    {
        TotalFrames++;

        if (currentFps > PeakFrameRate)
            PeakFrameRate = currentFps;

        if (currentFps < MinFrameRate)
            MinFrameRate = currentFps;

        AverageFrameRate = ((AverageFrameRate * (TotalFrames - 1)) + currentFps) / TotalFrames;
    }
}

/// <summary>
/// Validation result for session validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }

    public static ValidationResult Valid() => new() { IsValid = true };
    public static ValidationResult Invalid(string message) => new() { IsValid = false, ErrorMessage = message };
}