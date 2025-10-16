using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Services;

namespace UserService.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize] // ✅ Secured by Auth0
public class UserController : ControllerBase
{
    private readonly IUserService _service;
    public UserController(IUserService service) => _service = service;
	
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
        => await _service.GetByIdAsync(id) is { } user ? Ok(user) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Create(UserDto dto)
    {
        var user = await _service.CreateAsync(dto.Username, dto.Email, dto.Phone, dto.UserType, dto.Address);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserDto dto)
    {
        await _service.UpdateAsync(id, dto.Email, dto.Phone, dto.Address);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }


  [HttpPost("sub-business")]
    public async Task<IActionResult> CreateSubBusinessUser([FromBody] CreateSubBusinessUserDto dto)
    {
        try
        {
            
            var result = await _service.CreateSubBusinessUserAsync(dto);

            return CreatedAtAction(nameof(Get), new { id = result.UserId }, result
            );
        }
        catch (Exception ex)
        {
            
            return BadRequest(new { error = ex.Message });
        }
    }

}

