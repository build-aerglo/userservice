using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService service, IBusinessRepRepository businessRepRepository, IBadgeService badgeService, ILogger<UserController> logger) : ControllerBase
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
                result.Auth0UserId   // ✅ return Auth0 ID
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
    //[Authorize(Roles = "support_user")]
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
                result.Auth0UserId  // ✅ return Auth0 ID
            });
        }
        catch (DuplicateUserEmailException ex) { return Conflict(new { error = ex.Message }); }
        catch (UserCreationFailedException ex) { return StatusCode(500, new { error = ex.Message }); }
    }


		


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
    
    [HttpPost("create-business-user")]

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
            user.Auth0UserId  // ✅ return Auth0 ID
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
                result.Auth0UserId  // ✅ return Auth0 ID
            });
        }
        catch (DuplicateUserEmailException ex) { return Conflict(new { error = ex.Message }); }
        catch (UserCreationFailedException ex) { return StatusCode(500, new { error = ex.Message }); }
    }


    [AllowAnonymous]
    [HttpGet("business-rep/{businessRepId:guid}")]
    public async Task<IActionResult> GetBusinessRep(Guid businessRepId)
    {
        try
        {
            var businessRep = await businessRepRepository.GetByIdAsync(businessRepId);
            
            if (businessRep == null)
            {
                return NotFound(new { error = $"Business rep {businessRepId} not found" });
            }

            var dto = new
            {
                Id = businessRep.Id,
                BusinessId = businessRep.BusinessId,
                UserId = businessRep.UserId,
                BranchName = businessRep.BranchName,
                BranchAddress = businessRep.BranchAddress,
                CreatedAt = businessRep.CreatedAt
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting business rep {BusinessRepId}", businessRepId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
    
    [AllowAnonymous]
    [HttpGet("business-rep/parent/{businessId:guid}")]
    public async Task<IActionResult> GetParentRepByBusinessId(Guid businessId)
    {
        try
        {
            var parentRep = await businessRepRepository.GetParentRepByBusinessIdAsync(businessId);
            
            if (parentRep == null)
            {
                return NotFound(new { error = $"Parent business rep for business {businessId} not found" });
            }

            var dto = new
            {
                Id = parentRep.Id,
                BusinessId = parentRep.BusinessId,
                UserId = parentRep.UserId,
                BranchName = parentRep.BranchName,
                BranchAddress = parentRep.BranchAddress,
                CreatedAt = parentRep.CreatedAt
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting parent rep for business {BusinessId}", businessId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
    
    [AllowAnonymous]
    [HttpGet("support-user/{userId:guid}/exists")]
    public async Task<IActionResult> IsSupportUser(Guid userId)
    {
        try
        {
            var user = await service.GetUserByIdAsync(userId);
            
            if (user == null)
            {
                return NotFound(new { error = $"User {userId} not found" });
            }

            // Check if user type is support_user
            var isSupportUser = user.UserType == "support_user";

            return Ok(new { IsSupportUser = isSupportUser });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if user {UserId} is support user", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
    
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
    
    
    [AllowAnonymous]
    [HttpPut("end-user/{userId:guid}/profile")]
    public async Task<IActionResult> UpdateEndUserProfileDetail(
        Guid userId, 
        [FromBody] UpdateEndUserProfileDto dto)
    {
        if (!ModelState.IsValid)  
            return BadRequest(ModelState);
        
        
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

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


    [AllowAnonymous]
    [HttpGet("user-summary/{id:guid}")]
    public async Task<IActionResult> GetEndUserSummary(
        Guid id,
        [FromQuery] bool recalculate = true)
    {
        if (recalculate)
            await badgeService.RecalculateAllBadgesAsync(id);
        
        var result = await service.GetEndUserSummaryAsync(id);
        return Ok(result);
    }
}
