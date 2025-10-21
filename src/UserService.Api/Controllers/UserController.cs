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
    
    [HttpGet("get-business-user/{id:guid}")]
    public async Task<IActionResult> GetBusinessUser(Guid id)
        => await service.GetBusinessRepByIdAsync(id) is { } user ? Ok(user) : NotFound();
    
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
}
