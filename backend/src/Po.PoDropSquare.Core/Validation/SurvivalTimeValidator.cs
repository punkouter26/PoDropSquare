using Po.PoDropSquare.Core.Contracts;

namespace Po.PoDropSquare.Core.Validation;

/// <summary>
/// Shared validator for survival time following DRY principle
/// </summary>
public static class SurvivalTimeValidator
{
    private const double MinSurvivalTime = 0.05;   // 50ms minimum (human reaction time)
    private const double MaxSurvivalTime = 20.0;   // 20 seconds max
    private const int DecimalPlaces = 2;

    /// <summary>
    /// Validates survival time value
    /// </summary>
    /// <param name="survivalTime">The survival time in seconds</param>
    /// <returns>Validation result</returns>
    public static ValidationResult Validate(double survivalTime)
    {
        if (survivalTime <= 0)
            return ValidationResult.Invalid("Survival time must be positive");

        if (survivalTime < MinSurvivalTime)
            return ValidationResult.Invalid($"Survival time too low (minimum: {MinSurvivalTime}s)");

        if (survivalTime > MaxSurvivalTime)
            return ValidationResult.Invalid($"Survival time exceeds maximum ({MaxSurvivalTime}s)");

        // Validate decimal precision (prevent 15.123456789)
        var rounded = Math.Round(survivalTime, DecimalPlaces);
        if (Math.Abs(survivalTime - rounded) > 0.0001)
            return ValidationResult.Invalid($"Survival time must have maximum {DecimalPlaces} decimal places");

        return ValidationResult.Valid();
    }

    /// <summary>
    /// Quick validation check returning boolean
    /// </summary>
    /// <param name="survivalTime">The survival time in seconds</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(double survivalTime)
        => Validate(survivalTime).IsValid;

    /// <summary>
    /// Gets the minimum allowed survival time
    /// </summary>
    public static double MinimumValue => MinSurvivalTime;

    /// <summary>
    /// Gets the maximum allowed survival time
    /// </summary>
    public static double MaximumValue => MaxSurvivalTime;
}
