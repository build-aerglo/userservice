using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Services;
using UserService.Domain.Exceptions;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService service, ILogger<UserController> logger) : ControllerBase
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
                result.Auth0UserId  // ✅ return Auth0 ID
            });
        }
        catch (DuplicateUserEmailException ex) { return Conflict(new { error = ex.Message }); }
        catch (UserCreationFailedException ex) { return StatusCode(500, new { error = ex.Message }); }
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

    // PUBLIC end-user sign-up
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
}
