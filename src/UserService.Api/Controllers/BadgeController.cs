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
    /// Get all badges for a user
    /// </summary>
    [AllowAnonymous]
    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetUserBadges(Guid userId)
    {
        try
        {
            var result = await badgeService.GetUserBadgesAsync(userId);
            return Ok(result);
        }
        catch (EndUserNotFoundException ex)
        {
            logger.LogWarning(ex, "User {UserId} not found", userId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting badges for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a specific badge by ID
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{badgeId:guid}")]
    public async Task<IActionResult> GetBadge(Guid badgeId)
    {
        try
        {
            var result = await badgeService.GetBadgeByIdAsync(badgeId);
            return result is null
                ? NotFound(new { error = $"Badge {badgeId} not found" })
                : Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting badge {BadgeId}", badgeId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Assign a badge to a user (Support/Admin only)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpPost]
    public async Task<IActionResult> AssignBadge([FromBody] AssignBadgeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await badgeService.AssignBadgeAsync(dto);
            return Created($"/api/badge/{result.Id}", result);
        }
        catch (EndUserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidBadgeTypeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (BadgeAlreadyExistsException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (BadgeAssignmentFailedException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning badge to user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Revoke a badge from a user (Support/Admin only)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpDelete]
    public async Task<IActionResult> RevokeBadge([FromBody] RevokeBadgeDto dto)
    {
        try
        {
            var result = await badgeService.RevokeBadgeAsync(dto);
            return result
                ? Ok(new { message = "Badge revoked successfully" })
                : NotFound(new { error = "Badge not found" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error revoking badge from user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get badge information (display name, description, icon)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("info/{badgeType}")]
    public IActionResult GetBadgeInfo(string badgeType, [FromQuery] string? location = null, [FromQuery] string? category = null)
    {
        var (displayName, description, icon) = badgeService.GetBadgeInfo(badgeType, location, category);
        return Ok(new { badgeType, displayName, description, icon });
    }

    /// <summary>
    /// Recalculate all badges for a user (Internal/Background job)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpPost("recalculate/{userId:guid}")]
    public async Task<IActionResult> RecalculateBadges(Guid userId)
    {
        try
        {
            await badgeService.RecalculateAllBadgesAsync(userId);
            return Ok(new { message = "Badges recalculated successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recalculating badges for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
