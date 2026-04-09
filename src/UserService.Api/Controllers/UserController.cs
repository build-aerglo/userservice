using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.DTOs.Referral;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService service, IBusinessRepRepository businessRepRepository, IBadgeService badgeService, IReferralService referralService, ILogger<UserController> logger) : ControllerBase
{
    // BUSINESS USER creates sub-business users
    [Authorize(Roles = "business_user")]
    [HttpPost("sub-business")]
    public async Task<IActionResult> CreateSubBusinessUser([FromBody] CreateSubBusinessUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await service.CreateSubBusinessUserAsync(dto);

            return Created("", new
            {
                result.UserId,
                result.BusinessId,
                result.BusinessRepId,
                result.Username,
                result.Email,
                result.Phone,
                result.Address,
                result.BranchName,
                result.BranchAddress,
                result.CreatedAt,
                result.Auth0UserId
            });
        }
        catch (BusinessNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UserCreationFailedException ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [Authorize(Roles = "business_user")]
    [HttpPut("sub-business/{userId:guid}")]
    public async Task<IActionResult> UpdateSubBusinessUser(Guid userId, [FromBody] UpdateSubBusinessUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await service.UpdateSubBusinessUserAsync(userId, dto);
            return Ok(result);
        }
        catch (SubBusinessUserNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (SubBusinessUserUpdateFailedException ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    // SUPPORT ADMIN creates support users
    [Authorize(Roles = "support_user")]
    [HttpPost("support")]
    public async Task<IActionResult> CreateSupportUser([FromBody] CreateSupportUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await service.CreateSupportUserAsync(dto);

            return Created("", new
            {
                result.UserId,
                result.Email,
                result.Username,
                result.SupportUserProfileId,
                result.CreatedAt,
                result.Auth0UserId
            });
        }
        catch (DuplicateUserEmailException ex) { return Conflict(new { error = ex.Message }); }
        catch (UserCreationFailedException ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [Authorize(Roles = "support_user")]
    [HttpPut("support/{userId:guid}")]
    public async Task<IActionResult> UpdateSupportUser(Guid userId, [FromBody] UpdateSupportUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await service.UpdateSupportUserAsync(userId, dto);
            return Ok(result);
        }
        catch (SupportUserNotFoundException ex)
        {
            logger.LogWarning(ex, "Support user not found: {UserId}", userId);
            return NotFound(new { error = ex.Message });
        }
        catch (SupportUserUpdateFailedException ex)
        {
            logger.LogError(ex, "Support user update failed: {UserId}", userId);
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error updating support user: {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error occurred." });
        }
    }

    /// <summary>
    /// Registers credentials for a business owner after a business has been claimed.
    /// Receives the businessId (pre-existing), email, password and optional phone.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register-business")]
    public async Task<IActionResult> RegisterBusinessAfterClaim([FromBody] RegisterBusinessDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await service.RegisterBusinessAfterClaimAsync(dto);
            return Created("", result);
        }
        catch (BusinessClaimExpiredException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (BusinessClaimNotApprovedException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (BusinessNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (DuplicateUserEmailException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (UserCreationFailedException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering business after claim for businessId {BusinessId}", dto.BusinessId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // PUBLIC business registration
    [AllowAnonymous]
    [HttpPost("business")]
    public async Task<IActionResult> CreateBusinessUser([FromBody] BusinessUserDto dto)
    {
        var (user, businessId, business) = await service.RegisterBusinessAccountAsync(dto);

        return Created("", new
        {
            user.Id,
            user.Email,
            businessId,
            business,
            user.Auth0UserId
        });
    }

    [Authorize(Roles = "business_user,support_user")]
    [HttpGet("business/{id:guid}")]
    public async Task<IActionResult> GetBusinessUser(Guid id)
    {
        var result = await service.GetBusinessRepByIdAsync(id);
        return result is not null ? Ok(result) : NotFound();
    }

    [AllowAnonymous]
    [HttpPost("end-user")]
    public async Task<IActionResult> CreateEndUser([FromBody] CreateEndUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await service.CreateEndUserAsync(dto);

            return Created("", new
            {
                result.UserId,
                result.Username,
                result.Email,
                result.Phone,
                result.Address,
                result.SocialMedia,
                result.CreatedAt,
                result.Auth0UserId
            });
        }
        catch (DuplicateUserEmailException ex) { return Conflict(new { error = ex.Message }); }
        catch (UserCreationFailedException ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [Authorize]
    [HttpGet("business-rep/{businessRepId:guid}")]
    public async Task<IActionResult> GetBusinessRep(Guid businessRepId)
    {
        try
        {
            var businessRep = await businessRepRepository.GetByIdAsync(businessRepId);

            if (businessRep == null)
                return NotFound(new { error = $"Business rep {businessRepId} not found" });

            return Ok(new
            {
                Id = businessRep.Id,
                BusinessId = businessRep.BusinessId,
                UserId = businessRep.UserId,
                BranchName = businessRep.BranchName,
                BranchAddress = businessRep.BranchAddress,
                CreatedAt = businessRep.CreatedAt
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting business rep {BusinessRepId}", businessRepId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [Authorize]
    [HttpGet("business-rep/parent/{businessId:guid}")]
    public async Task<IActionResult> GetParentRepByBusinessId(Guid businessId)
    {
        try
        {
            var parentRep = await businessRepRepository.GetParentRepByBusinessIdAsync(businessId);

            if (parentRep == null)
                return NotFound(new { error = $"Parent business rep for business {businessId} not found" });

            return Ok(new
            {
                Id = parentRep.Id,
                BusinessId = parentRep.BusinessId,
                UserId = parentRep.UserId,
                BranchName = parentRep.BranchName,
                BranchAddress = parentRep.BranchAddress,
                CreatedAt = parentRep.CreatedAt
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting parent rep for business {BusinessId}", businessId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [Authorize]
    [HttpGet("support-user/{userId:guid}/exists")]
    public async Task<IActionResult> IsSupportUser(Guid userId)
    {
        try
        {
            var user = await service.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound(new { error = $"User {userId} not found" });

            return Ok(new { IsSupportUser = user.UserType == "support_user" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if user {UserId} is support user", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // Public: end-user profiles are intentionally readable without authentication
    // (allows other users and services to view reviewer profiles).
    [AllowAnonymous]
    [HttpGet("end-user/{userId:guid}/profile")]
    public async Task<IActionResult> GetEndUserProfileDetail(Guid userId)
    {
        try
        {
            logger.LogInformation("Fetching end user profile for user {UserId}", userId);
            var result = await service.GetEndUserProfileDetailAsync(userId);
            return Ok(result);
        }
        catch (EndUserNotFoundException ex)
        {
            logger.LogWarning(ex, "End user {UserId} not found", userId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting end user profile for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [Authorize]
    [HttpPut("end-user/{userId:guid}/profile")]
    public async Task<IActionResult> UpdateEndUserProfileDetail(
        Guid userId,
        [FromBody] UpdateEndUserProfileDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Enforce ownership: only the user themselves may update their own profile.
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(sub, out var currentUserId) || currentUserId != userId)
            return Forbid();

        try
        {
            logger.LogInformation("Updating end user profile for user {UserId}", userId);
            var result = await service.UpdateEndUserProfileAsync(userId, dto);
            logger.LogInformation("Successfully updated end user profile for user {UserId}", userId);
            return Ok(result);
        }
        catch (EndUserNotFoundException ex)
        {
            logger.LogWarning(ex, "End user {UserId} not found for update", userId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating end user profile for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // Public: user summary is visible to anyone (review platform public data)
    [AllowAnonymous]
    [HttpGet("user-summary/{id:guid}")]
    public async Task<IActionResult> GetEndUserSummary(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5,
        [FromQuery] bool recalculate = false)
    {
        var result = await service.GetEndUserSummaryAsync(id, page, pageSize, recalculate);

        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
