using UserService.Application.DTOs.Location;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class LocationService(
    IUserLocationRepository locationRepository,
    IUserSavedLocationRepository savedLocationRepository,
    IUserLocationPreferencesRepository preferencesRepository,
    IGeofenceRepository geofenceRepository,
    IUserGeofenceEventRepository geofenceEventRepository,
    IUserRepository userRepository
) : ILocationService
{
    public async Task<UserLocationDto> RecordLocationAsync(RecordLocationDto dto)
    {
        // Validate coordinates
        if (dto.Latitude < -90 || dto.Latitude > 90 || dto.Longitude < -180 || dto.Longitude > 180)
            throw new InvalidCoordinatesException(dto.Latitude, dto.Longitude);

        // Check if location history is enabled
        var prefs = await preferencesRepository.GetByUserIdAsync(dto.UserId);
        if (prefs != null && !prefs.LocationHistoryEnabled)
            throw new LocationHistoryDisabledException(dto.UserId);

        var location = new UserLocation(
            dto.UserId,
            dto.Latitude,
            dto.Longitude,
            dto.Source,
            dto.Accuracy,
            dto.Altitude,
            dto.AltitudeAccuracy,
            dto.Heading,
            dto.Speed
        );

        await locationRepository.AddAsync(location);

        // Check geofences
        await ProcessGeofences(dto.UserId, dto.Latitude, dto.Longitude, location.Id);

        return MapToLocationDto(location);
    }

    public async Task<UserLocationDto?> GetLatestLocationAsync(Guid userId)
    {
        var location = await locationRepository.GetLatestByUserIdAsync(userId);
        return location != null ? MapToLocationDto(location) : null;
    }

    public async Task<LocationHistoryDto> GetLocationHistoryAsync(Guid userId, int limit = 100, int offset = 0)
    {
        var locations = await locationRepository.GetByUserIdAsync(userId, limit, offset);
        var locationList = locations.ToList();

        return new LocationHistoryDto(
            userId,
            locationList.Select(MapToLocationDto),
            locationList.Count,
            locationList.LastOrDefault()?.RecordedAt,
            locationList.FirstOrDefault()?.RecordedAt
        );
    }

    public async Task<IEnumerable<UserLocationDto>> GetLocationsByDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        var locations = await locationRepository.GetByUserIdAndDateRangeAsync(userId, startDate, endDate);
        return locations.Select(MapToLocationDto);
    }

    public async Task DeleteLocationAsync(Guid locationId)
    {
        await locationRepository.DeleteAsync(locationId);
    }

    public async Task DeleteLocationHistoryAsync(Guid userId)
    {
        await locationRepository.DeleteAllByUserIdAsync(userId);
    }

    public async Task<IEnumerable<UserSavedLocationDto>> GetSavedLocationsAsync(Guid userId)
    {
        var locations = await savedLocationRepository.GetActiveByUserIdAsync(userId);
        return locations.Select(MapToSavedLocationDto);
    }

    public async Task<UserSavedLocationDto?> GetSavedLocationByIdAsync(Guid locationId)
    {
        var location = await savedLocationRepository.GetByIdAsync(locationId);
        return location != null ? MapToSavedLocationDto(location) : null;
    }

    public async Task<UserSavedLocationDto?> GetDefaultLocationAsync(Guid userId)
    {
        var location = await savedLocationRepository.GetDefaultByUserIdAsync(userId);
        return location != null ? MapToSavedLocationDto(location) : null;
    }

    public async Task<UserSavedLocationDto> CreateSavedLocationAsync(CreateSavedLocationDto dto)
    {
        // Validate coordinates
        if (dto.Latitude < -90 || dto.Latitude > 90 || dto.Longitude < -180 || dto.Longitude > 180)
            throw new InvalidCoordinatesException(dto.Latitude, dto.Longitude);

        // Check if name already exists
        var existing = await savedLocationRepository.GetByUserIdAndNameAsync(dto.UserId, dto.Name);
        if (existing != null)
            throw new SavedLocationAlreadyExistsException(dto.UserId, dto.Name);

        // Clear existing default if this is default
        if (dto.IsDefault)
            await savedLocationRepository.ClearDefaultForUserAsync(dto.UserId);

        var location = new UserSavedLocation(
            dto.UserId,
            dto.Name,
            dto.Latitude,
            dto.Longitude,
            dto.Label,
            dto.Address,
            dto.City,
            dto.State,
            dto.Country,
            dto.CountryCode,
            dto.PostalCode,
            dto.IsDefault
        );

        await savedLocationRepository.AddAsync(location);
        return MapToSavedLocationDto(location);
    }

    public async Task<UserSavedLocationDto> UpdateSavedLocationAsync(Guid locationId, UpdateSavedLocationDto dto)
    {
        var location = await savedLocationRepository.GetByIdAsync(locationId);
        if (location == null)
            throw new SavedLocationNotFoundException(locationId);

        // Validate new coordinates if provided
        if (dto.Latitude.HasValue || dto.Longitude.HasValue)
        {
            var lat = dto.Latitude ?? location.Latitude;
            var lon = dto.Longitude ?? location.Longitude;
            if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
                throw new InvalidCoordinatesException(lat, lon);
        }

        location.Update(
            dto.Name,
            dto.Label,
            dto.Latitude,
            dto.Longitude,
            dto.Address,
            dto.City,
            dto.State,
            dto.Country,
            dto.CountryCode,
            dto.PostalCode
        );

        await savedLocationRepository.UpdateAsync(location);
        return MapToSavedLocationDto(location);
    }

    public async Task SetDefaultLocationAsync(Guid userId, Guid locationId)
    {
        var location = await savedLocationRepository.GetByIdAsync(locationId);
        if (location == null || location.UserId != userId)
            throw new SavedLocationNotFoundException(locationId);

        await savedLocationRepository.ClearDefaultForUserAsync(userId);
        location.SetAsDefault();
        await savedLocationRepository.UpdateAsync(location);
    }

    public async Task DeleteSavedLocationAsync(Guid locationId)
    {
        await savedLocationRepository.DeleteAsync(locationId);
    }

    public async Task<UserLocationPreferencesDto> GetLocationPreferencesAsync(Guid userId)
    {
        var prefs = await preferencesRepository.GetByUserIdAsync(userId);
        if (prefs == null)
        {
            prefs = new UserLocationPreferences(userId);
            await preferencesRepository.AddAsync(prefs);
        }
        return MapToPreferencesDto(prefs);
    }

    public async Task<UserLocationPreferencesDto> UpdateLocationPreferencesAsync(Guid userId, UpdateLocationPreferencesDto dto)
    {
        var prefs = await preferencesRepository.GetByUserIdAsync(userId);
        if (prefs == null)
        {
            prefs = new UserLocationPreferences(userId);
            await preferencesRepository.AddAsync(prefs);
        }

        prefs.UpdatePreferences(
            dto.LocationSharingEnabled,
            dto.ShareWithBusinesses,
            dto.SharePreciseLocation,
            dto.LocationHistoryEnabled,
            dto.MaxHistoryDays,
            dto.AutoDetectTimezone,
            dto.DefaultSearchRadiusKm
        );

        await preferencesRepository.UpdateAsync(prefs);
        return MapToPreferencesDto(prefs);
    }

    public async Task<IEnumerable<GeofenceDto>> GetActiveGeofencesAsync()
    {
        var geofences = await geofenceRepository.GetActiveAsync();
        return geofences.Select(MapToGeofenceDto);
    }

    public async Task<GeofenceDto?> GetGeofenceByIdAsync(Guid geofenceId)
    {
        var geofence = await geofenceRepository.GetByIdAsync(geofenceId);
        return geofence != null ? MapToGeofenceDto(geofence) : null;
    }

    public async Task<GeofenceDto> CreateGeofenceAsync(CreateGeofenceDto dto)
    {
        if (dto.Latitude < -90 || dto.Latitude > 90 || dto.Longitude < -180 || dto.Longitude > 180)
            throw new InvalidCoordinatesException(dto.Latitude, dto.Longitude);

        var geofence = new Geofence(
            dto.Name,
            dto.Latitude,
            dto.Longitude,
            dto.RadiusMeters,
            dto.Description,
            dto.TriggerOnEnter,
            dto.TriggerOnExit,
            dto.TriggerOnDwell,
            dto.DwellTimeSeconds
        );

        await geofenceRepository.AddAsync(geofence);
        return MapToGeofenceDto(geofence);
    }

    public async Task<GeofenceDto> UpdateGeofenceAsync(Guid geofenceId, UpdateGeofenceDto dto)
    {
        var geofence = await geofenceRepository.GetByIdAsync(geofenceId);
        if (geofence == null)
            throw new GeofenceNotFoundException(geofenceId);

        if (dto.Latitude.HasValue || dto.Longitude.HasValue)
        {
            var lat = dto.Latitude ?? geofence.Latitude;
            var lon = dto.Longitude ?? geofence.Longitude;
            if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
                throw new InvalidCoordinatesException(lat, lon);
        }

        geofence.Update(
            dto.Name,
            dto.Description,
            dto.Latitude,
            dto.Longitude,
            dto.RadiusMeters,
            dto.TriggerOnEnter,
            dto.TriggerOnExit,
            dto.TriggerOnDwell,
            dto.DwellTimeSeconds
        );

        await geofenceRepository.UpdateAsync(geofence);
        return MapToGeofenceDto(geofence);
    }

    public async Task DeleteGeofenceAsync(Guid geofenceId)
    {
        await geofenceRepository.DeleteAsync(geofenceId);
    }

    public async Task<GeofenceCheckResultDto> CheckGeofencesAsync(CheckGeofenceDto dto)
    {
        var containingGeofences = await geofenceRepository.GetContainingPointAsync(dto.Latitude, dto.Longitude);
        var geofenceList = containingGeofences.ToList();
        var triggeredEvents = new List<UserGeofenceEventDto>();

        foreach (var geofence in geofenceList)
        {
            var lastEvent = await geofenceEventRepository.GetLatestByUserAndGeofenceAsync(dto.UserId, geofence.Id);
            var wasInside = lastEvent?.EventType == "enter" || lastEvent?.EventType == "dwell";
            var isInside = geofence.IsPointInside(dto.Latitude, dto.Longitude);

            if (isInside && !wasInside && geofence.TriggerOnEnter)
            {
                var enterEvent = UserGeofenceEvent.CreateEnterEvent(dto.UserId, geofence.Id);
                await geofenceEventRepository.AddAsync(enterEvent);
                triggeredEvents.Add(MapToEventDto(enterEvent, geofence.Name));
            }
        }

        return new GeofenceCheckResultDto(
            geofenceList.Any(),
            geofenceList.Select(MapToGeofenceDto),
            triggeredEvents
        );
    }

    public async Task<IEnumerable<UserGeofenceEventDto>> GetUserGeofenceEventsAsync(Guid userId, int limit = 100)
    {
        var events = await geofenceEventRepository.GetByUserIdAsync(userId, limit);
        var result = new List<UserGeofenceEventDto>();

        foreach (var evt in events)
        {
            var geofence = await geofenceRepository.GetByIdAsync(evt.GeofenceId);
            result.Add(MapToEventDto(evt, geofence?.Name ?? "Unknown"));
        }

        return result;
    }

    public async Task<IEnumerable<NearbyUserDto>> FindNearbyUsersAsync(SearchNearbyDto dto)
    {
        var nearbyLocations = await locationRepository.GetNearbyUsersAsync(dto.Latitude, dto.Longitude, dto.RadiusKm, dto.Limit);
        var result = new List<NearbyUserDto>();

        foreach (var loc in nearbyLocations)
        {
            var user = await userRepository.GetByIdAsync(loc.UserId);
            var prefs = await preferencesRepository.GetByUserIdAsync(loc.UserId);

            // Only include if user allows sharing
            if (prefs?.LocationSharingEnabled == true)
            {
                result.Add(new NearbyUserDto(
                    loc.UserId,
                    user?.Username,
                    (decimal)loc.DistanceToKm(dto.Latitude, dto.Longitude),
                    prefs.SharePreciseLocation ? loc.City : null,
                    loc.Country,
                    loc.RecordedAt
                ));
            }
        }

        return result;
    }

    public async Task CleanupOldLocationsAsync(Guid userId)
    {
        var prefs = await preferencesRepository.GetByUserIdAsync(userId);
        var daysToKeep = prefs?.MaxHistoryDays ?? 90;
        await locationRepository.DeleteOldLocationsAsync(userId, daysToKeep);
    }

    public async Task CleanupOldGeofenceEventsAsync(int daysToKeep = 30)
    {
        await geofenceEventRepository.DeleteOldEventsAsync(daysToKeep);
    }

    private async Task ProcessGeofences(Guid userId, decimal latitude, decimal longitude, Guid locationId)
    {
        var geofences = await geofenceRepository.GetActiveAsync();

        foreach (var geofence in geofences)
        {
            var isInside = geofence.IsPointInside(latitude, longitude);
            var lastEvent = await geofenceEventRepository.GetLatestByUserAndGeofenceAsync(userId, geofence.Id);
            var wasInside = lastEvent?.EventType == "enter" || lastEvent?.EventType == "dwell";

            if (isInside && !wasInside && geofence.TriggerOnEnter)
            {
                var enterEvent = UserGeofenceEvent.CreateEnterEvent(userId, geofence.Id, locationId);
                await geofenceEventRepository.AddAsync(enterEvent);
            }
            else if (!isInside && wasInside && geofence.TriggerOnExit)
            {
                var exitEvent = UserGeofenceEvent.CreateExitEvent(userId, geofence.Id, locationId);
                await geofenceEventRepository.AddAsync(exitEvent);
            }
        }
    }

    private static UserLocationDto MapToLocationDto(UserLocation l) => new(
        l.Id, l.UserId, l.Latitude, l.Longitude, l.Accuracy, l.Altitude, l.Heading, l.Speed, l.Source,
        l.Address, l.City, l.State, l.Country, l.CountryCode, l.PostalCode, l.Timezone, l.RecordedAt
    );

    private static UserSavedLocationDto MapToSavedLocationDto(UserSavedLocation l) => new(
        l.Id, l.UserId, l.Name, l.Label, l.Latitude, l.Longitude, l.Address, l.City, l.State,
        l.Country, l.CountryCode, l.PostalCode, l.IsDefault, l.IsActive
    );

    private static UserLocationPreferencesDto MapToPreferencesDto(UserLocationPreferences p) => new(
        p.UserId, p.LocationSharingEnabled, p.ShareWithBusinesses, p.SharePreciseLocation,
        p.LocationHistoryEnabled, p.MaxHistoryDays, p.AutoDetectTimezone, p.DefaultSearchRadiusKm
    );

    private static GeofenceDto MapToGeofenceDto(Geofence g) => new(
        g.Id, g.Name, g.Description, g.Latitude, g.Longitude, g.RadiusMeters, g.GeofenceType,
        g.TriggerOnEnter, g.TriggerOnExit, g.TriggerOnDwell, g.DwellTimeSeconds, g.IsActive
    );

    private static UserGeofenceEventDto MapToEventDto(UserGeofenceEvent e, string geofenceName) => new(
        e.Id, e.UserId, e.GeofenceId, geofenceName, e.EventType, e.LocationId, e.TriggeredAt
    );
}
