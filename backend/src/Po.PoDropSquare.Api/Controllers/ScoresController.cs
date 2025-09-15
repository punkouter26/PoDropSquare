using Microsoft.AspNetCore.Mvc;
using Po.PoDropSquare.Core.Contracts;
using Po.PoDropSquare.Services.Services;
using System.ComponentModel.DataAnnotations;

namespace Po.PoDropSquare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScoresController : ControllerBase
{
    private readonly IScoreService _scoreService;
    private readonly ILeaderboardService _leaderboardService;
    private readonly ILogger<ScoresController> _logger;

    public ScoresController(
        IScoreService scoreService,
        ILeaderboardService leaderboardService,
        ILogger<ScoresController> logger)
    {
        _scoreService = scoreService;
        _leaderboardService = leaderboardService;
        _logger = logger;
    }

    /// <summary>
    /// Submits a new score to the leaderboard with comprehensive validation and anti-cheat measures.
    /// </summary>
    /// <param name="request">Score submission request containing player data and score details</param>
    /// <returns>Score submission result with validation status and rank information</returns>
    /// <response code="201">Score successfully submitted and validated</response>
    /// <response code="400">Invalid request data or failed validation</response>
    /// <response code="409">Duplicate or suspicious score submission</response>
    /// <response code="429">Rate limit exceeded</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(ScoreSubmissionResult), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [ProducesResponseType(typeof(ErrorResponse), 429)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> SubmitScore([FromBody, Required] ScoreSubmissionRequest request)
    {
        if (request == null)
        {
            _logger.LogWarning("Score submission attempted with null request body from IP {RemoteIpAddress}",
                HttpContext.Connection.RemoteIpAddress);

            return BadRequest(new ErrorResponse
            {
                Error = "InvalidRequest",
                Message = "Request body cannot be null",
                Details = "A valid score submission request is required"
            });
        }

        // Log the submission attempt with context
        _logger.LogInformation(
            "Processing score submission for player {PlayerInitials} with survival time {SurvivalTime}s from IP {RemoteIpAddress}",
            request.PlayerInitials,
            request.SurvivalTime,
            HttpContext.Connection.RemoteIpAddress);

        try
        {
            // Validate the request first
            var validationResult = await _scoreService.ValidateScoreSubmissionAsync(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Score submission validation failed for player {PlayerInitials}: {ValidationError}",
                    request.PlayerInitials,
                    validationResult.ErrorMessage);

                return BadRequest(new ErrorResponse
                {
                    Error = "ValidationFailed",
                    Message = "Score submission failed validation",
                    Details = validationResult.ErrorMessage
                });
            }

            // Submit the score
            var result = await _scoreService.SubmitScoreAsync(request);

            if (result == null)
            {
                _logger.LogWarning(
                    "Score submission returned null result for player {PlayerInitials} - possible duplicate or suspicious activity",
                    request.PlayerInitials);

                return Conflict(new ErrorResponse
                {
                    Error = "SubmissionConflict",
                    Message = "Score submission could not be processed",
                    Details = "This may be due to a duplicate submission or suspicious activity"
                });
            }

            // Handle different result types from service
            if (result is ScoreSubmissionErrorResponse errorResponse)
            {
                _logger.LogWarning(
                    "Score submission failed for player {PlayerInitials}: {Error}",
                    request.PlayerInitials,
                    errorResponse.Error);

                return BadRequest(new ErrorResponse
                {
                    Error = errorResponse.Error,
                    Message = errorResponse.Message,
                    Details = errorResponse.Details?.ToString()
                });
            }

            if (result is ScoreSubmissionResponse successResponse)
            {
                // Log successful submission
                _logger.LogInformation(
                    "Score submitted successfully for player {PlayerInitials}: {Score} points, ranked #{Rank}",
                    request.PlayerInitials,
                    request.SurvivalTime,
                    successResponse.LeaderboardPosition);

                // Return success with detailed response
                return CreatedAtAction(
                    nameof(GetPlayerRank),
                    new { playerInitials = request.PlayerInitials },
                    new ScoreSubmissionResult
                    {
                        Success = true,
                        Message = successResponse.Message,
                        ScoreDetails = new ScoreDetails
                        {
                            Id = successResponse.ScoreId,
                            PlayerInitials = request.PlayerInitials,
                            Score = request.SurvivalTime,
                            SubmittedAt = DateTime.UtcNow.ToString("O"),
                            Rank = successResponse.LeaderboardPosition,
                            IsNewPersonalBest = successResponse.Metadata?.IsPersonalBest ?? false,
                            IsLeaderboardQualified = successResponse.QualifiedForLeaderboard
                        },
                        ValidationInfo = new ValidationInfo
                        {
                            SessionValidated = true,
                            TimingValidated = true,
                            DuplicateCheck = true
                        }
                    });
            }

            // Fallback for unexpected result type
            _logger.LogError("Unexpected result type from score service: {ResultType}", result.GetType());
            return StatusCode(500, new ErrorResponse
            {
                Error = "UnexpectedResult",
                Message = "Unexpected response from score service",
                Details = "Please try again or contact support"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex,
                "Invalid score submission data for player {PlayerInitials}: {Error}",
                request.PlayerInitials,
                ex.Message);

            return BadRequest(new ErrorResponse
            {
                Error = "InvalidData",
                Message = "Invalid submission data",
                Details = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex,
                "Score submission operation failed for player {PlayerInitials}: {Error}",
                request.PlayerInitials,
                ex.Message);

            return Conflict(new ErrorResponse
            {
                Error = "OperationFailed",
                Message = "Score submission operation failed",
                Details = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error during score submission for player {PlayerInitials}",
                request.PlayerInitials);

            return StatusCode(500, new ErrorResponse
            {
                Error = "InternalServerError",
                Message = "An unexpected error occurred while processing your score",
                Details = "Please try again later or contact support if the problem persists"
            });
        }
    }

    /// <summary>
    /// Retrieves the top leaderboard entries with caching and performance optimization.
    /// </summary>
    /// <param name="count">Number of entries to retrieve (default 10, max 50)</param>
    /// <param name="playerInitials">Optional player initials to include player rank information</param>
    /// <returns>Leaderboard response with top entries and caching information</returns>
    /// <response code="200">Successfully retrieved leaderboard</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("top10")]
    [HttpGet("leaderboard")] // Keep backward compatibility
    [ProducesResponseType(typeof(LeaderboardResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "count", "playerInitials" })]
    public async Task<IActionResult> GetTopLeaderboard(
        [FromQuery] int? count = null,
        [FromQuery] string? playerInitials = null)
    {
        var requestedCount = count ?? 10;

        // Validate request parameters
        if (requestedCount < 1 || requestedCount > 50)
        {
            _logger.LogWarning("Invalid leaderboard count requested: {Count}", requestedCount);
            return BadRequest(new ErrorResponse
            {
                Error = "InvalidParameter",
                Message = "Invalid count parameter",
                Details = "Count must be between 1 and 50"
            });
        }

        if (!string.IsNullOrEmpty(playerInitials) && !IsValidPlayerInitials(playerInitials))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "InvalidParameter",
                Message = "Invalid player initials format",
                Details = "Player initials must be 1-3 uppercase alphanumeric characters"
            });
        }

