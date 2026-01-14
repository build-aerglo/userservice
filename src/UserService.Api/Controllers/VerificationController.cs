using Microsoft.AspNetCore.Authorization;
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
    [AllowAnonymous]
    [HttpGet("status/{userId:guid}")]
    public async Task<IActionResult> GetVerificationStatus(Guid userId)
    {
        try
        {
            var result = await verificationService.GetVerificationStatusAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting verification status for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Send OTP to user's phone number
    /// </summary>
    [AllowAnonymous]
    [HttpPost("phone/send")]
    public async Task<IActionResult> SendPhoneOtp([FromBody] SendPhoneOtpDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await verificationService.SendPhoneOtpAsync(dto);
            return Ok(result);
        }
        catch (EndUserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidPhoneNumberException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (PhoneAlreadyVerifiedException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (VerificationSendFailedException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending phone OTP for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Verify phone OTP
    /// </summary>
    [AllowAnonymous]
    [HttpPost("phone/verify")]
    public async Task<IActionResult> VerifyPhoneOtp([FromBody] VerifyPhoneOtpDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await verificationService.VerifyPhoneOtpAsync(dto);
            return Ok(result);
        }
        catch (InvalidVerificationTokenException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (VerificationTokenExpiredException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (MaxVerificationAttemptsExceededException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying phone OTP for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Send email verification link
    /// </summary>
    [AllowAnonymous]
    [HttpPost("email/send")]
    public async Task<IActionResult> SendEmailVerification([FromBody] SendEmailVerificationDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await verificationService.SendEmailVerificationAsync(dto);
            return Ok(result);
        }
        catch (EndUserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (EmailAlreadyVerifiedException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (VerificationSendFailedException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email verification for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Verify email token
    /// </summary>
    [AllowAnonymous]
    [HttpPost("email/verify")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await verificationService.VerifyEmailAsync(dto);
            return Ok(result);
        }
        catch (InvalidVerificationTokenException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (VerificationTokenExpiredException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying email for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Check if user is verified
    /// </summary>
    [AllowAnonymous]
    [HttpGet("check/{userId:guid}")]
    public async Task<IActionResult> CheckVerification(Guid userId)
    {
        try
        {
            var isVerified = await verificationService.IsUserVerifiedAsync(userId);
            var isFullyVerified = await verificationService.IsUserFullyVerifiedAsync(userId);
            var level = await verificationService.GetVerificationLevelAsync(userId);
            var multiplier = await verificationService.GetPointsMultiplierAsync(userId);

            return Ok(new
            {
                userId,
                isVerified,
                isFullyVerified,
                level,
                pointsMultiplier = multiplier
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking verification for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Validate Nigerian phone number format
    /// </summary>
    [AllowAnonymous]
    [HttpGet("validate-phone")]
    public IActionResult ValidatePhoneNumber([FromQuery] string phoneNumber)
    {
        var isValid = verificationService.ValidateNigerianPhoneNumber(phoneNumber);
        return Ok(new { phoneNumber, isValid });
    }

    /// <summary>
    /// Initialize verification for new user
    /// </summary>
    [AllowAnonymous]
    [HttpPost("initialize/{userId:guid}")]
    public async Task<IActionResult> InitializeVerification(Guid userId)
    {
        try
        {
            await verificationService.InitializeUserVerificationAsync(userId);
            return Ok(new { message = "Verification initialized" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing verification for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Cleanup expired verification tokens (Background job endpoint)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpPost("cleanup")]
    public async Task<IActionResult> CleanupExpiredTokens()
    {
        try
        {
            await verificationService.CleanupExpiredTokensAsync();
            return Ok(new { message = "Expired tokens cleaned up" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cleaning up expired tokens");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
