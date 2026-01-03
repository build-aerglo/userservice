namespace UserService.Domain.Entities;

public class UserLocationPreferences
{
    public Guid UserId { get; private set; }
    public bool LocationSharingEnabled { get; private set; }
    public bool ShareWithBusinesses { get; private set; }
    public bool SharePreciseLocation { get; private set; }
    public bool LocationHistoryEnabled { get; private set; }
    public int MaxHistoryDays { get; private set; }
    public bool AutoDetectTimezone { get; private set; }
    public decimal DefaultSearchRadiusKm { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected UserLocationPreferences() { }

    public UserLocationPreferences(Guid userId)
    {
        UserId = userId;
        LocationSharingEnabled = false;
        ShareWithBusinesses = false;
        SharePreciseLocation = false;
        LocationHistoryEnabled = true;
        MaxHistoryDays = 90;
        AutoDetectTimezone = true;
        DefaultSearchRadiusKm = 25.00m;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePreferences(
        bool? locationSharingEnabled = null,
        bool? shareWithBusinesses = null,
        bool? sharePreciseLocation = null,
        bool? locationHistoryEnabled = null,
        int? maxHistoryDays = null,
        bool? autoDetectTimezone = null,
        decimal? defaultSearchRadiusKm = null)
    {
        if (locationSharingEnabled.HasValue) LocationSharingEnabled = locationSharingEnabled.Value;
        if (shareWithBusinesses.HasValue) ShareWithBusinesses = shareWithBusinesses.Value;
        if (sharePreciseLocation.HasValue) SharePreciseLocation = sharePreciseLocation.Value;
        if (locationHistoryEnabled.HasValue) LocationHistoryEnabled = locationHistoryEnabled.Value;
        if (maxHistoryDays.HasValue) MaxHistoryDays = Math.Max(1, Math.Min(365, maxHistoryDays.Value));
        if (autoDetectTimezone.HasValue) AutoDetectTimezone = autoDetectTimezone.Value;
        if (defaultSearchRadiusKm.HasValue) DefaultSearchRadiusKm = Math.Max(1, Math.Min(500, defaultSearchRadiusKm.Value));
        UpdatedAt = DateTime.UtcNow;
    }

    public void EnableLocationSharing()
    {
        LocationSharingEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DisableLocationSharing()
    {
        LocationSharingEnabled = false;
        ShareWithBusinesses = false;
        SharePreciseLocation = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
