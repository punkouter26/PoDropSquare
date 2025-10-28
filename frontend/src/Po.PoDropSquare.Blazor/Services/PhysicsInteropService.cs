using Microsoft.JSInterop;
using Po.PoDropSquare.Blazor.Models;
using System.Text.Json;

namespace Po.PoDropSquare.Blazor.Services;

/// <summary>
/// Service for interacting with the JavaScript physics engine (Matter.js)
/// Provides a bridge between Blazor C# code and JavaScript physics simulation
/// </summary>
public class PhysicsInteropService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _physicsModule;
    private bool _isInitialized;
    private readonly List<string> _activeBlocks = new();

    // Events for physics callbacks
    public event Action<string>? GoalLineCrossed;
    public event Action<string, string>? BlockCollision;
    public event Action<ScoreUpdateEventArgs>? ScoreUpdate;
    public event Action<GameStateChangeEventArgs>? GameStateChange;

    // Danger countdown events
    public event Action? DangerCountdownStarted;
    public event Action<double>? DangerCountdownUpdate;
    public event Action? DangerCountdownCancelled;
    public event Action? DangerGameOver;

    public PhysicsInteropService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Initialize the physics engine with the specified canvas
    /// </summary>
    /// <param name="canvasId">HTML canvas element ID</param>
    /// <returns>True if initialization was successful</returns>
    public async Task<bool> InitializeAsync(string canvasId)
    {
        try
        {
            if (_isInitialized)
            {
                await DisposeAsync();
            }

            // Initialize the physics engine with callbacks
            var callbacks = new
            {
                onGoalLineCrossed = "PhysicsInteropService.OnGoalLineCrossed",
                onBlockCollision = "PhysicsInteropService.OnBlockCollision",
                onScoreUpdate = "PhysicsInteropService.OnScoreUpdate",
                onGameStateChange = "PhysicsInteropService.OnGameStateChange"
            };

            _isInitialized = await _jsRuntime.InvokeAsync<bool>("initializePhysics", canvasId, callbacks);

            if (_isInitialized)
            {
                // Set up .NET callback references
                await SetupCallbacksAsync();
                Console.WriteLine("Physics engine initialized successfully");
            }
            else
            {
                Console.Error.WriteLine("Failed to initialize physics engine");
            }

            return _isInitialized;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error initializing physics engine: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Create a new block in the physics world
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="color">Block color</param>
    /// <param name="options">Additional block options</param>
    /// <returns>Block ID if successful, null otherwise</returns>
    public async Task<string?> CreateBlockAsync(float x, float y, string color, BlockCreateOptions? options = null)
    {
        try
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Physics engine not initialized");
            }

            var jsOptions = options != null ? new
            {
                width = options.Width,
                height = options.Height,
                restitution = options.Restitution,
                friction = options.Friction,
                frictionAir = options.FrictionAir,
                density = options.Density,
                physicsOptions = options.PhysicsOptions
            } : null;

            var blockId = await _jsRuntime.InvokeAsync<string>("createBlock", x, y, color, jsOptions);

            if (!string.IsNullOrEmpty(blockId))
            {
                _activeBlocks.Add(blockId);
                Console.WriteLine($"Created block {blockId} at ({x}, {y})");
            }

            return blockId;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error creating block: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Remove a block from the physics world
    /// </summary>
    /// <param name="blockId">Block ID to remove</param>
    /// <returns>True if removal was successful</returns>
    public async Task<bool> RemoveBlockAsync(string blockId)
    {
        try
        {
            if (!_isInitialized)
            {
                return false;
            }

            var success = await _jsRuntime.InvokeAsync<bool>("removeBlock", blockId);

            if (success)
            {
                _activeBlocks.Remove(blockId);
                Console.WriteLine($"Removed block {blockId}");
            }

            return success;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error removing block {blockId}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get current states of all blocks
    /// </summary>
    /// <returns>List of block states</returns>
    public async Task<List<BlockState>> GetBlockStatesAsync()
    {
        try
        {
            if (!_isInitialized)
            {
                return new List<BlockState>();
            }

            var blockData = await _jsRuntime.InvokeAsync<JsonElement>("getBlockStates");
            var blocks = new List<BlockState>();

            if (blockData.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in blockData.EnumerateArray())
                {
                    var block = new BlockState
                    {
                        Id = element.GetProperty("id").GetString() ?? string.Empty,
                        X = element.GetProperty("x").GetSingle(),
                        Y = element.GetProperty("y").GetSingle(),
                        Angle = element.GetProperty("angle").GetSingle(),
                        Color = element.GetProperty("color").GetString() ?? "#4834d4",
                        VelocityX = element.GetProperty("velocity").GetProperty("x").GetSingle(),
                        VelocityY = element.GetProperty("velocity").GetProperty("y").GetSingle()
                    };
                    blocks.Add(block);
                }
            }

            return blocks;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting block states: {ex.Message}");
            return new List<BlockState>();
        }
    }

    /// <summary>
    /// Check if the game over condition has been met
    /// </summary>
    /// <returns>True if game over</returns>
    public async Task<bool> CheckGameOverAsync()
    {
        try
        {
            if (!_isInitialized)
            {
                return false;
            }

            return await _jsRuntime.InvokeAsync<bool>("checkGameOver");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error checking game over: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Clear all blocks from the physics world
    /// </summary>
    /// <returns>True if successful</returns>
    public async Task<bool> ClearAllBlocksAsync()
    {
        try
        {
            if (!_isInitialized)
            {
                return false;
            }

            var success = await _jsRuntime.InvokeAsync<bool>("clearAllBlocks");

            if (success)
            {
                _activeBlocks.Clear();
                Console.WriteLine("Cleared all blocks");
            }

            return success;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error clearing blocks: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get current performance statistics
    /// </summary>
    /// <returns>Performance data</returns>
    public async Task<PhysicsPerformanceStats?> GetPerformanceStatsAsync()
    {
        try
        {
            if (!_isInitialized)
            {
                return null;
            }

            var statsData = await _jsRuntime.InvokeAsync<JsonElement>("getPerformanceStats");

            return new PhysicsPerformanceStats
            {
                Fps = statsData.GetProperty("fps").GetInt32(),
                FrameTime = statsData.GetProperty("frameTime").GetSingle(),
                FrameCount = statsData.GetProperty("frameCount").GetInt64(),
                BlockCount = statsData.GetProperty("blockCount").GetInt32(),
                IsRunning = statsData.GetProperty("isRunning").GetBoolean(),
                MemoryUsage = statsData.TryGetProperty("memoryUsage", out var memProp) && memProp.ValueKind != JsonValueKind.Null
                    ? new MemoryUsageStats
                    {
                        Used = memProp.GetProperty("used").GetInt32(),
                        Total = memProp.GetProperty("total").GetInt32(),
                        Limit = memProp.GetProperty("limit").GetInt32()
                    }
                    : null
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting performance stats: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Pause or resume the physics simulation
    /// </summary>
    /// <param name="paused">Whether to pause the simulation</param>
    /// <returns>True if operation was successful</returns>
    public async Task<bool> SetPausedAsync(bool paused)
    {
        try
        {
            if (!_isInitialized)
            {
                return false;
            }

            return await _jsRuntime.InvokeAsync<bool>("pausePhysics", paused);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error setting pause state: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Setup callback functions for JavaScript to call
    /// </summary>
    private async Task SetupCallbacksAsync()
    {
        try
        {
            // Register .NET callback methods with JavaScript
            var dotNetReference = DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("PhysicsInteropService.setupCallbacks", dotNetReference);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error setting up callbacks: {ex.Message}");
        }
    }

    /// <summary>
    /// JavaScript callback for goal line crossed events
    /// </summary>
    [JSInvokable]
    public void OnGoalLineCrossed(string blockId)
    {
        try
        {
            Console.WriteLine($"Goal line crossed by block: {blockId}");
            GoalLineCrossed?.Invoke(blockId);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error in OnGoalLineCrossed callback: {ex.Message}");
        }
    }

    /// <summary>
    /// JavaScript callback for block collision events
    /// </summary>
    [JSInvokable]
    public void OnBlockCollision(string blockIdA, string blockIdB)
    {
        try
        {
            BlockCollision?.Invoke(blockIdA, blockIdB);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error in OnBlockCollision callback: {ex.Message}");
        }
    }

    /// <summary>
    /// JavaScript callback for score update events
    /// </summary>
    [JSInvokable]
    public void OnScoreUpdate(JsonElement data)
    {
        try
        {
            var args = new ScoreUpdateEventArgs
            {
                Action = data.GetProperty("action").GetString() ?? string.Empty,
                BlockId = data.TryGetProperty("blockId", out var blockIdProp) ? blockIdProp.GetString() : null,
                TotalBlocks = data.TryGetProperty("totalBlocks", out var totalProp) ? totalProp.GetInt32() : 0
            };

            Console.WriteLine($"Score update: {args.Action} - Total blocks: {args.TotalBlocks}");
            ScoreUpdate?.Invoke(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error in OnScoreUpdate callback: {ex.Message}");
        }
    }

    /// <summary>
    /// JavaScript callback for game state change events
    /// </summary>
    [JSInvokable]
    public void OnGameStateChange(string state, JsonElement? data = null)
    {
        try
        {
            var args = new GameStateChangeEventArgs
            {
                State = state,
                Data = data
            };

            Console.WriteLine($"Game state changed: {state}");
            GameStateChange?.Invoke(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error in OnGameStateChange callback: {ex.Message}");
        }
    }

    /// <summary>
    /// JavaScript callback for danger countdown started
    /// </summary>
    [JSInvokable]
    public void OnDangerCountdownStarted()
    {
        try
        {
            Console.WriteLine("Danger countdown started!");
            DangerCountdownStarted?.Invoke();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error in OnDangerCountdownStarted callback: {ex.Message}");
        }
    }

    /// <summary>
    /// JavaScript callback for danger countdown updates
    /// </summary>
    [JSInvokable]
    public void OnDangerCountdownUpdate(double remainingSeconds)
    {
        try
        {
            DangerCountdownUpdate?.Invoke(remainingSeconds);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error in OnDangerCountdownUpdate callback: {ex.Message}");
        }
    }

    /// <summary>
    /// JavaScript callback for danger countdown cancelled
    /// </summary>
    [JSInvokable]
    public void OnDangerCountdownCancelled()
    {
        try
        {
            Console.WriteLine("Danger countdown cancelled - blocks moved below line");
            DangerCountdownCancelled?.Invoke();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error in OnDangerCountdownCancelled callback: {ex.Message}");
        }
    }

    /// <summary>
    /// JavaScript callback for danger game over
    /// </summary>
    [JSInvokable]
    public void OnDangerGameOver()
    {
        try
        {
            Console.WriteLine("Danger game over - countdown expired!");
            DangerGameOver?.Invoke();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error in OnDangerGameOver callback: {ex.Message}");
        }
    }

    /// <summary>
    /// Get list of active block IDs
    /// </summary>
    public IReadOnlyList<string> ActiveBlocks => _activeBlocks.AsReadOnly();

    /// <summary>
    /// Whether the physics engine is initialized
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Dispose of the physics engine and cleanup resources
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_isInitialized)
            {
                await _jsRuntime.InvokeVoidAsync("disposePhysics");
                _isInitialized = false;
            }

            _activeBlocks.Clear();

            if (_physicsModule != null)
            {
                await _physicsModule.DisposeAsync();
                _physicsModule = null;
            }

            Console.WriteLine("PhysicsInteropService disposed");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error disposing PhysicsInteropService: {ex.Message}");
        }
    }
}

/// <summary>
/// Options for creating blocks
/// </summary>
public class BlockCreateOptions
{
    public float? Width { get; set; }
    public float? Height { get; set; }
    public float? Restitution { get; set; }
    public float? Friction { get; set; }
    public float? FrictionAir { get; set; }
    public float? Density { get; set; }
    public Dictionary<string, object>? PhysicsOptions { get; set; }
}

/// <summary>
/// Current state of a physics block
/// </summary>
public class BlockState
{
    public string Id { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public float Angle { get; set; }
    public string Color { get; set; } = string.Empty;
    public float VelocityX { get; set; }
    public float VelocityY { get; set; }
}

/// <summary>
/// Physics engine performance statistics
/// </summary>
public class PhysicsPerformanceStats
{
    public int Fps { get; set; }
    public float FrameTime { get; set; }
    public long FrameCount { get; set; }
    public int BlockCount { get; set; }
    public bool IsRunning { get; set; }
    public MemoryUsageStats? MemoryUsage { get; set; }
}

/// <summary>
/// Memory usage statistics
/// </summary>
public class MemoryUsageStats
{
    public int Used { get; set; }  // MB
    public int Total { get; set; } // MB
    public int Limit { get; set; } // MB
}

/// <summary>
/// Event arguments for score updates
/// </summary>
public class ScoreUpdateEventArgs
{
    public string Action { get; set; } = string.Empty;
    public string? BlockId { get; set; }
    public int TotalBlocks { get; set; }
}

/// <summary>
/// Event arguments for game state changes
/// </summary>
public class GameStateChangeEventArgs
{
    public string State { get; set; } = string.Empty;
    public JsonElement? Data { get; set; }
}