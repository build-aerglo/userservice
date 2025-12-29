namespace UserService.Domain.Exceptions;

public class GeolocationNotFoundException(Guid userId)
    : Exception($"Geolocation record for user '{userId}' was not found.");

public class InvalidCoordinatesException(double latitude, double longitude)
    : Exception($"Invalid coordinates: latitude {latitude}, longitude {longitude}. Latitude must be between -90 and 90, longitude between -180 and 180.");

public class GeolocationDisabledException(Guid userId)
    : Exception($"Geolocation is disabled for user '{userId}'.");

public class VpnDetectedException(Guid userId)
    : Exception($"VPN usage detected for user '{userId}'.");

public class LocationMismatchException(string userLocation, string businessLocation)
    : Exception($"User location '{userLocation}' does not match business location '{businessLocation}'.");

public class GeolocationUpdateFailedException(string message)
    : Exception(message);
