using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.Referral;
using UserService.Application.Interfaces;
using UserService.Domain.Exceptions;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReferralController(IReferralService referralService, ILogger<ReferralController> logger) : ControllerBase
{
    /// <summary>
    /// Get user's referral code
    /// </summary>
    [AllowAnonymous]
    [HttpGet("code/{userId:guid}")]
    public async Task<IActionResult> GetUserReferralCode(Guid userId)
    {
        try
        {
            var result = await referralService.GetUserReferralCodeAsync(userId);
            return result is null
                ? NotFound(new { error = "User does not have a referral code" })
                : Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting referral code for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Generate a new referral code for a user
    /// </summary>
    [AllowAnonymous]
    [HttpPost("code/generate")]
    public async Task<IActionResult> GenerateReferralCode([FromBody] GenerateReferralCodeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await referralService.GenerateReferralCodeAsync(dto);
            return Created($"/api/referral/code/{dto.UserId}", result);
        }
        catch (EndUserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ReferralCodeAlreadyExistsException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating referral code for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Apply a referral code when user signs up
    /// </summary>
    [AllowAnonymous]
    [HttpPost("apply")]
    public async Task<IActionResult> ApplyReferralCode([FromBody] ApplyReferralCodeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await referralService.ApplyReferralCodeAsync(dto);
            return Ok(result);
        }
        catch (EndUserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UserAlreadyReferredException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ReferralCodeNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ReferralCodeInactiveException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (SelfReferralException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying referral code for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all referrals made by a user
    /// </summary>
    [AllowAnonymous]
    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetUserReferrals(Guid userId)
    {
        try
        {
            var result = await referralService.GetUserReferralsAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting referrals for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get referral statistics for a user
    /// </summary>
    [AllowAnonymous]
    [HttpGet("stats/{userId:guid}")]
    public async Task<IActionResult> GetReferralStats(Guid userId)
    {
        try
        {
            var result = await referralService.GetReferralStatsAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting referral stats for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Process a review approval for a referred user
    /// </summary>
    [AllowAnonymous]
    [HttpPost("review/process")]
    public async Task<IActionResult> ProcessReferralReview([FromBody] ProcessReferralReviewDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await referralService.ProcessReferralReviewAsync(dto);
            return Ok(new { message = "Referral review processed" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing referral review for user {UserId}", dto.ReferredUserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Validate a referral code
    /// </summary>
    [AllowAnonymous]
    [HttpGet("validate/{code}")]
    public async Task<IActionResult> ValidateReferralCode(string code)
    {
        try
        {
            var isValid = await referralService.ValidateReferralCodeAsync(code);
            return Ok(new { code, isValid });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating referral code {Code}", code);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Check if user was referred
    /// </summary>
    [AllowAnonymous]
    [HttpGet("was-referred/{userId:guid}")]
    public async Task<IActionResult> WasUserReferred(Guid userId)
    {
        try
        {
            var wasReferred = await referralService.WasUserReferredAsync(userId);
            return Ok(new { userId, wasReferred });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if user {UserId} was referred", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get top referrers leaderboard
    /// </summary>
    [AllowAnonymous]
    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetTopReferrers([FromQuery] int limit = 10)
    {
        try
        {
            var result = await referralService.GetTopReferrersAsync(limit);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting top referrers");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Process all qualified referrals (Background job endpoint)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpPost("process-qualified")]
    public async Task<IActionResult> ProcessQualifiedReferrals()
    {
        try
        {
            await referralService.ProcessQualifiedReferralsAsync();
            return Ok(new { message = "Qualified referrals processed" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing qualified referrals");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
