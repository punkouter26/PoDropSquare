using Po.PoDropSquare.Core.Contracts;

namespace Po.PoDropSquare.Core.Validation;

/// <summary>
/// Shared validator for client timestamps following DRY principle
/// </summary>
public static class TimestampValidator
{
    private static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Validates a client timestamp string
    /// </summary>
    /// <param name="clientTimestamp">The client timestamp in ISO 8601 format</param>
    /// <returns>Validation result</returns>
    public static ValidationResult ValidateClientTimestamp(string? clientTimestamp)
    {
        if (string.IsNullOrEmpty(clientTimestamp))
            return ValidationResult.Invalid("Timestamp is required");

        if (!DateTime.TryParse(clientTimestamp, out var clientTime))
            return ValidationResult.Invalid("Invalid timestamp format");

        var timeDifference = DateTime.UtcNow - clientTime.ToUniversalTime();
        if (Math.Abs(timeDifference.TotalMinutes) > MaxClockSkew.TotalMinutes)
            return ValidationResult.Invalid($"Timestamp differs from server time by more than {MaxClockSkew.TotalMinutes} minutes");

        return ValidationResult.Valid();
    }

    /// <summary>
    /// Quick validation check returning boolean
    /// </summary>
    /// <param name="clientTimestamp">The client timestamp to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(string? clientTimestamp)
        => ValidateClientTimestamp(clientTimestamp).IsValid;

    /// <summary>
    /// Gets the maximum allowed clock skew
    /// </summary>
    public static TimeSpan MaximumClockSkew => MaxClockSkew;
}
