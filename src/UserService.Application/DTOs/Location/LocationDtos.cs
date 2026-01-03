namespace UserService.Application.DTOs.Location;

// Response DTOs
public record UserLocationDto(
    Guid Id,
    Guid UserId,
    decimal Latitude,
    decimal Longitude,
    decimal? Accuracy,
    decimal? Altitude,
    decimal? Heading,
    decimal? Speed,
    string Source,
    string? Address,
    string? City,
    string? State,
    string? Country,
    string? CountryCode,
    string? PostalCode,
    string? Timezone,
    DateTime RecordedAt
);

public record UserSavedLocationDto(
    Guid Id,
    Guid UserId,
    string Name,
    string? Label,
    decimal Latitude,
    decimal Longitude,
    string? Address,
    string? City,
    string? State,
    string? Country,
    string? CountryCode,
    string? PostalCode,
    bool IsDefault,
    bool IsActive
);

public record UserLocationPreferencesDto(
    Guid UserId,
    bool LocationSharingEnabled,
    bool ShareWithBusinesses,
    bool SharePreciseLocation,
    bool LocationHistoryEnabled,
    int MaxHistoryDays,
    bool AutoDetectTimezone,
    decimal DefaultSearchRadiusKm
);

public record GeofenceDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Latitude,
    decimal Longitude,
    decimal RadiusMeters,
    string GeofenceType,
    bool TriggerOnEnter,
    bool TriggerOnExit,
    bool TriggerOnDwell,
    int DwellTimeSeconds,
    bool IsActive
);

public record UserGeofenceEventDto(
    Guid Id,
    Guid UserId,
    Guid GeofenceId,
    string GeofenceName,
    string EventType,
    Guid? LocationId,
    DateTime TriggeredAt
);

public record NearbyUserDto(
    Guid UserId,
    string? Username,
    decimal DistanceKm,
    string? City,
    string? Country,
    DateTime LastSeenAt
);

public record LocationHistoryDto(
    Guid UserId,
    IEnumerable<UserLocationDto> Locations,
    int TotalCount,
    DateTime? OldestRecord,
    DateTime? NewestRecord
);

// Request DTOs
public record RecordLocationDto(
    Guid UserId,
    decimal Latitude,
    decimal Longitude,
    string Source = "gps",
    decimal? Accuracy = null,
    decimal? Altitude = null,
    decimal? AltitudeAccuracy = null,
    decimal? Heading = null,
    decimal? Speed = null
);

public record UpdateLocationAddressDto(
    Guid LocationId,
    string? Address,
    string? City,
    string? State,
    string? Country,
    string? CountryCode,
    string? PostalCode,
    string? Timezone
);

public record CreateSavedLocationDto(
    Guid UserId,
    string Name,
    decimal Latitude,
    decimal Longitude,
    string? Label = null,
    string? Address = null,
    string? City = null,
    string? State = null,
    string? Country = null,
    string? CountryCode = null,
    string? PostalCode = null,
    bool IsDefault = false
);

public record UpdateSavedLocationDto(
    string? Name,
    string? Label,
    decimal? Latitude,
    decimal? Longitude,
    string? Address,
    string? City,
    string? State,
    string? Country,
    string? CountryCode,
    string? PostalCode
);

public record UpdateLocationPreferencesDto(
    bool? LocationSharingEnabled,
    bool? ShareWithBusinesses,
    bool? SharePreciseLocation,
    bool? LocationHistoryEnabled,
    int? MaxHistoryDays,
    bool? AutoDetectTimezone,
    decimal? DefaultSearchRadiusKm
);

public record CreateGeofenceDto(
    string Name,
    decimal Latitude,
    decimal Longitude,
    decimal RadiusMeters,
    string? Description = null,
    bool TriggerOnEnter = true,
    bool TriggerOnExit = false,
    bool TriggerOnDwell = false,
    int DwellTimeSeconds = 300
);

public record UpdateGeofenceDto(
    string? Name,
    string? Description,
    decimal? Latitude,
    decimal? Longitude,
    decimal? RadiusMeters,
    bool? TriggerOnEnter,
    bool? TriggerOnExit,
    bool? TriggerOnDwell,
    int? DwellTimeSeconds
);

public record SearchNearbyDto(
    decimal Latitude,
    decimal Longitude,
    decimal RadiusKm = 25,
    int Limit = 50
);

public record CheckGeofenceDto(
    Guid UserId,
    decimal Latitude,
    decimal Longitude
);

public record GeofenceCheckResultDto(
    bool IsInsideAnyGeofence,
    IEnumerable<GeofenceDto> ContainingGeofences,
    IEnumerable<UserGeofenceEventDto> TriggeredEvents
);
