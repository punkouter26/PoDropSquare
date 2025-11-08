using Po.PoDropSquare.Core.Entities;
using Po.PoDropSquare.Core.Contracts;

namespace Po.PoDropSquare.Core.Tests.Entities;

/// <summary>
/// Unit tests for ScoreEntry entity validation and business logic
/// </summary>
public class ScoreEntryTests
{
    #region Factory Method Tests

    [Fact]
    public void Create_WithValidData_ReturnsValidScoreEntry()
    {
        // Arrange
        var playerInitials = "ABC";
        var survivalTime = 15.5;
        var sessionSignature = "a".PadRight(64, 'b'); // Valid 64-char hex string
        var timestamp = DateTime.UtcNow;

        // Act
        var scoreEntry = ScoreEntry.Create(playerInitials, survivalTime, sessionSignature, timestamp);

        // Assert
        Assert.NotNull(scoreEntry);
        Assert.Equal(playerInitials, scoreEntry.PlayerInitials);
        Assert.Equal(survivalTime, scoreEntry.SurvivalTime);
        Assert.Equal(sessionSignature, scoreEntry.SessionSignature);
        Assert.NotEqual(Guid.Empty.ToString(), scoreEntry.ScoreId);
    }

    [Fact]
    public void Create_AutoGeneratesScoreId()
    {
        // Arrange & Act
        var scoreEntry1 = ScoreEntry.Create("ABC", 10.0, "a".PadRight(64, 'b'), DateTime.UtcNow);
        var scoreEntry2 = ScoreEntry.Create("XYZ", 12.0, "c".PadRight(64, 'd'), DateTime.UtcNow);

        // Assert
        Assert.NotEqual(scoreEntry1.ScoreId, scoreEntry2.ScoreId);
        Assert.NotEqual(Guid.Empty.ToString(), scoreEntry1.ScoreId);
        Assert.NotEqual(Guid.Empty.ToString(), scoreEntry2.ScoreId);
    }

    #endregion

    #region Player Initials Validation Tests

