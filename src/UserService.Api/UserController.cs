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
    public async Task<IActionResult> CreateEndUsers(UserDto dto)
    {
        var user = await _service.CreateEndUsers(dto.Username, dto.Email, dto.Phone, dto.UserType, dto.Address);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }
}

