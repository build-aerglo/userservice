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
    /// Get user's points balance
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetUserPoints(Guid userId)
    {
        try
        {
            var points = await pointsService.GetOrCreateUserPointsAsync(userId);
            return Ok(points);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting points for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve user points" });
        }
    }

    /// <summary>
    /// Get user's points summary with transactions and multipliers
    /// </summary>
    [HttpGet("user/{userId:guid}/summary")]
    public async Task<IActionResult> GetPointsSummary(Guid userId)
    {
        try
        {
            var summary = await pointsService.GetPointsSummaryAsync(userId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting points summary for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve points summary" });
        }
    }

    /// <summary>
    /// Get user's transaction history
    /// </summary>
    [HttpGet("user/{userId:guid}/transactions")]
    public async Task<IActionResult> GetTransactionHistory(Guid userId, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
    {
        try
        {
            var transactions = await pointsService.GetTransactionHistoryAsync(userId, limit, offset);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting transactions for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve transactions" });
        }
    }

    /// <summary>
    /// Get transactions by type
    /// </summary>
    [HttpGet("user/{userId:guid}/transactions/type/{transactionType}")]
    public async Task<IActionResult> GetTransactionsByType(Guid userId, string transactionType)
    {
        try
        {
            var transactions = await pointsService.GetTransactionsByTypeAsync(userId, transactionType);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting transactions by type for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve transactions" });
        }
    }

    /// <summary>
    /// Get transactions by date range
    /// </summary>
    [HttpGet("user/{userId:guid}/transactions/range")]
    public async Task<IActionResult> GetTransactionsByDateRange(Guid userId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var transactions = await pointsService.GetTransactionsByDateRangeAsync(userId, startDate, endDate);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting transactions by date range for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve transactions" });
        }
    }

    /// <summary>
    /// Earn points for an action
    /// </summary>
    [HttpPost("earn")]
    public async Task<IActionResult> EarnPoints([FromBody] EarnPointsDto dto)
    {
        try
        {
            var result = await pointsService.EarnPointsAsync(dto);
            if (result.Success)
                return Ok(result);
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error earning points for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Failed to earn points" });
        }
    }

    /// <summary>
    /// Redeem points
    /// </summary>
    [HttpPost("redeem")]
    public async Task<IActionResult> RedeemPoints([FromBody] RedeemPointsDto dto)
    {
        try
        {
            var transaction = await pointsService.RedeemPointsAsync(dto);
            return Ok(transaction);
        }
        catch (InsufficientPointsException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UserPointsNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error redeeming points for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Failed to redeem points" });
        }
    }

    /// <summary>
    /// Adjust points (Admin)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpPost("adjust")]
    public async Task<IActionResult> AdjustPoints([FromBody] AdjustPointsDto dto)
    {
        try
        {
            var transaction = await pointsService.AdjustPointsAsync(dto);
            return Ok(transaction);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adjusting points for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Failed to adjust points" });
        }
    }

    /// <summary>
    /// Get all point rules
    /// </summary>
    [HttpGet("rules")]
    public async Task<IActionResult> GetAllRules()
    {
        var rules = await pointsService.GetActiveRulesAsync();
        return Ok(rules);
    }

    /// <summary>
    /// Get point rule by action type
    /// </summary>
    [HttpGet("rules/{actionType}")]
    public async Task<IActionResult> GetRuleByActionType(string actionType)
    {
        var rule = await pointsService.GetRuleByActionTypeAsync(actionType);
        return rule != null ? Ok(rule) : NotFound(new { error = $"Rule for action '{actionType}' not found" });
    }

    /// <summary>
    /// Create a point rule (Admin)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpPost("rules")]
    public async Task<IActionResult> CreateRule([FromBody] CreatePointRuleDto dto)
    {
        try
        {
            var rule = await pointsService.CreateRuleAsync(dto);
            return Created($"/api/points/rules/{rule.ActionType}", rule);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating point rule {ActionType}", dto.ActionType);
            return StatusCode(500, new { error = "Failed to create rule" });
        }
    }

    /// <summary>
    /// Update a point rule (Admin)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpPut("rules/{id:guid}")]
    public async Task<IActionResult> UpdateRule(Guid id, [FromBody] UpdatePointRuleDto dto)
    {
        try
        {
            var rule = await pointsService.UpdateRuleAsync(id, dto);
            return Ok(rule);
        }
        catch (PointRuleNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating point rule {RuleId}", id);
            return StatusCode(500, new { error = "Failed to update rule" });
        }
    }

    /// <summary>
    /// Get active multipliers
    /// </summary>
    [HttpGet("multipliers")]
    public async Task<IActionResult> GetActiveMultipliers()
    {
        var multipliers = await pointsService.GetActiveMultipliersAsync();
        return Ok(multipliers);
    }

    /// <summary>
    /// Create a multiplier (Admin)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpPost("multipliers")]
    public async Task<IActionResult> CreateMultiplier([FromBody] CreatePointMultiplierDto dto)
    {
        try
        {
            var multiplier = await pointsService.CreateMultiplierAsync(dto);
            return Created("", multiplier);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating point multiplier {Name}", dto.Name);
            return StatusCode(500, new { error = "Failed to create multiplier" });
        }
    }

    /// <summary>
    /// Get points leaderboard
    /// </summary>
    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int count = 10)
    {
        try
        {
            var leaderboard = await pointsService.GetLeaderboardAsync(count);
            return Ok(leaderboard);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting leaderboard");
            return StatusCode(500, new { error = "Failed to retrieve leaderboard" });
        }
    }

    /// <summary>
    /// Get user's rank
    /// </summary>
    [HttpGet("user/{userId:guid}/rank")]
    public async Task<IActionResult> GetUserRank(Guid userId)
    {
        try
        {
            var rank = await pointsService.GetUserRankAsync(userId);
            return Ok(new { userId, rank });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting rank for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve rank" });
        }
    }
}