    [Fact]
    public void Validate_WithValidPlayerInitials_ReturnsValid()
    {
        // Arrange
        var scoreEntry = ScoreEntry.Create("ABC", 10.0, "a".PadRight(64, 'b'), DateTime.UtcNow);

        // Act
        var result = scoreEntry.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    public void Validate_WithEmptyPlayerInitials_ReturnsInvalid(string invalidInitials)
    {
        // Arrange
        var scoreEntry = ScoreEntry.Create("ABC", 10.0, "a".PadRight(64, 'b'), DateTime.UtcNow);
        typeof(ScoreEntry).GetProperty("PlayerInitials")!.SetValue(scoreEntry, invalidInitials);

        // Act
        var result = scoreEntry.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("are required", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABCD")]
    [InlineData("ABCDE")]
    public void Validate_WithInvalidLengthPlayerInitials_ReturnsInvalid(string invalidInitials)
    {
        // Arrange
        if (string.IsNullOrEmpty(invalidInitials))
        {
            // Tested separately
            return;
        }

        var scoreEntry = ScoreEntry.Create("ABC", 10.0, "a".PadRight(64, 'b'), DateTime.UtcNow);
        typeof(ScoreEntry).GetProperty("PlayerInitials")!.SetValue(scoreEntry, invalidInitials);

        // Act
        var result = scoreEntry.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("1-3 characters", result.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("aBc")]
    [InlineData("Ab")]
    public void Validate_WithLowercasePlayerInitials_ReturnsInvalid(string invalidInitials)
    {
        // Arrange
        var scoreEntry = ScoreEntry.Create("ABC", 10.0, "a".PadRight(64, 'b'), DateTime.UtcNow);
        typeof(ScoreEntry).GetProperty("PlayerInitials")!.SetValue(scoreEntry, invalidInitials);

        // Act
        var result = scoreEntry.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("uppercase", result.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("A@B")]
    [InlineData("A-C")]
    [InlineData("A C")]
    public void Validate_WithSpecialCharactersInPlayerInitials_ReturnsInvalid(string invalidInitials)
    {
        // Arrange
        var scoreEntry = ScoreEntry.Create("ABC", 10.0, "a".PadRight(64, 'b'), DateTime.UtcNow);
        typeof(ScoreEntry).GetProperty("PlayerInitials")!.SetValue(scoreEntry, invalidInitials);

        // Act
        var result = scoreEntry.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("alphanumeric", result.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("AB")]
    [InlineData("ABC")]
    [InlineData("A1")]
    [InlineData("AB2")]
    [InlineData("123")]
    public void Validate_WithValidPlayerInitialsFormats_ReturnsValid(string validInitials)
    {
        // Arrange
        var scoreEntry = ScoreEntry.Create(validInitials, 10.0, "a".PadRight(64, 'b'), DateTime.UtcNow);

        // Act
        var result = scoreEntry.Validate();

        // Assert
        Assert.True(result.IsValid, $"Expected {validInitials} to be valid, but got: {result.ErrorMessage}");
    }

    #endregion

    #region Survival Time Validation Tests

    [Theory]
    [InlineData(0.05)]  // Minimum valid (50ms)
    [InlineData(1.0)]
    [InlineData(10.5)]
    [InlineData(15.75)]
    [InlineData(19.99)]
    [InlineData(20.0)]  // Maximum valid
    public void Validate_WithValidSurvivalTime_ReturnsValid(double validTime)
    {
        // Arrange
        var scoreEntry = ScoreEntry.Create("ABC", validTime, "a".PadRight(64, 'b'), DateTime.UtcNow);

        // Act
        var result = scoreEntry.Validate();

        // Assert
        Assert.True(result.IsValid, $"Expected {validTime}s to be valid, but got: {result.ErrorMessage}");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(-10.5)]
    public void Validate_WithNegativeOrZeroSurvivalTime_ReturnsInvalid(double invalidTime)
    {
        // Arrange
        var scoreEntry = ScoreEntry.Create("ABC", 10.0, "a".PadRight(64, 'b'), DateTime.UtcNow);
        typeof(ScoreEntry).GetProperty("SurvivalTime")!.SetValue(scoreEntry, invalidTime);

        // Act
        var result = scoreEntry.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("must be greater than 0", result.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(20.01)]
    [InlineData(25.0)]
    [InlineData(100.0)]
    public void Validate_WithExcessiveSurvivalTime_ReturnsInvalid(double invalidTime)
    {
        // Arrange
        var scoreEntry = ScoreEntry.Create("ABC", 10.0, "a".PadRight(64, 'b'), DateTime.UtcNow);
        typeof(ScoreEntry).GetProperty("SurvivalTime")!.SetValue(scoreEntry, invalidTime);

        // Act
        var result = scoreEntry.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("cannot exceed 20 seconds", result.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(10.123)]  // 3 decimal places
    [InlineData(15.12345)]  // 5 decimal places
    public void Validate_WithTooManyDecimalPlaces_ReturnsInvalid(double invalidTime)
    {
        // Arrange
        var scoreEntry = ScoreEntry.Create("ABC", 10.0, "a".PadRight(64, 'b'), DateTime.UtcNow);
        typeof(ScoreEntry).GetProperty("SurvivalTime")!.SetValue(scoreEntry, invalidTime);

        // Act
        var result = scoreEntry.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("precision too high", result.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(10.0)]
    [InlineData(10.1)]
    [InlineData(10.12)]
    public void Validate_WithValidDecimalPlaces_ReturnsValid(double validTime)
    {
        // Arrange
        var scoreEntry = ScoreEntry.Create("ABC", validTime, "a".PadRight(64, 'b'), DateTime.UtcNow);

        // Act
        var result = scoreEntry.Validate();

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Session Signature Validation Tests

    [Fact]
    public void Validate_WithValidSessionSignature_ReturnsValid()
    {
        // Arrange - 64 character hex string (SHA-256)
        var validSignature = "a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd";
        var scoreEntry = ScoreEntry.Create("ABC", 10.0, validSignature, DateTime.UtcNow);

        // Act
        var result = scoreEntry.Validate();

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    public void Validate_WithEmptySessionSignature_ReturnsInvalid(string invalidSignature)
    {
        // Arrange
        var scoreEntry = ScoreEntry.Create("ABC", 10.0, "a".PadRight(64, 'b'), DateTime.UtcNow);
        typeof(ScoreEntry).GetProperty("SessionSignature")!.SetValue(scoreEntry, invalidSignature);

        // Act
        var result = scoreEntry.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("signature", result.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region ScoreId Validation Tests

    [Fact]
    public void Validate_WithValidScoreId_ReturnsValid()
    {
        // Arrange
        var scoreEntry = ScoreEntry.Create("ABC", 10.0, "a".PadRight(64, 'b'), DateTime.UtcNow);

        // Act
        var result = scoreEntry.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.NotEqual(Guid.Empty.ToString(), scoreEntry.ScoreId);
    }

    [Theory]
    [InlineData("")]
    public void Validate_WithEmptyScoreId_ReturnsInvalid(string invalidScoreId)
    {
        // Arrange
        var scoreEntry = ScoreEntry.Create("ABC", 10.0, "a".PadRight(64, 'b'), DateTime.UtcNow);
        typeof(ScoreEntry).GetProperty("ScoreId")!.SetValue(scoreEntry, invalidScoreId);

        // Act
        var result = scoreEntry.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Score ID is required", result.ErrorMessage ?? "");
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public void Validate_WithAllInvalidFields_ReturnsFirstError()
    {
        // Arrange
        var scoreEntry = ScoreEntry.Create("ABC", 10.0, "a".PadRight(64, 'b'), DateTime.UtcNow);
        typeof(ScoreEntry).GetProperty("PlayerInitials")!.SetValue(scoreEntry, "");
        typeof(ScoreEntry).GetProperty("SurvivalTime")!.SetValue(scoreEntry, -1.0);
        typeof(ScoreEntry).GetProperty("SessionSignature")!.SetValue(scoreEntry, "");

        // Act
        var result = scoreEntry.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        // Should return first validation error encountered
    }

    [Fact]
    public void Validate_WithBoundaryMinimumValues_ReturnsValid()
    {
        // Arrange - Minimum valid values
        var scoreEntry = ScoreEntry.Create("A", 0.05, "a".PadRight(64, 'b'), DateTime.UtcNow);

        // Act
        var result = scoreEntry.Validate();

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithBoundaryMaximumValues_ReturnsValid()
    {
        // Arrange - Maximum valid values
        var scoreEntry = ScoreEntry.Create("ABC", 20.0, "f".PadRight(64, 'f'), DateTime.UtcNow);

        // Act
        var result = scoreEntry.Validate();

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion
}
