using Microsoft.Extensions.Logging;
using Po.PoDropSquare.Core.Contracts;
using Po.PoDropSquare.Core.Entities;
using Po.PoDropSquare.Data.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace Po.PoDropSquare.Services.Services;

/// <summary>
/// Service implementation for score submission and validation
/// </summary>
public class ScoreService : IScoreService
{
    private readonly IScoreRepository _scoreRepository;
    private readonly ILeaderboardRepository _leaderboardRepository;
    private readonly ILogger<ScoreService> _logger;

    // Anti-cheat configuration
    private const double MaxHumanReactionTime = 0.05; // 50ms minimum survival time
    private const double MaxReasonableSurvivalTime = 20.0; // 20 seconds max
    private const int MaxSubmissionsPerMinute = 5;
    private const int MaxSubmissionsPerHour = 50;

    public ScoreService(
        IScoreRepository scoreRepository,
        ILeaderboardRepository leaderboardRepository,
        ILogger<ScoreService> logger)
    {
        _scoreRepository = scoreRepository;
        _leaderboardRepository = leaderboardRepository;
        _logger = logger;
    }

    public async Task<object> SubmitScoreAsync(ScoreSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing score submission for player {PlayerInitials}: {SurvivalTime}s",
                request.PlayerInitials, request.SurvivalTime);

