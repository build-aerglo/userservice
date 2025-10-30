using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Services;
using UserService.Domain.Exceptions;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService service, ILogger<UserController> logger) : ControllerBase
{
    [HttpPost("sub-business")]
    public async Task<IActionResult> CreateSubBusinessUser([FromBody] CreateSubBusinessUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await service.CreateSubBusinessUserAsync(dto);

            var location = Url.Action("Get", "User", new { id = result.UserId });
            return Created(location ?? string.Empty, result);
        }
        catch (BusinessNotFoundException ex)
        {
            logger.LogWarning(ex, "Business not found: {BusinessId}", dto.BusinessId);
            return NotFound(new { error = ex.Message });
        }
        catch (UserCreationFailedException ex)
        {
            logger.LogError(ex, "User creation failed: {Username}", dto.Username);
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating sub-business user");
            return StatusCode(500, new { error = "Internal server error occurred." });
        }
    }

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
        catch (SubBusinessUserNotFoundException ex)
        {
            logger.LogWarning(ex, "Sub-business user not found: {UserId}", userId);
            return NotFound(new { error = ex.Message });
        }
        catch (SubBusinessUserUpdateFailedException ex)
        {
            logger.LogError(ex, "Sub-business user update failed: {UserId}", userId);
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error updating sub-business user: {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error occurred." });
        }
    }

    // Support User Routes 
    [HttpPost("support")]
    public async Task<IActionResult> CreateSupportUser([FromBody] CreateSupportUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await service.CreateSupportUserAsync(dto);
            
            var location = Url.Action("Get", "User", new { id = result.UserId });
            return Created(location ?? string.Empty, result);
        }
        catch (DuplicateUserEmailException ex)
        {
            logger.LogError(ex, "Email already exist: {Email}", dto.Email);
            return StatusCode(500, new { error = ex.Message });
        }
        catch (UserCreationFailedException ex)
        {
            logger.LogError(ex, "Support user creation failed: {Username}", dto.Username);
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating support user");
            return StatusCode(500, new { error = "Internal server error occurred." });
        }
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
    public async Task<IActionResult> CreateBusinessUser([FromBody] BusinessUserDto dto)
    {
        var (user, businessId, business) = await service.RegisterBusinessAccountAsync(dto);

        return CreatedAtAction(
            nameof(CreateBusinessUser),
            new { id = businessId },
            new
            {
                User = user,
                BusinessId = businessId,
                Business = business
            }
        );
    }
    
    [HttpGet("get-business-rep-user/{id:guid}")]
    public async Task<IActionResult> GetBusinessUser(Guid id)
    {
        var result = await service.GetBusinessRepByIdAsync(id);
        return result is not null ? Ok(result) : NotFound();
    }
    
    // ---------------------- END USER ----------------------
    [HttpPost("end-user")]
    public async Task<IActionResult> CreateEndUser([FromBody] CreateEndUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await service.CreateEndUserAsync(dto);

            var location = Url.Action("Get", "User", new { id = result.UserId });
            return Created(location ?? string.Empty, result);
        }
        catch (DuplicateUserEmailException ex)
        {
            logger.LogError(ex, "Email already exists: {Email}", dto.Email);
            return Conflict(new { error = ex.Message });
        }
        catch (UserCreationFailedException ex)
        {
            logger.LogError(ex, "End user creation failed: {Username}", dto.Username);
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating end user");
            return StatusCode(500, new { error = "Internal server error occurred." });
        }
    }
}
