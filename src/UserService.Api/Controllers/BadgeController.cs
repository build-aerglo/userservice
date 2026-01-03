using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.Badge;
using UserService.Application.Interfaces;
using UserService.Domain.Exceptions;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BadgeController(IBadgeService badgeService, ILogger<BadgeController> logger) : ControllerBase
{
    /// <summary>
    /// Get all badge definitions
    /// </summary>
    [HttpGet("definitions")]
    public async Task<IActionResult> GetAllBadges()
    {
        var badges = await badgeService.GetActiveBadgesAsync();
        return Ok(badges);
    }

    /// <summary>
    /// Get badge by ID
    /// </summary>
    [HttpGet("definitions/{id:guid}")]
    public async Task<IActionResult> GetBadgeById(Guid id)
    {
        var badge = await badgeService.GetBadgeByIdAsync(id);
        return badge != null ? Ok(badge) : NotFound(new { error = $"Badge {id} not found" });
    }

    /// <summary>
    /// Get badges by category
    /// </summary>
    [HttpGet("definitions/category/{category}")]
    public async Task<IActionResult> GetBadgesByCategory(string category)
    {
        var badges = await badgeService.GetBadgesByCategoryAsync(category);
        return Ok(badges);
    }

    /// <summary>
    /// Get user's earned badges
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetUserBadges(Guid userId)
    {
        try
        {
            var badges = await badgeService.GetUserBadgesAsync(userId);
            return Ok(badges);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting badges for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve user badges" });
        }
    }

    /// <summary>
    /// Get user's badge level and progress
    /// </summary>
    [HttpGet("user/{userId:guid}/level")]
    public async Task<IActionResult> GetUserBadgeLevel(Guid userId)
    {
        try
        {
            var level = await badgeService.GetUserBadgeLevelAsync(userId);
            return Ok(level);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting badge level for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve badge level" });
        }
    }

    /// <summary>
    /// Get user's badge summary including level, badges, and available badges
    /// </summary>
    [HttpGet("user/{userId:guid}/summary")]
    public async Task<IActionResult> GetUserBadgeSummary(Guid userId)
    {
        try
        {
            var summary = await badgeService.GetUserBadgeSummaryAsync(userId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting badge summary for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve badge summary" });
        }
    }

    /// <summary>
    /// Check if user has a specific badge
    /// </summary>
    [HttpGet("user/{userId:guid}/has/{badgeName}")]
    public async Task<IActionResult> HasBadge(Guid userId, string badgeName)
    {
        var hasBadge = await badgeService.HasBadgeAsync(userId, badgeName);
        return Ok(new { hasBadge });
    }

    /// <summary>
    /// Award a badge to a user
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpPost("award")]
    public async Task<IActionResult> AwardBadge([FromBody] AwardBadgeDto dto)
    {
        try
        {
            var badge = await badgeService.AwardBadgeAsync(dto.UserId, dto.BadgeName, dto.Source);
            return Created("", badge);
        }
        catch (BadgeNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BadgeAlreadyEarnedException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error awarding badge {BadgeName} to user {UserId}", dto.BadgeName, dto.UserId);
            return StatusCode(500, new { error = "Failed to award badge" });
        }
    }

    /// <summary>
    /// Check and award eligible badges based on user's points
    /// </summary>
    [HttpPost("user/{userId:guid}/check-eligible")]
    public async Task<IActionResult> CheckAndAwardEligibleBadges(Guid userId)
    {
        try
        {
            var awardedBadges = await badgeService.CheckAndAwardEligibleBadgesAsync(userId);
            return Ok(new { awardedBadges, count = awardedBadges.Count() });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking eligible badges for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to check eligible badges" });
        }
    }

    /// <summary>
    /// Create a new badge definition (Admin)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpPost("definitions")]
    public async Task<IActionResult> CreateBadge([FromBody] CreateBadgeDefinitionDto dto)
    {
        try
        {
            var badge = await badgeService.CreateBadgeAsync(dto);
            return Created($"/api/badge/definitions/{badge.Id}", badge);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating badge {BadgeName}", dto.Name);
            return StatusCode(500, new { error = "Failed to create badge" });
        }
    }

    /// <summary>
    /// Update a badge definition (Admin)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpPut("definitions/{id:guid}")]
    public async Task<IActionResult> UpdateBadge(Guid id, [FromBody] UpdateBadgeDefinitionDto dto)
    {
        try
        {
            var badge = await badgeService.UpdateBadgeAsync(id, dto);
            return Ok(badge);
        }
        catch (BadgeNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating badge {BadgeId}", id);
            return StatusCode(500, new { error = "Failed to update badge" });
        }
    }

    /// <summary>
    /// Activate a badge (Admin)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpPost("definitions/{id:guid}/activate")]
    public async Task<IActionResult> ActivateBadge(Guid id)
    {
        try
        {
            await badgeService.ActivateBadgeAsync(id);
            return Ok(new { message = "Badge activated" });
        }
        catch (BadgeNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deactivate a badge (Admin)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpPost("definitions/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateBadge(Guid id)
    {
        try
        {
            await badgeService.DeactivateBadgeAsync(id);
            return Ok(new { message = "Badge deactivated" });
        }
        catch (BadgeNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
