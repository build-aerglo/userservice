namespace UserService.Domain.Entities;

/// <summary>
/// Represents a user's geographic location for badges, leaderboards, and review validation.
/// </summary>
public class UserGeolocation
{
    protected UserGeolocation() { }

    public UserGeolocation(
        Guid userId,
        double latitude,
        double longitude,
        string? state = null,
        string? lga = null,
        string? city = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Latitude = latitude;
        Longitude = longitude;
        State = state;
        Lga = lga;
        City = city;
        IsEnabled = true;
        LastUpdated = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    /// <summary>
    /// Nigerian state (e.g., "Lagos", "Abuja FCT", "Rivers")
    /// </summary>
    public string? State { get; private set; }

    /// <summary>
    /// Local Government Area
    /// </summary>
    public string? Lga { get; private set; }

    /// <summary>
    /// City name
    /// </summary>
    public string? City { get; private set; }

    /// <summary>
    /// Whether location tracking is enabled by user
    /// </summary>
    public bool IsEnabled { get; private set; }

    public DateTime LastUpdated { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void UpdateLocation(
        double latitude,
        double longitude,
        string? state = null,
        string? lga = null,
        string? city = null)
    {
        Latitude = latitude;
        Longitude = longitude;
        State = state ?? State;
        Lga = lga ?? Lga;
        City = city ?? City;
        LastUpdated = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a history entry of user location updates.
/// Used for analytics and validation purposes.
/// </summary>
public class GeolocationHistory
{
    protected GeolocationHistory() { }

    public GeolocationHistory(
        Guid userId,
        double latitude,
        double longitude,
        string? state = null,
        string? lga = null,
        string? city = null,
        string? source = null,
        bool? vpnDetected = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Latitude = latitude;
        Longitude = longitude;
        State = state;
        Lga = lga;
        City = city;
        Source = source ?? GeolocationSources.Gps;
        VpnDetected = vpnDetected ?? false;
        RecordedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public string? State { get; private set; }
    public string? Lga { get; private set; }
    public string? City { get; private set; }

    /// <summary>
    /// Source of location data: gps, ip, manual
    /// </summary>
    public string Source { get; private set; } = default!;

    /// <summary>
    /// Whether VPN usage was detected at the time of recording
    /// </summary>
    public bool VpnDetected { get; private set; }

    public DateTime RecordedAt { get; private set; }
}

public static class GeolocationSources
{
    public const string Gps = "gps";
    public const string Ip = "ip";
    public const string Manual = "manual";
}

/// <summary>
/// Common Nigerian states for reference
/// </summary>
public static class NigerianStates
{
    public const string Lagos = "Lagos";
    public const string AbujaFct = "Abuja FCT";
    public const string Rivers = "Rivers";
    public const string Kano = "Kano";
    public const string Oyo = "Oyo";
    public const string Kaduna = "Kaduna";
    public const string Ogun = "Ogun";
    public const string Anambra = "Anambra";
    public const string Edo = "Edo";
    public const string Delta = "Delta";
    // Add more states as needed
}
