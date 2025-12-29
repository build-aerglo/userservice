using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.Points;
using UserService.Application.Interfaces;
using UserService.Domain.Exceptions;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PointsController(IPointsService pointsService, ILogger<PointsController> logger) : ControllerBase
{
    /// <summary>
    /// Get user's points and statistics
    /// </summary>
    [AllowAnonymous]
    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetUserPoints(Guid userId)
    {
        try
        {
            var result = await pointsService.GetUserPointsAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting points for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get user's point transaction history
    /// </summary>
    [AllowAnonymous]
    [HttpGet("user/{userId:guid}/history")]
    public async Task<IActionResult> GetPointsHistory(Guid userId, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
    {
        try
        {
            var result = await pointsService.GetPointsHistoryAsync(userId, limit, offset);
            return Ok(result);
        }
        catch (UserPointsNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting points history for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Award points to a user (Internal use - called by other services)
    /// </summary>
    [AllowAnonymous]
    [HttpPost("award")]
    public async Task<IActionResult> AwardPoints([FromBody] AwardPointsDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await pointsService.AwardPointsAsync(dto);
            return Created("", result);
        }
        catch (InvalidPointsAmountException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error awarding points to user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Calculate points for a review (without awarding)
    /// </summary>
    [AllowAnonymous]
    [HttpPost("calculate/review")]
    public async Task<IActionResult> CalculateReviewPoints([FromBody] CalculateReviewPointsDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await pointsService.CalculateReviewPointsAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating review points");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Award points for a review
    /// </summary>
    [AllowAnonymous]
    [HttpPost("award/review")]
    public async Task<IActionResult> AwardReviewPoints([FromBody] CalculateReviewPointsDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await pointsService.AwardReviewPointsAsync(dto);
            return Created("", result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error awarding review points to user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get global leaderboard
    /// </summary>
    [AllowAnonymous]
    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int limit = 10)
    {
        try
        {
            var result = await pointsService.GetLeaderboardAsync(limit);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting leaderboard");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get location-based leaderboard
    /// </summary>
    [AllowAnonymous]
    [HttpGet("leaderboard/{state}")]
    public async Task<IActionResult> GetLocationLeaderboard(string state, [FromQuery] int limit = 10)
    {
        try
        {
            var result = await pointsService.GetLocationLeaderboardAsync(state, limit);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting leaderboard for state {State}", state);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get user's tier
    /// </summary>
    [AllowAnonymous]
    [HttpGet("user/{userId:guid}/tier")]
    public async Task<IActionResult> GetUserTier(Guid userId)
    {
        try
        {
            var tier = await pointsService.GetUserTierAsync(userId);
            return Ok(new { userId, tier });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting tier for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update user's streak (Internal - called when user performs activity)
    /// </summary>
    [AllowAnonymous]
    [HttpPost("streak/{userId:guid}")]
    public async Task<IActionResult> UpdateStreak(Guid userId)
    {
        try
        {
            await pointsService.UpdateStreakAsync(userId, DateTime.UtcNow);
            return Ok(new { message = "Streak updated" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating streak for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Check and award milestone bonuses
    /// </summary>
    [AllowAnonymous]
    [HttpPost("milestone/streak/{userId:guid}")]
    public async Task<IActionResult> CheckStreakMilestone(Guid userId)
    {
        try
        {
            var result = await pointsService.CheckAndAwardStreakMilestoneAsync(userId);
            return result is null
                ? Ok(new { message = "No milestone reached" })
                : Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking streak milestone for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Check and award review milestone
    /// </summary>
    [AllowAnonymous]
    [HttpPost("milestone/reviews/{userId:guid}")]
    public async Task<IActionResult> CheckReviewMilestone(Guid userId, [FromQuery] int totalReviews)
    {
        try
        {
            var result = await pointsService.CheckAndAwardReviewMilestoneAsync(userId, totalReviews);
            return result is null
                ? Ok(new { message = "No milestone reached" })
                : Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking review milestone for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Initialize points for a new user
    /// </summary>
    [AllowAnonymous]
    [HttpPost("initialize/{userId:guid}")]
    public async Task<IActionResult> InitializeUserPoints(Guid userId)
    {
        try
        {
            await pointsService.InitializeUserPointsAsync(userId);
            return Ok(new { message = "Points initialized" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing points for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