            // Validate the request
            var validation = await ValidateScoreSubmissionAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Score submission validation failed for player {PlayerInitials}: {Error}",
                    request.PlayerInitials, validation.ErrorMessage);

                return new ScoreSubmissionErrorResponse
                {
                    Error = "VALIDATION_FAILED",
                    Message = validation.ErrorMessage ?? "Invalid score submission"
                };
            }

            // Get previous best score for metadata
            var previousBest = await _scoreRepository.GetPlayerBestScoreAsync(request.PlayerInitials, cancellationToken);
            var submissionCount = await _scoreRepository.GetPlayerSubmissionCountAsync(request.PlayerInitials, cancellationToken);

            // Create score entry
            var scoreEntry = ScoreEntry.Create(
                request.PlayerInitials,
                request.SurvivalTime,
                request.SessionSignature,
                DateTime.Parse(request.ClientTimestamp)
            );

            // Submit to repository
            var submittedScore = await _scoreRepository.SubmitScoreAsync(scoreEntry, cancellationToken);

            // Update leaderboard
            var leaderboardEntry = await _leaderboardRepository.UpdateLeaderboardAsync(submittedScore, cancellationToken);

            // Prepare response
            var isPersonalBest = previousBest == null || request.SurvivalTime > previousBest.SurvivalTime;
            var qualifiedForLeaderboard = leaderboardEntry != null;
            var leaderboardPosition = leaderboardEntry?.Rank ?? 0;

            stopwatch.Stop();

            var response = new ScoreSubmissionResponse
            {
                Success = true,
                ScoreId = submittedScore.ScoreId,
                LeaderboardPosition = leaderboardPosition,
                QualifiedForLeaderboard = qualifiedForLeaderboard,
                Message = GenerateSuccessMessage(isPersonalBest, qualifiedForLeaderboard, leaderboardPosition),
                Metadata = new ScoreSubmissionMetadata
                {
                    IsPersonalBest = isPersonalBest,
                    PreviousBest = previousBest?.SurvivalTime,
                    Improvement = isPersonalBest && previousBest != null ? request.SurvivalTime - previousBest.SurvivalTime : null,
                    TotalSubmissions = submissionCount + 1,
                    ProcessingTime = (int)stopwatch.ElapsedMilliseconds
                }
            };

            _logger.LogInformation("Score submission successful for player {PlayerInitials}: {ScoreId}, qualified: {Qualified}, rank: {Rank}",
                request.PlayerInitials, submittedScore.ScoreId, qualifiedForLeaderboard, leaderboardPosition);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to submit score for player {PlayerInitials}", request.PlayerInitials);

            return new ScoreSubmissionErrorResponse
            {
                Error = "SUBMISSION_FAILED",
                Message = "Failed to submit score. Please try again.",
                Details = new { ProcessingTime = stopwatch.ElapsedMilliseconds }
            };
        }
    }

    public async Task<ValidationResult> ValidateScoreSubmissionAsync(ScoreSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Basic contract validation
            var contractValidation = request.Validate();
            if (!contractValidation.IsValid)
            {
                return contractValidation;
            }

            // Anti-cheat validations

            // 1. Minimum reaction time check
            if (request.SurvivalTime < MaxHumanReactionTime)
            {
                _logger.LogWarning("Suspicious submission: survival time {SurvivalTime}s below human reaction threshold",
                    request.SurvivalTime);
                return ValidationResult.Invalid("Survival time too low to be realistic");
            }

            // 2. Maximum reasonable time check
            if (request.SurvivalTime > MaxReasonableSurvivalTime)
            {
                _logger.LogWarning("Suspicious submission: survival time {SurvivalTime}s exceeds maximum expected",
                    request.SurvivalTime);
                return ValidationResult.Invalid("Survival time exceeds maximum expected value");
            }

            // 3. Rate limiting checks
            var rateLimitValidation = await ValidateRateLimitsAsync(request.PlayerInitials, cancellationToken);
            if (!rateLimitValidation.IsValid)
            {
                return rateLimitValidation;
            }

            // 4. Session signature validation (basic check - in production, this would be more sophisticated)
            var signatureValidation = ValidateSessionSignature(request);
            if (!signatureValidation.IsValid)
            {
                return signatureValidation;
            }

            // 5. Timestamp validation (already done in contract validation, but double-check)
            if (!DateTime.TryParse(request.ClientTimestamp, out var clientTime))
            {
                return ValidationResult.Invalid("Invalid timestamp format");
            }

            var timeDifference = (DateTime.UtcNow - clientTime.ToUniversalTime()).TotalMinutes;
            if (Math.Abs(timeDifference) > 10) // Allow 10 minutes clock skew
            {
                _logger.LogWarning("Suspicious submission: timestamp {ClientTimestamp} differs from server time by {TimeDifference} minutes",
                    request.ClientTimestamp, timeDifference);
                return ValidationResult.Invalid("Timestamp too far from server time");
            }

            return ValidationResult.Valid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during score validation for player {PlayerInitials}", request.PlayerInitials);
            return ValidationResult.Invalid("Validation failed due to server error");
        }
    }

    public async Task<List<ScoreEntry>> GetPlayerScoresAsync(string playerInitials, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _scoreRepository.GetPlayerScoresAsync(playerInitials, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get scores for player {PlayerInitials}", playerInitials);
            throw;
        }
    }

    public async Task<ScoreEntry?> GetPlayerBestScoreAsync(string playerInitials, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _scoreRepository.GetPlayerBestScoreAsync(playerInitials, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get best score for player {PlayerInitials}", playerInitials);
            throw;
        }
    }

    public async Task<ScoreEntry?> GetScoreByIdAsync(string scoreId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _scoreRepository.GetScoreByIdAsync(scoreId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get score by ID {ScoreId}", scoreId);
            throw;
        }
    }

    public async Task<int> CleanupOldScoresAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting cleanup of scores older than {RetentionDays} days", retentionDays);
            var deletedCount = await _scoreRepository.CleanupOldScoresAsync(retentionDays, cancellationToken);
            _logger.LogInformation("Cleanup completed: {DeletedCount} scores deleted", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old scores");
            throw;
        }
    }

    /// <summary>
    /// Validates rate limiting for a player
    /// </summary>
    private async Task<ValidationResult> ValidateRateLimitsAsync(string playerInitials, CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTime.UtcNow;

            // Check submissions in the last minute
            var recentSubmissions = await _scoreRepository.GetScoresByTimeRangeAsync(
                now.AddMinutes(-1), now, cancellationToken);

            var playerRecentSubmissions = recentSubmissions.Count(s => s.PlayerInitials == playerInitials);
            if (playerRecentSubmissions >= MaxSubmissionsPerMinute)
            {
                _logger.LogWarning("Rate limit exceeded for player {PlayerInitials}: {Submissions} in last minute",
                    playerInitials, playerRecentSubmissions);
                return ValidationResult.Invalid("Too many submissions in the last minute. Please wait before submitting again.");
            }

            // Check submissions in the last hour
            var hourlySubmissions = await _scoreRepository.GetScoresByTimeRangeAsync(
                now.AddHours(-1), now, cancellationToken);

            var playerHourlySubmissions = hourlySubmissions.Count(s => s.PlayerInitials == playerInitials);
            if (playerHourlySubmissions >= MaxSubmissionsPerHour)
            {
                _logger.LogWarning("Hourly rate limit exceeded for player {PlayerInitials}: {Submissions} in last hour",
                    playerInitials, playerHourlySubmissions);
                return ValidationResult.Invalid("Too many submissions in the last hour. Please wait before submitting again.");
            }

            return ValidationResult.Valid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating rate limits for player {PlayerInitials}", playerInitials);
            return ValidationResult.Invalid("Rate limit validation failed");
        }
    }

    /// <summary>
    /// Validates the session signature for anti-cheat purposes
    /// </summary>
    private ValidationResult ValidateSessionSignature(ScoreSubmissionRequest request)
    {
        try
        {
            // Basic signature format validation
            if (string.IsNullOrEmpty(request.SessionSignature))
            {
                return ValidationResult.Invalid("Session signature is required");
            }

            if (request.SessionSignature.Length != 64) // SHA-256 hex string length
            {
                _logger.LogWarning("Invalid signature length for player {PlayerInitials}: {Length}",
                    request.PlayerInitials, request.SessionSignature.Length);
                return ValidationResult.Invalid("Invalid session signature format");
            }

            // Verify it's a valid hex string
            if (!IsValidHexString(request.SessionSignature))
            {
                _logger.LogWarning("Invalid signature format for player {PlayerInitials}: not hex",
                    request.PlayerInitials);
                return ValidationResult.Invalid("Invalid session signature format");
            }

            // In a production system, you would:
            // 1. Verify the signature was generated by your client-side code
            // 2. Check if the signature includes game session data
            // 3. Validate timing information embedded in the signature
            // 4. Check for replay attacks using nonce/timestamps

            // For now, we just ensure it's properly formatted
            return ValidationResult.Valid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session signature for player {PlayerInitials}", request.PlayerInitials);
            return ValidationResult.Invalid("Session signature validation failed");
        }
    }

    /// <summary>
    /// Checks if a string is a valid hexadecimal string
    /// </summary>
    private static bool IsValidHexString(string hex)
    {
        return hex.All(c => char.IsDigit(c) || (char.ToLower(c) >= 'a' && char.ToLower(c) <= 'f'));
    }

    /// <summary>
    /// Generates a success message based on submission results
    /// </summary>
    private static string GenerateSuccessMessage(bool isPersonalBest, bool qualifiedForLeaderboard, int leaderboardPosition)
    {
        if (qualifiedForLeaderboard)
        {
            if (leaderboardPosition == 1)
            {
                return "🎉 New high score! You're #1 on the leaderboard!";
            }
            else if (isPersonalBest)
            {
                return $"🎉 Personal best! You're #{leaderboardPosition} on the leaderboard!";
            }
            else
            {
                return $"Great job! You're #{leaderboardPosition} on the leaderboard!";
            }
        }
        else if (isPersonalBest)
        {
            return "🎉 Personal best! Keep improving to reach the leaderboard!";
        }
        else
        {
            return "Score submitted! Keep practicing to improve your best time!";
        }
    }
}