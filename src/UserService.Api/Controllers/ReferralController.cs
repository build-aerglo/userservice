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
    /// Get or create user's referral code
    /// </summary>
    [HttpGet("user/{userId:guid}/code")]
    public async Task<IActionResult> GetReferralCode(Guid userId)
    {
        try
        {
            var code = await referralService.GetOrCreateReferralCodeAsync(userId);
            return Ok(code);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting referral code for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve referral code" });
        }
    }

    /// <summary>
    /// Validate a referral code
    /// </summary>
    [HttpGet("validate/{code}")]
    public async Task<IActionResult> ValidateCode(string code, [FromQuery] Guid referredUserId)
    {
        var isValid = await referralService.ValidateReferralCodeAsync(code, referredUserId);
        return Ok(new { isValid, code });
    }

    /// <summary>
    /// Look up referral code details
    /// </summary>
    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetCodeDetails(string code)
    {
        var referralCode = await referralService.GetReferralCodeByCodeAsync(code);
        return referralCode != null
            ? Ok(referralCode)
            : NotFound(new { error = $"Referral code '{code}' not found" });
    }

    /// <summary>
    /// Set custom referral code
    /// </summary>
    [HttpPut("user/{userId:guid}/code/custom")]
    public async Task<IActionResult> SetCustomCode(Guid userId, [FromBody] SetCustomCodeDto dto)
    {
        try
        {
            var code = await referralService.SetCustomCodeAsync(userId, dto.CustomCode);
            return Ok(code);
        }
        catch (UserReferralCodeNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ReferralCodeAlreadyExistsException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting custom code for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to set custom code" });
        }
    }

    /// <summary>
    /// Use a referral code when signing up
    /// </summary>
    [HttpPost("use")]
    public async Task<IActionResult> UseReferralCode([FromBody] UseReferralCodeDto dto)
    {
        try
        {
            var result = await referralService.UseReferralCodeAsync(dto);
            if (result.Success)
                return Ok(result);
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error using referral code for user {UserId}", dto.ReferredUserId);
            return StatusCode(500, new { error = "Failed to use referral code" });
        }
    }

    /// <summary>
    /// Get user's referrals
    /// </summary>
    [HttpGet("user/{userId:guid}/referrals")]
    public async Task<IActionResult> GetUserReferrals(Guid userId)
    {
        try
        {
            var referrals = await referralService.GetReferralsByReferrerAsync(userId);
            return Ok(referrals);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting referrals for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve referrals" });
        }
    }

    /// <summary>
    /// Get user's referral summary
    /// </summary>
    [HttpGet("user/{userId:guid}/summary")]
    public async Task<IActionResult> GetReferralSummary(Guid userId)
    {
        try
        {
            var summary = await referralService.GetReferralSummaryAsync(userId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting referral summary for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve referral summary" });
        }
    }

    /// <summary>
    /// Check who referred a user
    /// </summary>
    [HttpGet("user/{userId:guid}/referred-by")]
    public async Task<IActionResult> GetReferredBy(Guid userId)
    {
        var referral = await referralService.GetReferralByReferredUserIdAsync(userId);
        return referral != null
            ? Ok(referral)
            : NotFound(new { error = "User was not referred by anyone" });
    }

    /// <summary>
    /// Complete a referral (when referred user completes required action)
    /// </summary>
    [HttpPost("{referralId:guid}/complete")]
    public async Task<IActionResult> CompleteReferral(Guid referralId)
    {
        try
        {
            var referral = await referralService.CompleteReferralAsync(referralId);
            return Ok(referral);
        }
        catch (ReferralNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ReferralAlreadyCompletedException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ReferralExpiredException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error completing referral {ReferralId}", referralId);
            return StatusCode(500, new { error = "Failed to complete referral" });
        }
    }

    /// <summary>
    /// Get referral leaderboard
    /// </summary>
    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int count = 10)
    {
        try
        {
            var leaderboard = await referralService.GetReferralLeaderboardAsync(count);
            return Ok(leaderboard);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting referral leaderboard");
            return StatusCode(500, new { error = "Failed to retrieve leaderboard" });
        }
    }

    /// <summary>
    /// Get reward tiers
    /// </summary>
    [HttpGet("tiers")]
    public async Task<IActionResult> GetRewardTiers()
    {
        var tiers = await referralService.GetRewardTiersAsync();
        return Ok(tiers);
    }

    /// <summary>
    /// Get user's current reward tier
    /// </summary>
    [HttpGet("user/{userId:guid}/tier")]
    public async Task<IActionResult> GetCurrentTier(Guid userId)
    {
        var tier = await referralService.GetCurrentTierAsync(userId);
        return tier != null
            ? Ok(tier)
            : NotFound(new { error = "No tier found" });
    }

    /// <summary>
    /// Get active referral campaign
    /// </summary>
    [HttpGet("campaign/active")]
    public async Task<IActionResult> GetActiveCampaign()
    {
        var campaign = await referralService.GetActiveCampaignAsync();
        return campaign != null
            ? Ok(campaign)
            : NotFound(new { error = "No active campaign" });
    }

    /// <summary>
    /// Get all campaigns
    /// </summary>
    [HttpGet("campaigns")]
    public async Task<IActionResult> GetAllCampaigns()
    {
        var campaigns = await referralService.GetAllCampaignsAsync();
        return Ok(campaigns);
    }

    /// <summary>
    /// Create a referral campaign (Admin)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpPost("campaigns")]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateReferralCampaignDto dto)
    {
        try
        {
            var campaign = await referralService.CreateCampaignAsync(dto);
            return Created($"/api/referral/campaigns/{campaign.Id}", campaign);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating referral campaign {Name}", dto.Name);
            return StatusCode(500, new { error = "Failed to create campaign" });
        }
    }

    /// <summary>
    /// Send referral invite
    /// </summary>
    [HttpPost("invite")]
    public async Task<IActionResult> SendInvite([FromBody] SendReferralInviteDto dto)
    {
        try
        {
            var referral = await referralService.SendReferralInviteAsync(dto);
            return Ok(new
            {
                message = "Invitation sent",
                referralId = referral.Id,
                expiresAt = referral.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending referral invite from user {UserId}", dto.ReferrerUserId);
            return StatusCode(500, new { error = "Failed to send invite" });
        }
    }
}
