namespace UserService.Domain.Entities;

public class UserLocation
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public decimal? Accuracy { get; private set; }
    public decimal? Altitude { get; private set; }
    public decimal? AltitudeAccuracy { get; private set; }
    public decimal? Heading { get; private set; }
    public decimal? Speed { get; private set; }
    public string Source { get; private set; } = "gps";
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? Country { get; private set; }
    public string? CountryCode { get; private set; }
    public string? PostalCode { get; private set; }
    public string? Timezone { get; private set; }
    public string Metadata { get; private set; } = "{}";
    public DateTime RecordedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected UserLocation() { }

    public UserLocation(
        Guid userId,
        decimal latitude,
        decimal longitude,
        string source = "gps",
        decimal? accuracy = null,
        decimal? altitude = null,
        decimal? altitudeAccuracy = null,
        decimal? heading = null,
        decimal? speed = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Latitude = latitude;
        Longitude = longitude;
        Source = source;
        Accuracy = accuracy;
        Altitude = altitude;
        AltitudeAccuracy = altitudeAccuracy;
        Heading = heading;
        Speed = speed;
        RecordedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetAddressInfo(
        string? address = null,
        string? city = null,
        string? state = null,
        string? country = null,
        string? countryCode = null,
        string? postalCode = null,
        string? timezone = null)
    {
        Address = address;
        City = city;
        State = state;
        Country = country;
        CountryCode = countryCode;
        PostalCode = postalCode;
        Timezone = timezone;
    }

    public void SetMetadata(string metadata)
    {
        Metadata = metadata;
    }

    public double DistanceToKm(decimal otherLat, decimal otherLon)
    {
        const double earthRadiusKm = 6371.0;

        var lat1Rad = ToRadians((double)Latitude);
        var lat2Rad = ToRadians((double)otherLat);
        var deltaLat = ToRadians((double)(otherLat - Latitude));
        var deltaLon = ToRadians((double)(otherLon - Longitude));

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