        _logger.LogInformation(
            "Retrieving top {Count} leaderboard entries{PlayerContext}",
            requestedCount,
            !string.IsNullOrEmpty(playerInitials) ? $" with player rank for {playerInitials}" : "");

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Get leaderboard data
            var leaderboard = await _leaderboardService.GetTopLeaderboardAsync(requestedCount);

            object response;

            if (!string.IsNullOrEmpty(playerInitials))
            {
                // Include player rank in response if specific player requested
                var playerRank = await _leaderboardService.GetPlayerLeaderboardPositionAsync(playerInitials);

                response = new
                {
                    Success = true,
                    Leaderboard = leaderboard.Leaderboard,
                    PlayerRank = playerRank,
                    LastUpdated = leaderboard.LastUpdated,
                    TotalEntries = leaderboard.TotalEntries,
                    RequestInfo = new
                    {
                        RequestedCount = requestedCount,
                        ActualCount = leaderboard.Leaderboard?.Count ?? 0,
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                        CachedResponse = false // Will be true if served from cache
                    }
                };

                if (playerRank != null)
                {
                    _logger.LogInformation(
                        "Player {PlayerInitials} is at rank {Rank}",
                        playerInitials,
                        playerRank.Rank);
                }
                else
                {
                    _logger.LogInformation(
                        "Player {PlayerInitials} not found on leaderboard",
                        playerInitials);
                }
            }
            else
            {
                response = new
                {
                    Success = true,
                    Leaderboard = leaderboard.Leaderboard,
                    LastUpdated = leaderboard.LastUpdated,
                    TotalEntries = leaderboard.TotalEntries,
                    RequestInfo = new
                    {
                        RequestedCount = requestedCount,
                        ActualCount = leaderboard.Leaderboard?.Count ?? 0,
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                        CachedResponse = false
                    }
                };
            }

