using UserService.Application.DTOs.Geolocation;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class GeolocationService(
    IUserGeolocationRepository geolocationRepository,
    IGeolocationHistoryRepository historyRepository,
    IUserRepository userRepository
) : IGeolocationService
{
    // Earth radius in kilometers for Haversine formula
    private const double EarthRadiusKm = 6371.0;

    // Maximum acceptable distance (km) for review validation
    private const double MaxReviewDistanceKm = 100.0;

    // Default history retention period
    private const int DefaultRetentionDays = 90;

    public async Task<UserGeolocationDto?> GetUserGeolocationAsync(Guid userId)
    {
        var geolocation = await geolocationRepository.GetByUserIdAsync(userId);
        return geolocation is null ? null : MapToDto(geolocation);
    }

    public async Task<UserGeolocationDto> UpdateGeolocationAsync(UpdateGeolocationDto dto)
    {
        // Validate coordinates
        if (!ValidateCoordinates(dto.Latitude, dto.Longitude))
            throw new InvalidCoordinatesException(dto.Latitude, dto.Longitude);

        // Validate user exists
        var user = await userRepository.GetByIdAsync(dto.UserId);
        if (user is null)
            throw new EndUserNotFoundException(dto.UserId);

        var geolocation = await geolocationRepository.GetByUserIdAsync(dto.UserId);

        if (geolocation is null)
        {
            // Create new geolocation record
            geolocation = new UserGeolocation(
                userId: dto.UserId,
                latitude: dto.Latitude,
                longitude: dto.Longitude,
                state: dto.State,
                lga: dto.Lga,
                city: dto.City
            );
            await geolocationRepository.AddAsync(geolocation);
        }
        else
        {
            // Update existing
            geolocation.UpdateLocation(
                latitude: dto.Latitude,
                longitude: dto.Longitude,
                state: dto.State,
                lga: dto.Lga,
                city: dto.City
            );
            await geolocationRepository.UpdateAsync(geolocation);
        }

        var savedGeolocation = await geolocationRepository.GetByUserIdAsync(dto.UserId);
        if (savedGeolocation is null)
            throw new GeolocationUpdateFailedException("Failed to save geolocation.");

        return MapToDto(savedGeolocation);
    }

    public async Task<GeolocationHistoryDto> RecordGeolocationHistoryAsync(RecordGeolocationHistoryDto dto)
    {
        // Validate coordinates
        if (!ValidateCoordinates(dto.Latitude, dto.Longitude))
            throw new InvalidCoordinatesException(dto.Latitude, dto.Longitude);

        var history = new GeolocationHistory(
            userId: dto.UserId,
            latitude: dto.Latitude,
            longitude: dto.Longitude,
            state: dto.State,
            lga: dto.Lga,
            city: dto.City,
            source: dto.Source,
            vpnDetected: dto.VpnDetected
        );

        await historyRepository.AddAsync(history);

        // Also update current geolocation if enabled
        var currentGeolocation = await geolocationRepository.GetByUserIdAsync(dto.UserId);
        if (currentGeolocation?.IsEnabled == true)
        {
            currentGeolocation.UpdateLocation(
                latitude: dto.Latitude,
                longitude: dto.Longitude,
                state: dto.State,
                lga: dto.Lga,
                city: dto.City
            );
            await geolocationRepository.UpdateAsync(currentGeolocation);
        }

        return MapToHistoryDto(history);
    }

    public async Task<GeolocationHistoryResponseDto> GetGeolocationHistoryAsync(Guid userId, int limit = 50, int offset = 0)
    {
        var history = await historyRepository.GetByUserIdAsync(userId, limit, offset);
        var historyDtos = history.Select(MapToHistoryDto).ToList();

        var vpnCount = await historyRepository.GetVpnDetectionCountAsync(userId);

        return new GeolocationHistoryResponseDto(
            UserId: userId,
            History: historyDtos,
            TotalCount: historyDtos.Count,
            VpnDetectionCount: vpnCount
        );
    }

    public async Task<UserGeolocationDto> ToggleGeolocationAsync(ToggleGeolocationDto dto)
    {
        var geolocation = await geolocationRepository.GetByUserIdAsync(dto.UserId);
        if (geolocation is null)
        {
            // Initialize with default coordinates (Nigeria center)
            await InitializeUserGeolocationAsync(dto.UserId);
            geolocation = await geolocationRepository.GetByUserIdAsync(dto.UserId);
        }

        if (dto.Enable)
            geolocation!.Enable();
        else
            geolocation!.Disable();

        await geolocationRepository.UpdateAsync(geolocation);

        return MapToDto(geolocation);
    }

    public async Task<LocationValidationDto> ValidateLocationForReviewAsync(ValidateLocationForReviewDto dto)
    {
        // Check if user has geolocation enabled
        var userGeolocation = await geolocationRepository.GetByUserIdAsync(dto.UserId);
        if (userGeolocation is null || !userGeolocation.IsEnabled)
        {
            return new LocationValidationDto(
                IsValid: true, // Allow if geolocation not enabled
                VpnDetected: false,
                Message: "Location tracking not enabled for user",
                DistanceFromBusiness: null
            );
        }

        // Check for VPN usage in recent history
        var recentHistory = await historyRepository.GetLatestByUserIdAsync(dto.UserId);
        if (recentHistory?.VpnDetected == true)
        {
            return new LocationValidationDto(
                IsValid: false,
                VpnDetected: true,
                Message: "VPN usage detected. Please disable VPN to submit reviews.",
                DistanceFromBusiness: null
            );
        }

        // If business has coordinates, calculate distance
        if (dto.BusinessLatitude.HasValue && dto.BusinessLongitude.HasValue)
        {
            var distance = CalculateDistance(
                userGeolocation.Latitude,
                userGeolocation.Longitude,
                dto.BusinessLatitude.Value,
                dto.BusinessLongitude.Value
            );

            if (distance > MaxReviewDistanceKm)
            {
                return new LocationValidationDto(
                    IsValid: false,
                    VpnDetected: false,
                    Message: $"Your location is {distance:F1}km from the business. Reviews must be submitted within {MaxReviewDistanceKm}km.",
                    DistanceFromBusiness: distance
                );
            }

            return new LocationValidationDto(
                IsValid: true,
                VpnDetected: false,
                Message: "Location validated successfully",
                DistanceFromBusiness: distance
            );
        }

        // If business has state but no coordinates, check state match
        if (!string.IsNullOrEmpty(dto.BusinessState) && !string.IsNullOrEmpty(userGeolocation.State))
        {
            var statesMatch = string.Equals(userGeolocation.State, dto.BusinessState, StringComparison.OrdinalIgnoreCase);
            if (!statesMatch)
            {
                return new LocationValidationDto(
                    IsValid: false,
                    VpnDetected: false,
                    Message: $"Your location ({userGeolocation.State}) does not match the business location ({dto.BusinessState}).",
                    DistanceFromBusiness: null
                );
            }
        }

        return new LocationValidationDto(
            IsValid: true,
            VpnDetected: false,
            Message: "Location validated successfully",
            DistanceFromBusiness: null
        );
    }

    public async Task<UsersInLocationDto> GetUsersByStateAsync(string state, int limit = 100, int offset = 0)
    {
        var geolocations = await geolocationRepository.GetByStateAsync(state, limit, offset);
        var userIds = geolocations.Select(g => g.UserId).ToList();

        var totalCount = await geolocationRepository.GetUserCountByStateAsync(state);

        return new UsersInLocationDto(
            State: state,
            Lga: null,
            UserCount: totalCount,
            UserIds: userIds
        );
    }

    public async Task<UsersInLocationDto> GetUsersByLgaAsync(string lga, int limit = 100, int offset = 0)
    {
        var geolocations = await geolocationRepository.GetByLgaAsync(lga, limit, offset);
        var userIds = geolocations.Select(g => g.UserId).ToList();

        // Find the state from the first result
        var state = geolocations.FirstOrDefault()?.State ?? string.Empty;

        return new UsersInLocationDto(
            State: state,
            Lga: lga,
            UserCount: userIds.Count,
            UserIds: userIds
        );
    }

    public async Task<int> GetUserCountByStateAsync(string state)
    {
        return await geolocationRepository.GetUserCountByStateAsync(state);
    }

    public async Task<int> GetVpnDetectionCountAsync(Guid userId)
    {
        return await historyRepository.GetVpnDetectionCountAsync(userId);
    }

    public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula for calculating distance between two points on Earth
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    public bool ValidateCoordinates(double latitude, double longitude)
    {
        return latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;
    }

    public async Task InitializeUserGeolocationAsync(Guid userId)
    {
        var existingGeolocation = await geolocationRepository.GetByUserIdAsync(userId);
        if (existingGeolocation is not null)
            return;

        // Initialize with default coordinates (Nigeria center) and disabled
        var geolocation = new UserGeolocation(
            userId: userId,
            latitude: 9.0820, // Nigeria center latitude
            longitude: 8.6753, // Nigeria center longitude
            state: null,
            lga: null,
            city: null
        );

        // Disable by default for privacy
        geolocation.Disable();

        await geolocationRepository.AddAsync(geolocation);
    }

    public async Task CleanupOldHistoryAsync(int retentionDays = DefaultRetentionDays)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        await historyRepository.DeleteOldHistoryAsync(cutoffDate);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static UserGeolocationDto MapToDto(UserGeolocation geolocation)
    {
        return new UserGeolocationDto(
            UserId: geolocation.UserId,
            Latitude: geolocation.Latitude,
            Longitude: geolocation.Longitude,
            State: geolocation.State,
            Lga: geolocation.Lga,
            City: geolocation.City,
            IsEnabled: geolocation.IsEnabled,
            LastUpdated: geolocation.LastUpdated
        );
    }

    private static GeolocationHistoryDto MapToHistoryDto(GeolocationHistory history)
    {
        return new GeolocationHistoryDto(
            Id: history.Id,
            UserId: history.UserId,
            Latitude: history.Latitude,
            Longitude: history.Longitude,
            State: history.State,
            Lga: history.Lga,
            City: history.City,
            Source: history.Source,
            VpnDetected: history.VpnDetected,
            RecordedAt: history.RecordedAt
        );
    }
}
