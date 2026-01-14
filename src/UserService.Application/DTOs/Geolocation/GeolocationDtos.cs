namespace UserService.Application.DTOs.Geolocation;

// Response DTOs
public record UserGeolocationDto(
    Guid UserId,
    double Latitude,
    double Longitude,
    string? State,
    string? Lga,
    string? City,
    bool IsEnabled,
    DateTime LastUpdated
);

public record GeolocationHistoryDto(
    Guid Id,
    Guid UserId,
    double Latitude,
    double Longitude,
    string? State,
    string? Lga,
    string? City,
    string Source,
    bool VpnDetected,
    DateTime RecordedAt
);

public record GeolocationHistoryResponseDto(
    Guid UserId,
    IEnumerable<GeolocationHistoryDto> History,
    int TotalCount,
    int VpnDetectionCount
);

public record LocationValidationDto(
    bool IsValid,
    bool VpnDetected,
    string? Message,
    double? DistanceFromBusiness
);

public record UsersInLocationDto(
    string State,
    string? Lga,
    int UserCount,
    IEnumerable<Guid> UserIds
);

// Request DTOs
public record UpdateGeolocationDto(
    Guid UserId,
    double Latitude,
    double Longitude,
    string? State = null,
    string? Lga = null,
    string? City = null
);

public record RecordGeolocationHistoryDto(
    Guid UserId,
    double Latitude,
    double Longitude,
    string? State = null,
    string? Lga = null,
    string? City = null,
    string Source = "gps",
    bool VpnDetected = false
);

public record ToggleGeolocationDto(
    Guid UserId,
    bool Enable
);

public record ValidateLocationForReviewDto(
    Guid UserId,
    Guid BusinessId,
    double? BusinessLatitude,
    double? BusinessLongitude,
    string? BusinessState
);
