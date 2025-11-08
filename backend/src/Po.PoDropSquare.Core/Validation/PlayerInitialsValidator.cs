using Po.PoDropSquare.Core.Contracts;

namespace Po.PoDropSquare.Core.Validation;

/// <summary>
/// Shared validator for player initials following DRY principle
/// </summary>
public static class PlayerInitialsValidator
{
    private const int MinLength = 1;
    private const int MaxLength = 3;

    /// <summary>
    /// Validates player initials format
    /// </summary>
    /// <param name="playerInitials">The player initials to validate</param>
    /// <returns>Validation result</returns>
    public static ValidationResult Validate(string? playerInitials)
    {
        if (string.IsNullOrEmpty(playerInitials))
            return ValidationResult.Invalid("Player initials cannot be empty");

        if (playerInitials.Length < MinLength || playerInitials.Length > MaxLength)
            return ValidationResult.Invalid($"Player initials must be {MinLength}-{MaxLength} characters");

        if (!playerInitials.All(char.IsLetterOrDigit))
            return ValidationResult.Invalid("Player initials must contain only alphanumeric characters");

        if (!playerInitials.All(char.IsUpper))
            return ValidationResult.Invalid("Player initials must be uppercase");

        return ValidationResult.Valid();
    }

    /// <summary>
    /// Quick validation check returning boolean
    /// </summary>
    /// <param name="playerInitials">The player initials to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(string? playerInitials)
        => Validate(playerInitials).IsValid;
}
