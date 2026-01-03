namespace UserService.Domain.Enums;

public enum LocationSource
{
    Gps,
    Network,
    Ip,
    Manual
}

public enum GeofenceType
{
    Circle,
    Polygon
}

public enum GeofenceEventType
{
    Enter,
    Exit,
    Dwell
}
