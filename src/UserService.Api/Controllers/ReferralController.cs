using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.Referral;
using UserService.Application.Interfaces;
using UserService.Domain.Exceptions;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/referral")]
public class ReferralController(IReferralService referralService, ILogger<ReferralController> logger) : ControllerBase
{
    // ========================================================================
    // REFERRAL CODE MANAGEMENT
    // ========================================================================

    /// <summary>
    /// GET /api/referral/user/{userId}/code - Get or create user's referral code
    /// </summary>
    [AllowAnonymous]
    [HttpGet("user/{userId:guid}/code")]
    public async Task<IActionResult> GetUserReferralCode(Guid userId)
    {
        try
        {
            var result = await referralService.GetUserReferralCodeAsync(userId);
            
            // If user doesn't have a code, generate one automatically
            if (result is null)
            {
                result = await referralService.GenerateReferralCodeAsync(new GenerateReferralCodeDto(userId));
                return Created($"/api/referral/user/{userId}/code", result);
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting referral code for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// GET /api/referral/validate/{code} - Validate a referral code
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
    /// GET /api/referral/code/{code} - Look up referral code details
    /// </summary>
    [AllowAnonymous]
    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetReferralCodeDetails(string code)
    {
        try
        {
            var result = await referralService.GetReferralCodeDetailsAsync(code);
            return result is null
                ? NotFound(new { error = "Referral code not found" })
                : Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting details for referral code {Code}", code);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// PUT /api/referral/user/{userId}/code/custom - Set custom referral code
    /// </summary>
    [AllowAnonymous]
    [HttpPut("user/{userId:guid}/code/custom")]
    public async Task<IActionResult> SetCustomReferralCode(Guid userId, [FromBody] SetCustomReferralCodeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (userId != dto.UserId)
            return BadRequest(new { error = "UserId in URL must match UserId in body" });

        try
        {
            var result = await referralService.SetCustomReferralCodeAsync(dto);
            return Ok(result);
        }
        catch (ReferralCodeAlreadyExistsException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidReferralCodeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting custom referral code for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ========================================================================
    // REFERRAL USAGE
    // ========================================================================

    /// <summary>
    /// POST /api/referral/use - Use referral code when signing up
    /// </summary>
    [AllowAnonymous]
    [HttpPost("use")]
    public async Task<IActionResult> UseReferralCode([FromBody] ApplyReferralCodeDto dto)
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
            logger.LogError(ex, "Error using referral code for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ========================================================================
    // REFERRAL TRACKING
    // ========================================================================

    /// <summary>
    /// GET /api/referral/user/{userId}/referrals - Get user's referrals
    /// </summary>
    [AllowAnonymous]
    [HttpGet("user/{userId:guid}/referrals")]
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
    /// GET /api/referral/user/{userId}/summary - Get referral summary
    /// </summary>
    [AllowAnonymous]
    [HttpGet("user/{userId:guid}/summary")]
    public async Task<IActionResult> GetReferralSummary(Guid userId)
    {
        try
        {
            var result = await referralService.GetReferralStatsAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting referral summary for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// GET /api/referral/user/{userId}/referred-by - Check who referred a user
    /// </summary>
    [AllowAnonymous]
    [HttpGet("user/{userId:guid}/referred-by")]
    public async Task<IActionResult> GetReferredBy(Guid userId)
    {
        try
        {
            var result = await referralService.GetReferredByAsync(userId);
            return result is null
                ? NotFound(new { message = "User was not referred" })
                : Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking who referred user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// POST /api/referral/{referralId}/complete - Complete a referral (after 3rd approved review)
    /// </summary>
    [AllowAnonymous]
    [HttpPost("{referralId:guid}/complete")]
    public async Task<IActionResult> CompleteReferral(Guid referralId)
    {
        try
        {
            await referralService.CompleteReferralAsync(referralId);
            return Ok(new { message = "Referral completed and points awarded" });
        }
        catch (ReferralNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ReferralAlreadyCompletedException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error completing referral {ReferralId}", referralId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// POST /api/referral/review/process - Process a review approval for referral tracking
    /// Called by ReviewService when a review is approved
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

    // ========================================================================
    // LEADERBOARD & REWARDS
    // ========================================================================

    /// <summary>
    /// GET /api/referral/leaderboard - Get referral leaderboard
    /// </summary>
    [AllowAnonymous]
    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int limit = 10)
    {
        try
        {
            var result = await referralService.GetTopReferrersAsync(limit);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting referral leaderboard");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// GET /api/referral/tiers - Get reward tiers
    /// </summary>
    [AllowAnonymous]
    [HttpGet("tiers")]
    public async Task<IActionResult> GetRewardTiers()
    {
        try
        {
            var result = await referralService.GetRewardTiersAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting reward tiers");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// GET /api/referral/user/{userId}/tier - Get user's current reward tier
    /// </summary>
    [AllowAnonymous]
    [HttpGet("user/{userId:guid}/tier")]
    public async Task<IActionResult> GetUserTier(Guid userId)
    {
        try
        {
            var result = await referralService.GetUserTierAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting tier for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ========================================================================
    // CAMPAIGNS (Support Only)
    // ========================================================================

    /// <summary>
    /// GET /api/referral/campaign/active - Get active referral campaign
    /// </summary>
    [AllowAnonymous]
    [HttpGet("campaign/active")]
    public async Task<IActionResult> GetActiveCampaign()
    {
        try
        {
            var result = await referralService.GetActiveCampaignAsync();
            return result is null
                ? NotFound(new { message = "No active campaign" })
                : Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting active campaign");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// GET /api/referral/campaigns - Get all campaigns
    /// </summary>
    [AllowAnonymous]
    [HttpGet("campaigns")]
    public async Task<IActionResult> GetAllCampaigns()
    {
        try
        {
            var result = await referralService.GetAllCampaignsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting campaigns");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// POST /api/referral/campaigns - Create referral campaign (Support only)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpPost("campaigns")]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await referralService.CreateCampaignAsync(dto);
            return Created($"/api/referral/campaigns/{result.Id}", result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating campaign");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// POST /api/referral/invite - Send referral invite
    /// </summary>
    [AllowAnonymous]
    [HttpPost("invite")]
    public async Task<IActionResult> SendReferralInvite([FromBody] SendReferralInviteDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await referralService.SendReferralInviteAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending referral invite from user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ========================================================================
    // LEGACY/BACKWARD COMPATIBILITY (Keep existing endpoints)
    // ========================================================================

    [AllowAnonymous]
    [HttpGet("code/{userId:guid}")]
    [ApiExplorerSettings(IgnoreApi = true)] // Hide from Swagger
    public Task<IActionResult> GetUserReferralCodeLegacy(Guid userId) 
        => GetUserReferralCode(userId);

    [AllowAnonymous]
    [HttpPost("apply")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<IActionResult> ApplyReferralCodeLegacy([FromBody] ApplyReferralCodeDto dto) 
        => UseReferralCode(dto);

    [AllowAnonymous]
    [HttpGet("user/{userId:guid}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<IActionResult> GetUserReferralsLegacy(Guid userId) 
        => GetUserReferrals(userId);

    [AllowAnonymous]
    [HttpGet("stats/{userId:guid}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<IActionResult> GetReferralStatsLegacy(Guid userId) 
        => GetReferralSummary(userId);

    [AllowAnonymous]
    [HttpGet("was-referred/{userId:guid}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> WasUserReferredLegacy(Guid userId)
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

    [Authorize(Roles = "support_user")]
    [HttpPost("process-qualified")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> ProcessQualifiedReferralsLegacy()
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