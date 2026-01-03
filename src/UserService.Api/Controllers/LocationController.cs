using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.Location;
using UserService.Application.Interfaces;
using UserService.Domain.Exceptions;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationController(ILocationService locationService, ILogger<LocationController> logger) : ControllerBase
{
    /// <summary>
    /// Record user's current location
    /// </summary>
    [HttpPost("record")]
    public async Task<IActionResult> RecordLocation([FromBody] RecordLocationDto dto)
    {
        try
        {
            var location = await locationService.RecordLocationAsync(dto);
            return Created("", location);
        }
        catch (InvalidCoordinatesException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (LocationHistoryDisabledException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recording location for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Failed to record location" });
        }
    }

    /// <summary>
    /// Get user's latest location
    /// </summary>
    [HttpGet("user/{userId:guid}/latest")]
    public async Task<IActionResult> GetLatestLocation(Guid userId)
    {
        var location = await locationService.GetLatestLocationAsync(userId);
        return location != null
            ? Ok(location)
            : NotFound(new { error = "No location found for user" });
    }

    /// <summary>
    /// Get user's location history
    /// </summary>
    [HttpGet("user/{userId:guid}/history")]
    public async Task<IActionResult> GetLocationHistory(Guid userId, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
    {
        try
        {
            var history = await locationService.GetLocationHistoryAsync(userId, limit, offset);
            return Ok(history);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting location history for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve location history" });
        }
    }

    /// <summary>
    /// Get locations by date range
    /// </summary>
    [HttpGet("user/{userId:guid}/history/range")]
    public async Task<IActionResult> GetLocationsByDateRange(Guid userId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var locations = await locationService.GetLocationsByDateRangeAsync(userId, startDate, endDate);
            return Ok(locations);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting locations by date range for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve locations" });
        }
    }

    /// <summary>
    /// Delete a specific location
    /// </summary>
    [HttpDelete("{locationId:guid}")]
    public async Task<IActionResult> DeleteLocation(Guid locationId)
    {
        await locationService.DeleteLocationAsync(locationId);
        return NoContent();
    }

    /// <summary>
    /// Delete all location history for a user
    /// </summary>
    [HttpDelete("user/{userId:guid}/history")]
    public async Task<IActionResult> DeleteLocationHistory(Guid userId)
    {
        await locationService.DeleteLocationHistoryAsync(userId);
        return NoContent();
    }

    /// <summary>
    /// Get user's saved locations
    /// </summary>
    [HttpGet("user/{userId:guid}/saved")]
    public async Task<IActionResult> GetSavedLocations(Guid userId)
    {
        var locations = await locationService.GetSavedLocationsAsync(userId);
        return Ok(locations);
    }

    /// <summary>
    /// Get saved location by ID
    /// </summary>
    [HttpGet("saved/{locationId:guid}")]
    public async Task<IActionResult> GetSavedLocationById(Guid locationId)
    {
        var location = await locationService.GetSavedLocationByIdAsync(locationId);
        return location != null
            ? Ok(location)
            : NotFound(new { error = $"Saved location {locationId} not found" });
    }

    /// <summary>
    /// Get user's default location
    /// </summary>
    [HttpGet("user/{userId:guid}/saved/default")]
    public async Task<IActionResult> GetDefaultLocation(Guid userId)
    {
        var location = await locationService.GetDefaultLocationAsync(userId);
        return location != null
            ? Ok(location)
            : NotFound(new { error = "No default location set" });
    }

    /// <summary>
    /// Create a saved location
    /// </summary>
    [HttpPost("saved")]
    public async Task<IActionResult> CreateSavedLocation([FromBody] CreateSavedLocationDto dto)
    {
        try
        {
            var location = await locationService.CreateSavedLocationAsync(dto);
            return Created($"/api/location/saved/{location.Id}", location);
        }
        catch (InvalidCoordinatesException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (SavedLocationAlreadyExistsException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating saved location for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Failed to create saved location" });
        }
    }

    /// <summary>
    /// Update a saved location
    /// </summary>
    [HttpPut("saved/{locationId:guid}")]
    public async Task<IActionResult> UpdateSavedLocation(Guid locationId, [FromBody] UpdateSavedLocationDto dto)
    {
        try
        {
            var location = await locationService.UpdateSavedLocationAsync(locationId, dto);
            return Ok(location);
        }
        catch (SavedLocationNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidCoordinatesException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating saved location {LocationId}", locationId);
            return StatusCode(500, new { error = "Failed to update saved location" });
        }
    }

    /// <summary>
    /// Set default location
    /// </summary>
    [HttpPost("user/{userId:guid}/saved/{locationId:guid}/set-default")]
    public async Task<IActionResult> SetDefaultLocation(Guid userId, Guid locationId)
    {
        try
        {
            await locationService.SetDefaultLocationAsync(userId, locationId);
            return Ok(new { message = "Default location set" });
        }
        catch (SavedLocationNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a saved location
    /// </summary>
    [HttpDelete("saved/{locationId:guid}")]
    public async Task<IActionResult> DeleteSavedLocation(Guid locationId)
    {
        await locationService.DeleteSavedLocationAsync(locationId);
        return NoContent();
    }

    /// <summary>
    /// Get user's location preferences
    /// </summary>
    [HttpGet("user/{userId:guid}/preferences")]
    public async Task<IActionResult> GetLocationPreferences(Guid userId)
    {
        var prefs = await locationService.GetLocationPreferencesAsync(userId);
        return Ok(prefs);
    }

    /// <summary>
    /// Update location preferences
    /// </summary>
    [HttpPut("user/{userId:guid}/preferences")]
    public async Task<IActionResult> UpdateLocationPreferences(Guid userId, [FromBody] UpdateLocationPreferencesDto dto)
    {
        try
        {
            var prefs = await locationService.UpdateLocationPreferencesAsync(userId, dto);
            return Ok(prefs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating location preferences for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to update preferences" });
        }
    }

    /// <summary>
    /// Get active geofences
    /// </summary>
    [HttpGet("geofences")]
    public async Task<IActionResult> GetActiveGeofences()
    {
        var geofences = await locationService.GetActiveGeofencesAsync();
        return Ok(geofences);
    }

    /// <summary>
    /// Get geofence by ID
    /// </summary>
    [HttpGet("geofences/{geofenceId:guid}")]
    public async Task<IActionResult> GetGeofenceById(Guid geofenceId)
    {
        var geofence = await locationService.GetGeofenceByIdAsync(geofenceId);
        return geofence != null
            ? Ok(geofence)
            : NotFound(new { error = $"Geofence {geofenceId} not found" });
    }

    /// <summary>
    /// Create a geofence (Admin)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpPost("geofences")]
    public async Task<IActionResult> CreateGeofence([FromBody] CreateGeofenceDto dto)
    {
        try
        {
            var geofence = await locationService.CreateGeofenceAsync(dto);
            return Created($"/api/location/geofences/{geofence.Id}", geofence);
        }
        catch (InvalidCoordinatesException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating geofence {Name}", dto.Name);
            return StatusCode(500, new { error = "Failed to create geofence" });
        }
    }

    /// <summary>
    /// Update a geofence (Admin)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpPut("geofences/{geofenceId:guid}")]
    public async Task<IActionResult> UpdateGeofence(Guid geofenceId, [FromBody] UpdateGeofenceDto dto)
    {
        try
        {
            var geofence = await locationService.UpdateGeofenceAsync(geofenceId, dto);
            return Ok(geofence);
        }
        catch (GeofenceNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidCoordinatesException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating geofence {GeofenceId}", geofenceId);
            return StatusCode(500, new { error = "Failed to update geofence" });
        }
    }

    /// <summary>
    /// Delete a geofence (Admin)
    /// </summary>
    [Authorize(Roles = "support_user")]
    [HttpDelete("geofences/{geofenceId:guid}")]
    public async Task<IActionResult> DeleteGeofence(Guid geofenceId)
    {
        await locationService.DeleteGeofenceAsync(geofenceId);
        return NoContent();
    }

    /// <summary>
    /// Check if user is inside any geofences
    /// </summary>
    [HttpPost("geofences/check")]
    public async Task<IActionResult> CheckGeofences([FromBody] CheckGeofenceDto dto)
    {
        try
        {
            var result = await locationService.CheckGeofencesAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking geofences for user {UserId}", dto.UserId);
            return StatusCode(500, new { error = "Failed to check geofences" });
        }
    }

    /// <summary>
    /// Get user's geofence events
    /// </summary>
    [HttpGet("user/{userId:guid}/geofence-events")]
    public async Task<IActionResult> GetUserGeofenceEvents(Guid userId, [FromQuery] int limit = 100)
    {
        var events = await locationService.GetUserGeofenceEventsAsync(userId, limit);
        return Ok(events);
    }

    /// <summary>
    /// Find nearby users
    /// </summary>
    [HttpPost("nearby")]
    public async Task<IActionResult> FindNearbyUsers([FromBody] SearchNearbyDto dto)
    {
        try
        {
            var users = await locationService.FindNearbyUsersAsync(dto);
            return Ok(users);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finding nearby users");
            return StatusCode(500, new { error = "Failed to find nearby users" });
        }
    }

    /// <summary>
    /// Cleanup old locations for user
    /// </summary>
    [HttpPost("user/{userId:guid}/cleanup")]
    public async Task<IActionResult> CleanupOldLocations(Guid userId)
    {
        await locationService.CleanupOldLocationsAsync(userId);
        return Ok(new { message = "Old locations cleaned up" });
    }
}
