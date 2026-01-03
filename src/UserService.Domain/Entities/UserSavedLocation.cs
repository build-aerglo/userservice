namespace UserService.Domain.Entities;

public class UserSavedLocation
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Label { get; private set; }
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? Country { get; private set; }
    public string? CountryCode { get; private set; }
    public string? PostalCode { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected UserSavedLocation() { }

    public UserSavedLocation(
        Guid userId,
        string name,
        decimal latitude,
        decimal longitude,
        string? label = null,
        string? address = null,
        string? city = null,
        string? state = null,
        string? country = null,
        string? countryCode = null,
        string? postalCode = null,
        bool isDefault = false)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Name = name;
        Label = label;
        Latitude = latitude;
        Longitude = longitude;
        Address = address;
        City = city;
        State = state;
        Country = country;
        CountryCode = countryCode;
        PostalCode = postalCode;
        IsDefault = isDefault;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        string? name = null,
        string? label = null,
        decimal? latitude = null,
        decimal? longitude = null,
        string? address = null,
        string? city = null,
        string? state = null,
        string? country = null,
        string? countryCode = null,
        string? postalCode = null)
    {
        if (!string.IsNullOrEmpty(name)) Name = name;
        if (label != null) Label = label;
        if (latitude.HasValue) Latitude = latitude.Value;
        if (longitude.HasValue) Longitude = longitude.Value;
        if (address != null) Address = address;
        if (city != null) City = city;
        if (state != null) State = state;
        if (country != null) Country = country;
        if (countryCode != null) CountryCode = countryCode;
        if (postalCode != null) PostalCode = postalCode;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAsDefault() { IsDefault = true; UpdatedAt = DateTime.UtcNow; }
    public void RemoveDefault() { IsDefault = false; UpdatedAt = DateTime.UtcNow; }
    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
}
