using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.Points;
using UserService.Application.Interfaces;
using UserService.Domain.Exceptions;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PointsController(
    IPointsService pointsService, 
    ILogger<PointsController> logger,
    IReviewServiceClient reviewServiceClient) : ControllerBase 
{
    private readonly IReviewServiceClient _reviewServiceClient;
    
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
            await pointsService.UpdateLoginStreakAsync(userId, DateTime.UtcNow);
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
    /// Check and award helpful vote milestone (100 helpful votes)
    /// Called by ReviewService when a reviewer reaches 100 total helpful votes
    /// </summary>
    [AllowAnonymous]
    [HttpPost("milestone/helpful-votes/{userId:guid}")]
    public async Task<IActionResult> CheckHelpfulVoteMilestone(Guid userId)
    {
        try
        {
            // Get total helpful votes from ReviewService
            var totalHelpfulVotes = await reviewServiceClient.GetTotalHelpfulVotesForUserAsync(userId);
        
            logger.LogInformation(
                "Checking helpful vote milestone for user {UserId} with {TotalVotes} total votes", 
                userId, 
                totalHelpfulVotes);

            var result = await pointsService.CheckAndAwardHelpfulVoteMilestoneAsync(userId, totalHelpfulVotes);
        
            return result is null
                ? Ok(new { message = "No milestone reached", totalHelpfulVotes })
                : Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking helpful vote milestone for user {UserId}", userId);
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
    
    
    // Redemption Endpoints
[Authorize(Roles = "end_user")]
[HttpPost("redeem")]
public async Task<IActionResult> RedeemPoints([FromBody] RedeemPointsDto dto)
{
    try
    {
        var result = await pointsService.RedeemPointsAsync(dto);
        return Ok(result);
    }
    catch (InsufficientPointsException ex)
    {
        return BadRequest(new { error = ex.Message });
    }
    catch (InvalidPhoneNumberException ex)
    {
        return BadRequest(new { error = ex.Message });
    }
    catch (PointRedemptionFailedException ex)
    {
        return StatusCode(500, new { error = ex.Message });
    }
}

[Authorize(Roles = "end_user")]
[HttpGet("user/{userId:guid}/redemptions")]
public async Task<IActionResult> GetRedemptionHistory(Guid userId, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
{
    try
    {
        var result = await pointsService.GetRedemptionHistoryAsync(userId, limit, offset);
        return Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error getting redemption history");
        return StatusCode(500, new { error = "Internal server error" });
    }
}

// Point Rules Endpoints
[AllowAnonymous]
[HttpGet("rules")]
public async Task<IActionResult> GetAllRules()
{
    try
    {
        var result = await pointsService.GetAllPointRulesAsync();
        return Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error getting point rules");
        return StatusCode(500, new { error = "Internal server error" });
    }
}

[AllowAnonymous]
[HttpGet("rules/{actionType}")]
public async Task<IActionResult> GetRuleByActionType(string actionType)
{
    try
    {
        var result = await pointsService.GetPointRuleByActionTypeAsync(actionType);
        return Ok(result);
    }
    catch (PointRuleNotFoundException ex)
    {
        return NotFound(new { error = ex.Message });
    }
}

[Authorize(Roles = "support_user")]
[HttpPost("rules")]
public async Task<IActionResult> CreatePointRule([FromBody] CreatePointRuleDto dto)
{
    try
    {
        var userId = GetCurrentUserId();
        var result = await pointsService.CreatePointRuleAsync(dto, userId);
        return Created("", result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating point rule");
        return StatusCode(500, new { error = "Internal server error" });
    }
}

// Point Multipliers Endpoints
[AllowAnonymous]
[HttpGet("multipliers")]
public async Task<IActionResult> GetActiveMultipliers()
{
    try
    {
        var result = await pointsService.GetActivePointMultipliersAsync();
        return Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error getting multipliers");
        return StatusCode(500, new { error = "Internal server error" });
    }
}

[Authorize(Roles = "support_user")]
[HttpPost("multipliers")]
public async Task<IActionResult> CreateMultiplier([FromBody] CreatePointMultiplierDto dto)
{
    try
    {
        var userId = GetCurrentUserId();
        var result = await pointsService.CreatePointMultiplierAsync(dto, userId);
        return Created("", result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating multiplier");
        return StatusCode(500, new { error = "Internal server error" });
    }
}

// Summary and Transaction Query Endpoints
[AllowAnonymous]
[HttpGet("user/{userId:guid}/summary")]
public async Task<IActionResult> GetPointsSummary(Guid userId, [FromQuery] int transactionLimit = 10)
{
    try
    {
        var result = await pointsService.GetUserPointsSummaryAsync(userId, transactionLimit);
        return Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error getting points summary");
        return StatusCode(500, new { error = "Internal server error" });
    }
}

[AllowAnonymous]
[HttpGet("user/{userId:guid}/transactions/type/{type}")]
public async Task<IActionResult> GetTransactionsByType(Guid userId, string type)
{
    try
    {
        var result = await pointsService.GetTransactionsByTypeAsync(userId, type);
        return Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error getting transactions by type");
        return StatusCode(500, new { error = "Internal server error" });
    }
}

[AllowAnonymous]
[HttpGet("user/{userId:guid}/transactions/range")]
public async Task<IActionResult> GetTransactionsByDateRange(
    Guid userId, 
    [FromQuery] DateTime startDate, 
    [FromQuery] DateTime endDate)
{
    try
    {
        var result = await pointsService.GetTransactionsByDateRangeAsync(userId, startDate, endDate);
        return Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error getting transactions by date range");
        return StatusCode(500, new { error = "Internal server error" });
    }
}

[AllowAnonymous]
[HttpGet("user/{userId:guid}/rank")]
public async Task<IActionResult> GetUserRank(Guid userId)
{
    try
    {
        var userPoints = await pointsService.GetUserPointsAsync(userId);
        return Ok(new { userId, rank = userPoints.Rank });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error getting user rank");
        return StatusCode(500, new { error = "Internal server error" });
    }
}

// Helper method
private Guid? GetCurrentUserId()
{
    var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;
    return Guid.TryParse(subClaim, out var userId) ? userId : null;
}
}
