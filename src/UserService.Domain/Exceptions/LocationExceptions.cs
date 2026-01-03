namespace UserService.Domain.Exceptions;

public class LocationNotFoundException : Exception
{
    public LocationNotFoundException(Guid locationId)
        : base($"Location with ID '{locationId}' not found.") { }
}

public class SavedLocationNotFoundException : Exception
{
    public SavedLocationNotFoundException(Guid locationId)
        : base($"Saved location with ID '{locationId}' not found.") { }

    public SavedLocationNotFoundException(Guid userId, string name)
        : base($"Saved location '{name}' for user '{userId}' not found.") { }
}

public class SavedLocationAlreadyExistsException : Exception
{
    public SavedLocationAlreadyExistsException(Guid userId, string name)
        : base($"User '{userId}' already has a saved location named '{name}'.") { }
}

public class GeofenceNotFoundException : Exception
{
    public GeofenceNotFoundException(Guid geofenceId)
        : base($"Geofence with ID '{geofenceId}' not found.") { }
}

public class InvalidCoordinatesException : Exception
{
    public InvalidCoordinatesException(decimal latitude, decimal longitude)
        : base($"Invalid coordinates: latitude {latitude}, longitude {longitude}. Latitude must be between -90 and 90, longitude between -180 and 180.") { }
}

public class LocationSharingDisabledException : Exception
{
    public LocationSharingDisabledException(Guid userId)
        : base($"Location sharing is disabled for user '{userId}'.") { }
}

public class LocationHistoryDisabledException : Exception
{
    public LocationHistoryDisabledException(Guid userId)
        : base($"Location history is disabled for user '{userId}'.") { }
}
