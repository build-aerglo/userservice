using Moq;
using NUnit.Framework;
using UserService.Application.DTOs.Location;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Tests;

[TestFixture]
public class LocationServiceTests
{
    private Mock<IUserLocationRepository> _locationRepoMock;
    private Mock<IUserSavedLocationRepository> _savedLocationRepoMock;
    private Mock<IUserLocationPreferencesRepository> _preferencesRepoMock;
    private Mock<IGeofenceRepository> _geofenceRepoMock;
    private Mock<IUserGeofenceEventRepository> _geofenceEventRepoMock;
    private Mock<IUserRepository> _userRepoMock;
    private ILocationService _locationService;

    [SetUp]
    public void Setup()
    {
        _locationRepoMock = new Mock<IUserLocationRepository>();
        _savedLocationRepoMock = new Mock<IUserSavedLocationRepository>();
        _preferencesRepoMock = new Mock<IUserLocationPreferencesRepository>();
        _geofenceRepoMock = new Mock<IGeofenceRepository>();
        _geofenceEventRepoMock = new Mock<IUserGeofenceEventRepository>();
        _userRepoMock = new Mock<IUserRepository>();

        _locationService = new LocationService(
            _locationRepoMock.Object,
            _savedLocationRepoMock.Object,
            _preferencesRepoMock.Object,
            _geofenceRepoMock.Object,
            _geofenceEventRepoMock.Object,
            _userRepoMock.Object
        );
    }

    [Test]
    public async Task RecordLocationAsync_ValidCoordinates_RecordsLocation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var prefs = new UserLocationPreferences(userId);
        _preferencesRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(prefs);
        _geofenceRepoMock.Setup(r => r.GetActiveAsync()).ReturnsAsync(new List<Geofence>());

        var dto = new RecordLocationDto(userId, 40.7128m, -74.0060m, "gps", 10.0m);

        // Act
        var result = await _locationService.RecordLocationAsync(dto);

