using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.Geolocation;
using UserService.Application.Interfaces;
using UserService.Domain.Exceptions;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeolocationController(IGeolocationService geolocationService, ILogger<GeolocationController> logger) : ControllerBase
{
    /// <summary>
    /// Get user's current geolocation
    /// </summary>
    [AllowAnonymous]
    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetUserGeolocation(Guid userId)
    {
        try
        {
            var result = await geolocationService.GetUserGeolocationAsync(userId);
            return result is null
                ? NotFound(new { error = "Geolocation not found for user" })
                : Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting geolocation for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update user's geolocation
    /// </summary>
    [AllowAnonymous]
    [HttpPut]
    public async Task<IActionResult> UpdateGeolocation([FromBody] UpdateGeolocationDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await geolocationService.UpdateGeolocationAsync(dto);
            return Ok(result);
        }
        catch (InvalidCoordinatesException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (EndUserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (GeolocationUpdateFailedException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating geolocation for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Record geolocation history entry
    /// </summary>
    [AllowAnonymous]
    [HttpPost("history")]
    public async Task<IActionResult> RecordGeolocationHistory([FromBody] RecordGeolocationHistoryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await geolocationService.RecordGeolocationHistoryAsync(dto);
            return Created("", result);
        }
        catch (InvalidCoordinatesException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recording geolocation history for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get user's geolocation history
    /// </summary>
    [AllowAnonymous]
    [HttpGet("user/{userId:guid}/history")]
    public async Task<IActionResult> GetGeolocationHistory(Guid userId, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
    {
        try
        {
            var result = await geolocationService.GetGeolocationHistoryAsync(userId, limit, offset);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting geolocation history for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Enable/disable geolocation tracking for a user
    /// </summary>
    [AllowAnonymous]
    [HttpPost("toggle")]
    public async Task<IActionResult> ToggleGeolocation([FromBody] ToggleGeolocationDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await geolocationService.ToggleGeolocationAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error toggling geolocation for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Validate location for review
    /// </summary>
    [AllowAnonymous]
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateLocationForReview([FromBody] ValidateLocationForReviewDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await geolocationService.ValidateLocationForReviewAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating location for review");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get users by state
    /// </summary>
    [AllowAnonymous]
    [HttpGet("state/{state}")]
    public async Task<IActionResult> GetUsersByState(string state, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
    {
        try
        {
            var result = await geolocationService.GetUsersByStateAsync(state, limit, offset);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting users in state {State}", state);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get users by LGA
    /// </summary>
    [AllowAnonymous]
    [HttpGet("lga/{lga}")]
    public async Task<IActionResult> GetUsersByLga(string lga, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
    {
        try
        {
            var result = await geolocationService.GetUsersByLgaAsync(lga, limit, offset);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting users in LGA {Lga}", lga);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get user count by state
    /// </summary>
    [AllowAnonymous]
    [HttpGet("state/{state}/count")]
    public async Task<IActionResult> GetUserCountByState(string state)
    {
        try
        {
            var count = await geolocationService.GetUserCountByStateAsync(state);
            return Ok(new { state, userCount = count });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user count for state {State}", state);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get VPN detection count for a user
    /// </summary>
    [AllowAnonymous]
    [HttpGet("user/{userId:guid}/vpn-count")]
    public async Task<IActionResult> GetVpnDetectionCount(Guid userId)
    {
        try
        {
            var count = await geolocationService.GetVpnDetectionCountAsync(userId);
            return Ok(new { userId, vpnDetectionCount = count });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting VPN detection count for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Calculate distance between two coordinates
    /// </summary>
    [AllowAnonymous]
    [HttpGet("distance")]
    public IActionResult CalculateDistance(
        [FromQuery] double lat1,
        [FromQuery] double lon1,
        [FromQuery] double lat2,
        [FromQuery] double lon2)
    {
        if (!geolocationService.ValidateCoordinates(lat1, lon1) ||
            !geolocationService.ValidateCoordinates(lat2, lon2))
        {
            return BadRequest(new { error = "Invalid coordinates provided" });
        }

        var distance = geolocationService.CalculateDistance(lat1, lon1, lat2, lon2);
        return Ok(new { distanceKm = distance });
    }

    /// <summary>
    /// Initialize geolocation for new user
    /// </summary>
    [AllowAnonymous]
    [HttpPost("initialize/{userId:guid}")]
    public async Task<IActionResult> InitializeGeolocation(Guid userId)
    {
        try
        {
            await geolocationService.InitializeUserGeolocationAsync(userId);
            return Ok(new { message = "Geolocation initialized" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing geolocation for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Cleanup old geolocation history (Background job endpoint)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpPost("cleanup")]
    public async Task<IActionResult> CleanupOldHistory([FromQuery] int retentionDays = 90)
    {
        try
        {
            await geolocationService.CleanupOldHistoryAsync(retentionDays);
            return Ok(new { message = $"History older than {retentionDays} days cleaned up" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cleaning up old geolocation history");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
