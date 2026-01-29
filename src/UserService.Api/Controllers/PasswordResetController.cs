using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.PasswordReset;
using UserService.Application.Interfaces;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/password")]
public class PasswordResetController : ControllerBase
{
    private readonly IPasswordResetService _passwordResetService;
    private readonly ILogger<PasswordResetController> _logger;

    public PasswordResetController(
        IPasswordResetService passwordResetService,
        ILogger<PasswordResetController> logger)
    {
        _passwordResetService = passwordResetService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("reset-email")]
    public async Task<IActionResult> ResetEmail([FromBody] ResetEmailRequest request)
    {
        try
        {
            var (success, message) = await _passwordResetService.ResetEmailAsync(request);

            if (!success)
            {
                return BadRequest(new { error = "email_reset_failed", message });
            }

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting email from {CurrentEmail}", request.CurrentEmail);
            return StatusCode(500, new
            {
                error = "server_error",
                message = "Unexpected error occurred while resetting email."
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("request-password-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetRequest request)
    {
        try
        {
            var (success, message) = await _passwordResetService.RequestPasswordResetAsync(request);

            if (!success)
            {
                return BadRequest(new { error = "password_reset_request_failed", message });
            }

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting password reset for {Id}", request.Id);
            return StatusCode(500, new
            {
                error = "server_error",
                message = "Unexpected error occurred while requesting password reset."
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            var (success, message) = await _passwordResetService.ResetPasswordAsync(request);

            if (!success)
            {
                return BadRequest(new { error = "password_reset_failed", message });
            }

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for {Id}", request.Id);
            return StatusCode(500, new
            {
                error = "server_error",
                message = "Unexpected error occurred while resetting password."
            });
        }
    }
}