        // Assert
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.Latitude, Is.EqualTo(40.7128m));
        Assert.That(result.Longitude, Is.EqualTo(-74.0060m));
        _locationRepoMock.Verify(r => r.AddAsync(It.IsAny<UserLocation>()), Times.Once);
    }

    [Test]
    public void RecordLocationAsync_InvalidLatitude_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new RecordLocationDto(userId, 100.0m, -74.0060m); // Invalid latitude > 90

        // Act & Assert
        Assert.ThrowsAsync<InvalidCoordinatesException>(async () =>
            await _locationService.RecordLocationAsync(dto));
    }

    [Test]
    public void RecordLocationAsync_InvalidLongitude_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new RecordLocationDto(userId, 40.7128m, -200.0m); // Invalid longitude < -180

        // Act & Assert
        Assert.ThrowsAsync<InvalidCoordinatesException>(async () =>
            await _locationService.RecordLocationAsync(dto));
    }

    [Test]
    public void RecordLocationAsync_HistoryDisabled_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var prefs = new UserLocationPreferences(userId);
        prefs.UpdatePreferences(locationHistoryEnabled: false);
        _preferencesRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(prefs);

        var dto = new RecordLocationDto(userId, 40.7128m, -74.0060m);

        // Act & Assert
        Assert.ThrowsAsync<LocationHistoryDisabledException>(async () =>
            await _locationService.RecordLocationAsync(dto));
    }

    [Test]
    public async Task GetLatestLocationAsync_HasLocation_ReturnsLocation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var location = new UserLocation(userId, 40.7128m, -74.0060m);
        _locationRepoMock.Setup(r => r.GetLatestByUserIdAsync(userId)).ReturnsAsync(location);

        // Act
        var result = await _locationService.GetLatestLocationAsync(userId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Latitude, Is.EqualTo(40.7128m));
    }

    [Test]
    public async Task CreateSavedLocationAsync_ValidInput_CreatesLocation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _savedLocationRepoMock.Setup(r => r.GetByUserIdAndNameAsync(userId, "Home")).ReturnsAsync((UserSavedLocation?)null);

        var dto = new CreateSavedLocationDto(userId, "Home", 40.7128m, -74.0060m, "My Home", "123 Main St");

        // Act
        var result = await _locationService.CreateSavedLocationAsync(dto);

        // Assert
        Assert.That(result.Name, Is.EqualTo("Home"));
        Assert.That(result.Label, Is.EqualTo("My Home"));
        _savedLocationRepoMock.Verify(r => r.AddAsync(It.IsAny<UserSavedLocation>()), Times.Once);
    }

    [Test]
    public void CreateSavedLocationAsync_DuplicateName_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existing = new UserSavedLocation(userId, "Home", 40.7128m, -74.0060m);
        _savedLocationRepoMock.Setup(r => r.GetByUserIdAndNameAsync(userId, "Home")).ReturnsAsync(existing);

        var dto = new CreateSavedLocationDto(userId, "Home", 41.0m, -75.0m);

        // Act & Assert
        Assert.ThrowsAsync<SavedLocationAlreadyExistsException>(async () =>
            await _locationService.CreateSavedLocationAsync(dto));
    }

    [Test]
    public async Task GetLocationPreferencesAsync_NewUser_CreatesDefaultPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _preferencesRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserLocationPreferences?)null);

        // Act
        var result = await _locationService.GetLocationPreferencesAsync(userId);

        // Assert
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.LocationSharingEnabled, Is.False);
        Assert.That(result.MaxHistoryDays, Is.EqualTo(90));
        _preferencesRepoMock.Verify(r => r.AddAsync(It.IsAny<UserLocationPreferences>()), Times.Once);
    }

    [Test]
    public async Task UpdateLocationPreferencesAsync_ValidInput_UpdatesPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var prefs = new UserLocationPreferences(userId);
        _preferencesRepoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(prefs);

        var dto = new UpdateLocationPreferencesDto(
            LocationSharingEnabled: true,
            ShareWithBusinesses: true,
            MaxHistoryDays: 30
        );

        // Act
        var result = await _locationService.UpdateLocationPreferencesAsync(userId, dto);

        // Assert
        Assert.That(result.LocationSharingEnabled, Is.True);
        Assert.That(result.ShareWithBusinesses, Is.True);
        Assert.That(result.MaxHistoryDays, Is.EqualTo(30));
        _preferencesRepoMock.Verify(r => r.UpdateAsync(It.IsAny<UserLocationPreferences>()), Times.Once);
    }

    [Test]
    public async Task CreateGeofenceAsync_ValidInput_CreatesGeofence()
    {
        // Arrange
        var dto = new CreateGeofenceDto("Office", 40.7128m, -74.0060m, 100.0m, "Company office");

        // Act
        var result = await _locationService.CreateGeofenceAsync(dto);

        // Assert
        Assert.That(result.Name, Is.EqualTo("Office"));
        Assert.That(result.RadiusMeters, Is.EqualTo(100.0m));
        Assert.That(result.TriggerOnEnter, Is.True);
        _geofenceRepoMock.Verify(r => r.AddAsync(It.IsAny<Geofence>()), Times.Once);
    }

    [Test]
    public async Task CheckGeofencesAsync_InsideGeofence_ReturnsContainingGeofences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var geofence = new Geofence("Office", 40.7128m, -74.0060m, 1000.0m);
        _geofenceRepoMock.Setup(r => r.GetContainingPointAsync(40.7128m, -74.0060m))
            .ReturnsAsync(new List<Geofence> { geofence });
        _geofenceEventRepoMock.Setup(r => r.GetLatestByUserAndGeofenceAsync(userId, geofence.Id))
            .ReturnsAsync((UserGeofenceEvent?)null);

        var dto = new CheckGeofenceDto(userId, 40.7128m, -74.0060m);

        // Act
        var result = await _locationService.CheckGeofencesAsync(dto);

        // Assert
        Assert.That(result.IsInsideAnyGeofence, Is.True);
        Assert.That(result.ContainingGeofences.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task SetDefaultLocationAsync_ValidLocation_SetsDefault()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var location = new UserSavedLocation(userId, "Home", 40.7128m, -74.0060m);
        _savedLocationRepoMock.Setup(r => r.GetByIdAsync(location.Id)).ReturnsAsync(location);

        // Act
        await _locationService.SetDefaultLocationAsync(userId, location.Id);

        // Assert
        _savedLocationRepoMock.Verify(r => r.ClearDefaultForUserAsync(userId), Times.Once);
        _savedLocationRepoMock.Verify(r => r.UpdateAsync(It.IsAny<UserSavedLocation>()), Times.Once);
    }

    [Test]
    public void SetDefaultLocationAsync_LocationNotFound_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        _savedLocationRepoMock.Setup(r => r.GetByIdAsync(locationId)).ReturnsAsync((UserSavedLocation?)null);

        // Act & Assert
        Assert.ThrowsAsync<SavedLocationNotFoundException>(async () =>
            await _locationService.SetDefaultLocationAsync(userId, locationId));
    }
}
