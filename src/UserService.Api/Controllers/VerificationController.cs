using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.Verification;
using UserService.Application.Interfaces;
using UserService.Domain.Exceptions;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VerificationController(IVerificationService verificationService, ILogger<VerificationController> logger) : ControllerBase
{
    /// <summary>
    /// Get user's verification status
    /// </summary>
    [HttpGet("user/{userId:guid}/status")]
    public async Task<IActionResult> GetVerificationStatus(Guid userId)
    {
        try
        {
            var status = await verificationService.GetOrCreateVerificationStatusAsync(userId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting verification status for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve verification status" });
        }
    }

    /// <summary>
    /// Send email verification code
    /// </summary>
    [HttpPost("email/send")]
    public async Task<IActionResult> SendEmailVerification([FromBody] SendEmailVerificationDto dto)
    {
        try
        {
            var verification = await verificationService.SendEmailVerificationAsync(dto);
            return Ok(new
            {
                message = "Verification email sent",
                verificationId = verification.Id,
                expiresAt = verification.ExpiresAt,
                attemptsRemaining = verification.AttemptsRemaining
            });
        }
        catch (AlreadyVerifiedException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email verification for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Failed to send verification email" });
        }
    }

    /// <summary>
    /// Verify email with code
    /// </summary>
    [HttpPost("email/verify")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        try
        {
            var result = await verificationService.VerifyEmailAsync(dto);
            return Ok(result);
        }
        catch (VerificationNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (VerificationExpiredException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (VerificationMaxAttemptsException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidVerificationCodeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying email for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Failed to verify email" });
        }
    }

    /// <summary>
    /// Verify email via token link
    /// </summary>
    [HttpGet("email/verify/{token:guid}")]
    public async Task<IActionResult> VerifyEmailByToken(Guid token)
    {
        try
        {
            var result = await verificationService.VerifyEmailByTokenAsync(new VerifyEmailByTokenDto(token));
            return Ok(result);
        }
        catch (VerificationTokenNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (VerificationExpiredException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying email by token {Token}", token);
            return StatusCode(500, new { error = "Failed to verify email" });
        }
    }

    /// <summary>
    /// Resend email verification
    /// </summary>
    [HttpPost("email/resend/{userId:guid}")]
    public async Task<IActionResult> ResendEmailVerification(Guid userId)
    {
        try
        {
            var verification = await verificationService.ResendEmailVerificationAsync(userId);
            return Ok(new
            {
                message = "Verification email resent",
                verificationId = verification.Id,
                expiresAt = verification.ExpiresAt,
                attemptsRemaining = verification.AttemptsRemaining
            });
        }
        catch (EndUserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resending email verification for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to resend verification email" });
        }
    }

    /// <summary>
    /// Get active email verification
    /// </summary>
    [HttpGet("email/active/{userId:guid}")]
    public async Task<IActionResult> GetActiveEmailVerification(Guid userId)
    {
        var verification = await verificationService.GetActiveEmailVerificationAsync(userId);
        return verification != null
            ? Ok(verification)
            : NotFound(new { error = "No active email verification found" });
    }

    /// <summary>
    /// Send phone verification code
    /// </summary>
    [HttpPost("phone/send")]
    public async Task<IActionResult> SendPhoneVerification([FromBody] SendPhoneVerificationDto dto)
    {
        try
        {
            var verification = await verificationService.SendPhoneVerificationAsync(dto);
            return Ok(new
            {
                message = $"Verification code sent via {verification.VerificationMethod}",
                verificationId = verification.Id,
                phoneNumber = $"{verification.CountryCode}****{verification.PhoneNumber[^4..]}",
                expiresAt = verification.ExpiresAt,
                attemptsRemaining = verification.AttemptsRemaining
            });
        }
        catch (AlreadyVerifiedException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending phone verification for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Failed to send verification code" });
        }
    }

    /// <summary>
    /// Verify phone with code
    /// </summary>
    [HttpPost("phone/verify")]
    public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneDto dto)
    {
        try
        {
            var result = await verificationService.VerifyPhoneAsync(dto);
            return Ok(result);
        }
        catch (VerificationNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (VerificationExpiredException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (VerificationMaxAttemptsException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidVerificationCodeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying phone for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Failed to verify phone" });
        }
    }

    /// <summary>
    /// Resend phone verification
    /// </summary>
    [HttpPost("phone/resend/{userId:guid}")]
    public async Task<IActionResult> ResendPhoneVerification(Guid userId)
    {
        try
        {
            var verification = await verificationService.ResendPhoneVerificationAsync(userId);
            return Ok(new
            {
                message = "Verification code resent",
                verificationId = verification.Id,
                expiresAt = verification.ExpiresAt,
                attemptsRemaining = verification.AttemptsRemaining
            });
        }
        catch (VerificationNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resending phone verification for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to resend verification code" });
        }
    }

    /// <summary>
    /// Get active phone verification
    /// </summary>
    [HttpGet("phone/active/{userId:guid}")]
    public async Task<IActionResult> GetActivePhoneVerification(Guid userId)
    {
        var verification = await verificationService.GetActivePhoneVerificationAsync(userId);
        return verification != null
            ? Ok(verification)
            : NotFound(new { error = "No active phone verification found" });
    }
}
