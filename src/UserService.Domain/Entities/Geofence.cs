namespace UserService.Domain.Entities;

public class Geofence
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public decimal RadiusMeters { get; private set; }
    public string GeofenceType { get; private set; } = "circle";
    public string? PolygonPoints { get; private set; }
    public bool TriggerOnEnter { get; private set; }
    public bool TriggerOnExit { get; private set; }
    public bool TriggerOnDwell { get; private set; }
    public int DwellTimeSeconds { get; private set; }
    public bool IsActive { get; private set; }
    public string Metadata { get; private set; } = "{}";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected Geofence() { }

    public Geofence(
        string name,
        decimal latitude,
        decimal longitude,
        decimal radiusMeters,
        string? description = null,
        bool triggerOnEnter = true,
        bool triggerOnExit = false,
        bool triggerOnDwell = false,
        int dwellTimeSeconds = 300)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Latitude = latitude;
        Longitude = longitude;
        RadiusMeters = radiusMeters;
        GeofenceType = "circle";
        TriggerOnEnter = triggerOnEnter;
        TriggerOnExit = triggerOnExit;
        TriggerOnDwell = triggerOnDwell;
        DwellTimeSeconds = dwellTimeSeconds;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsPointInside(decimal pointLat, decimal pointLon)
    {
        var distance = CalculateDistanceMeters(pointLat, pointLon);
        return distance <= (double)RadiusMeters;
    }

    public double CalculateDistanceMeters(decimal pointLat, decimal pointLon)
    {
        const double earthRadiusMeters = 6371000.0;

        var lat1Rad = ToRadians((double)Latitude);
        var lat2Rad = ToRadians((double)pointLat);
        var deltaLat = ToRadians((double)(pointLat - Latitude));
        var deltaLon = ToRadians((double)(pointLon - Longitude));

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusMeters * c;
    }

    public void Update(
        string? name = null,
        string? description = null,
        decimal? latitude = null,
        decimal? longitude = null,
        decimal? radiusMeters = null,
        bool? triggerOnEnter = null,
        bool? triggerOnExit = null,
        bool? triggerOnDwell = null,
        int? dwellTimeSeconds = null)
    {
        if (!string.IsNullOrEmpty(name)) Name = name;
        if (description != null) Description = description;
        if (latitude.HasValue) Latitude = latitude.Value;
        if (longitude.HasValue) Longitude = longitude.Value;
        if (radiusMeters.HasValue) RadiusMeters = radiusMeters.Value;
        if (triggerOnEnter.HasValue) TriggerOnEnter = triggerOnEnter.Value;
        if (triggerOnExit.HasValue) TriggerOnExit = triggerOnExit.Value;
        if (triggerOnDwell.HasValue) TriggerOnDwell = triggerOnDwell.Value;
        if (dwellTimeSeconds.HasValue) DwellTimeSeconds = dwellTimeSeconds.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
