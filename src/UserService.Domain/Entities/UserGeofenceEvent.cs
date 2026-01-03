namespace UserService.Domain.Entities;

public class UserGeofenceEvent
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid GeofenceId { get; private set; }
    public string EventType { get; private set; } = default!;
    public Guid? LocationId { get; private set; }
    public DateTime TriggeredAt { get; private set; }
    public string Metadata { get; private set; } = "{}";

    protected UserGeofenceEvent() { }

    public UserGeofenceEvent(
        Guid userId,
        Guid geofenceId,
        string eventType,
        Guid? locationId = null,
        string? metadata = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        GeofenceId = geofenceId;
        EventType = eventType;
        LocationId = locationId;
        TriggeredAt = DateTime.UtcNow;
        Metadata = metadata ?? "{}";
    }

    public static UserGeofenceEvent CreateEnterEvent(Guid userId, Guid geofenceId, Guid? locationId = null)
        => new(userId, geofenceId, "enter", locationId);

    public static UserGeofenceEvent CreateExitEvent(Guid userId, Guid geofenceId, Guid? locationId = null)
        => new(userId, geofenceId, "exit", locationId);

    public static UserGeofenceEvent CreateDwellEvent(Guid userId, Guid geofenceId, Guid? locationId = null)
        => new(userId, geofenceId, "dwell", locationId);
}
