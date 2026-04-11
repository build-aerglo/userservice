using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

    /// <summary>
    /// Change email address for the currently authenticated user.
    /// Requires the user to be logged in.
    /// </summary>
    [Authorize]
    [HttpPost("reset-email")]
    public async Task<IActionResult> ResetEmail([FromBody] ResetEmailRequest request)
    {
        try
        {
            var (success, message) = await _passwordResetService.ResetEmailAsync(request);

            if (!success)
                return BadRequest(new { error = "email_reset_failed", message });

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

    /// <summary>
    /// Initiates a forgot-password flow by sending an OTP to the user's email or phone.
    /// Intentionally anonymous — user is not logged in when they have forgotten their password.
    /// Rate-limited to prevent SMS/email bombing.
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting("sensitive")]
    [HttpPost("request-password-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetRequest request)
    {
        try
        {
            var (success, message) = await _passwordResetService.RequestPasswordResetAsync(request);

            if (!success)
                return BadRequest(new { error = "password_reset_request_failed", message });

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

    /// <summary>
    /// Completes a forgot-password reset using the OTP-verified reset request stored in the DB.
    /// Intentionally anonymous — user is not logged in when resetting a forgotten password.
    /// The service validates that a pending, non-expired reset request exists before applying the change.
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting("sensitive")]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            var (success, message) = await _passwordResetService.ResetPasswordAsync(request);

            if (!success)
                return BadRequest(new { error = "password_reset_failed", message });

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

    /// <summary>
    /// Changes the password for the currently authenticated user by verifying the old password first.
    /// Requires the user to be logged in.
    /// </summary>
    [Authorize]
    [HttpPost("update-password")]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
    {
        try
        {
            var (success, message) = await _passwordResetService.UpdatePasswordAsync(request);

            if (!success)
                return BadRequest(new { error = "password_update_failed", message });

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password for {Email}", request.Email);
            return StatusCode(500, new
            {
                error = "server_error",
                message = "Unexpected error occurred while updating password."
            });
        }
    }
}
