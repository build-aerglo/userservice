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
    
    [HttpPatch("update-business-user")]
    public async Task<IActionResult> UpdateBusinessUser([FromQuery] UpdateBusinessUserDto dto)
    {
        
        try
        {
            await service.UpdateBusinessAccount(dto);
            return NoContent();
        }
        catch (BusinessNotFoundException ex)
        {
            logger.LogError(ex, "Business Not Found: {Id}", dto.Id);
            return StatusCode(500, new { error = ex.Message });
        }
        catch (UserNotFoundException ex)
        {
            logger.LogError(ex, "User Not Found");
            return StatusCode(500, new { error = ex.Message });
        }
        catch (BusinessNotUpdatedException ex)
        {
            logger.LogError(ex, "Unexpected error when updating business");
            return StatusCode(500, new { error = "Internal server error occurred." });
        }
    }
    
    [HttpDelete("{id:guid}/delete")]
    public async Task<IActionResult> DeleteUser(Guid id, [FromBody] string type)
    {
        
        try
        {
            await service.DeleteUserAsync(id, type);
            return Ok();
        }
        catch (UserNotFoundException ex)
        {
            logger.LogError(ex, "User Not Found");
            return StatusCode(500, new { error = ex.Message });
        }
        catch (UserTypeNotFoundException ex)
        {
            logger.LogError(ex, "User Type Error");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