            stopwatch.Stop();

            // Set cache headers for better performance
            Response.Headers["X-Cache-Duration"] = "30";
            Response.Headers["X-Processing-Time"] = stopwatch.ElapsedMilliseconds.ToString();

            // Add ETag for cache validation
            var eTag = GenerateETag(leaderboard.LastUpdated, requestedCount, playerInitials);
            Response.Headers["ETag"] = eTag;

            // Check if client has cached version
            if (Request.Headers.ContainsKey("If-None-Match") &&
                Request.Headers["If-None-Match"].ToString().Contains(eTag))
            {
                return StatusCode(304); // Not Modified
            }

            _logger.LogInformation(
                "Leaderboard retrieved successfully: {Count} entries in {ElapsedMs}ms",
                leaderboard.Leaderboard?.Count ?? 0,
                stopwatch.ElapsedMilliseconds);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving leaderboard with count {Count}{PlayerContext}",
                requestedCount,
                !string.IsNullOrEmpty(playerInitials) ? $" for player {playerInitials}" : "");

            return StatusCode(500, new ErrorResponse
            {
                Error = "InternalServerError",
                Message = "Failed to retrieve leaderboard data",
                Details = "Please try again later or contact support if the problem persists"
            });
        }
    }

    [HttpGet("player/{playerInitials}/rank")]
    public async Task<IActionResult> GetPlayerRank(string playerInitials)
    {
        if (string.IsNullOrWhiteSpace(playerInitials))
        {
            return BadRequest("Player initials are required");
        }

        try
        {
            var playerRank = await _leaderboardService.GetPlayerLeaderboardPositionAsync(playerInitials);

            if (playerRank == null)
            {
                return NotFound("Player not found");
            }

            return Ok(playerRank);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving player rank");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Validates player initials format
    /// </summary>
    /// <param name="playerInitials">Player initials to validate</param>
    /// <returns>True if valid format</returns>
    private static bool IsValidPlayerInitials(string playerInitials)
    {
        if (string.IsNullOrEmpty(playerInitials))
            return false;

        if (playerInitials.Length < 1 || playerInitials.Length > 3)
            return false;

        return playerInitials.All(char.IsLetterOrDigit) && playerInitials.All(char.IsUpper);
    }

    /// <summary>
    /// Generates ETag for cache validation
    /// </summary>
    /// <param name="lastUpdated">Last updated timestamp</param>
    /// <param name="count">Requested count</param>
    /// <param name="playerInitials">Optional player initials</param>
    /// <returns>ETag string</returns>
    private static string GenerateETag(string lastUpdated, int count, string? playerInitials)
    {
        var content = $"{lastUpdated}-{count}-{playerInitials ?? ""}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        return $"\"{Convert.ToHexString(hash)[..16]}\"";
    }
}
