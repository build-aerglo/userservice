namespace UserService.Domain.Entities;

public class Settings
{
    public Guid UserId { get; private set; }
    public List<string> NotificationPreferences { get; private set; } = new();
    public bool DarkMode { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Settings() {} // EF Core/Dapper needs this sometimes

    public Settings(Guid userId, List<string> notificationPreferences, bool darkMode)
    {
        UserId = userId;
        NotificationPreferences = notificationPreferences;
        DarkMode = darkMode;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSettings(List<string> notificationPreferences, bool darkMode)
    {
        NotificationPreferences = notificationPreferences;
        DarkMode = darkMode;
        UpdatedAt = DateTime.UtcNow;
    }
}