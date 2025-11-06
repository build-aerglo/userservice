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
    
    [HttpGet("settings")]
    public async Task<IActionResult> UserSettings([FromQuery] Guid userId)
    {
        var result = await service.GetSettingsAsync(userId);
        return Ok(result);
    }
    [HttpPost("settings")]
    public async Task<IActionResult> SetUserSettings([FromBody] SettingsDto dto)
    {
        var updatedSettings = await service.SetSettingsAsync(dto);
        return Ok(updatedSettings);
    }

}
