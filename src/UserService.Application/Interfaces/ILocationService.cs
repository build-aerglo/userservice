using UserService.Application.DTOs.Location;

namespace UserService.Application.Interfaces;

public interface ILocationService
{
    // User locations
    Task<UserLocationDto> RecordLocationAsync(RecordLocationDto dto);
    Task<UserLocationDto?> GetLatestLocationAsync(Guid userId);
    Task<LocationHistoryDto> GetLocationHistoryAsync(Guid userId, int limit = 100, int offset = 0);
    Task<IEnumerable<UserLocationDto>> GetLocationsByDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate);
    Task DeleteLocationAsync(Guid locationId);
    Task DeleteLocationHistoryAsync(Guid userId);

    // Saved locations
    Task<IEnumerable<UserSavedLocationDto>> GetSavedLocationsAsync(Guid userId);
    Task<UserSavedLocationDto?> GetSavedLocationByIdAsync(Guid locationId);
    Task<UserSavedLocationDto?> GetDefaultLocationAsync(Guid userId);
    Task<UserSavedLocationDto> CreateSavedLocationAsync(CreateSavedLocationDto dto);
    Task<UserSavedLocationDto> UpdateSavedLocationAsync(Guid locationId, UpdateSavedLocationDto dto);
    Task SetDefaultLocationAsync(Guid userId, Guid locationId);
    Task DeleteSavedLocationAsync(Guid locationId);

    // Preferences
    Task<UserLocationPreferencesDto> GetLocationPreferencesAsync(Guid userId);
    Task<UserLocationPreferencesDto> UpdateLocationPreferencesAsync(Guid userId, UpdateLocationPreferencesDto dto);

    // Geofences
    Task<IEnumerable<GeofenceDto>> GetActiveGeofencesAsync();
    Task<GeofenceDto?> GetGeofenceByIdAsync(Guid geofenceId);
    Task<GeofenceDto> CreateGeofenceAsync(CreateGeofenceDto dto);
    Task<GeofenceDto> UpdateGeofenceAsync(Guid geofenceId, UpdateGeofenceDto dto);
    Task DeleteGeofenceAsync(Guid geofenceId);
    Task<GeofenceCheckResultDto> CheckGeofencesAsync(CheckGeofenceDto dto);

    // Geofence events
    Task<IEnumerable<UserGeofenceEventDto>> GetUserGeofenceEventsAsync(Guid userId, int limit = 100);

    // Nearby search
    Task<IEnumerable<NearbyUserDto>> FindNearbyUsersAsync(SearchNearbyDto dto);

    // Cleanup
    Task CleanupOldLocationsAsync(Guid userId);
    Task CleanupOldGeofenceEventsAsync(int daysToKeep = 30);
}
