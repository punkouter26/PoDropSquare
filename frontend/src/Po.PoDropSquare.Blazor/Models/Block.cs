using System.Numerics;
using System.Text.Json.Serialization;

namespace Po.PoDropSquare.Blazor.Models;

/// <summary>
/// Represents a physics-enabled block in the game world.
/// Each block has position, rotation, physics properties, and visual appearance.
/// </summary>
public class Block
{
    /// <summary>
    /// Unique identifier for this block
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Type of block (affects physics properties and appearance)
    /// </summary>
    public BlockType Type { get; set; } = BlockType.Square;

    /// <summary>
    /// Current position in world coordinates
    /// </summary>
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>
    /// Current rotation in radians
    /// </summary>
    public double Rotation { get; set; } = 0.0;

    /// <summary>
    /// Current velocity vector (pixels per second)
    /// </summary>
    public Vector2 Velocity { get; set; } = Vector2.Zero;

    /// <summary>
    /// Angular velocity in radians per second
    /// </summary>
    public double AngularVelocity { get; set; } = 0.0;

    /// <summary>
    /// Whether this block is currently sleeping (physics optimization)
    /// </summary>
    public bool IsSleeping { get; set; } = false;

    /// <summary>
    /// Color of the block (CSS color string or hex)
    /// </summary>
    public string Color { get; set; } = "#4834d4";

    /// <summary>
    /// When this block was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this block is currently selected/highlighted
    /// </summary>
    [JsonIgnore]
    public bool IsSelected { get; set; } = false;

    /// <summary>
    /// Current game score contribution from this block
    /// </summary>
    public int ScoreValue { get; set; } = 0;
}

/// <summary>
/// Types of blocks available in the game
/// </summary>
public enum BlockType
{
    Square,
    Rectangle,
    Circle,
    Triangle,
    Hexagon,
    Diamond
}