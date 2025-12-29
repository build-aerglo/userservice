using UserService.Application.DTOs.Geolocation;

namespace UserService.Application.Interfaces;

public interface IGeolocationService
{
    /// <summary>
    /// Get user's current geolocation
    /// </summary>
    Task<UserGeolocationDto?> GetUserGeolocationAsync(Guid userId);

    /// <summary>
    /// Update user's geolocation
    /// </summary>
    Task<UserGeolocationDto> UpdateGeolocationAsync(UpdateGeolocationDto dto);

    /// <summary>
    /// Record geolocation history entry
    /// </summary>
    Task<GeolocationHistoryDto> RecordGeolocationHistoryAsync(RecordGeolocationHistoryDto dto);

    /// <summary>
    /// Get user's geolocation history
    /// </summary>
    Task<GeolocationHistoryResponseDto> GetGeolocationHistoryAsync(Guid userId, int limit = 50, int offset = 0);

    /// <summary>
    /// Enable/disable geolocation tracking for a user
    /// </summary>
    Task<UserGeolocationDto> ToggleGeolocationAsync(ToggleGeolocationDto dto);

    /// <summary>
    /// Validate location for review (compare user location with business location)
    /// </summary>
    Task<LocationValidationDto> ValidateLocationForReviewAsync(ValidateLocationForReviewDto dto);

    /// <summary>
    /// Get users by state
    /// </summary>
    Task<UsersInLocationDto> GetUsersByStateAsync(string state, int limit = 100, int offset = 0);

    /// <summary>
    /// Get users by LGA
    /// </summary>
    Task<UsersInLocationDto> GetUsersByLgaAsync(string lga, int limit = 100, int offset = 0);

    /// <summary>
    /// Get user count by state
    /// </summary>
    Task<int> GetUserCountByStateAsync(string state);

    /// <summary>
    /// Get VPN detection count for a user
    /// </summary>
    Task<int> GetVpnDetectionCountAsync(Guid userId);

    /// <summary>
    /// Calculate distance between two coordinates (in kilometers)
    /// </summary>
    double CalculateDistance(double lat1, double lon1, double lat2, double lon2);

    /// <summary>
    /// Validate coordinates are within valid ranges
    /// </summary>
    bool ValidateCoordinates(double latitude, double longitude);

    /// <summary>
    /// Initialize geolocation record for new user (disabled by default)
    /// </summary>
    Task InitializeUserGeolocationAsync(Guid userId);

    /// <summary>
    /// Clean up old geolocation history (older than specified days)
    /// </summary>
    Task CleanupOldHistoryAsync(int retentionDays = 90);
}
