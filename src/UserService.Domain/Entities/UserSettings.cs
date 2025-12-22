using System.Text.Json;

namespace UserService.Domain.Entities;
public class UserSettings
{
    public Guid UserId { get; private set; }
    public string NotificationPreferences { get; private set; } = "{}"; // JSONB stored as string
    public bool DarkMode { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    protected UserSettings() 
    {
        NotificationPreferences = "{}";
    }

    // Domain constructor - creates default settings for a user
    public UserSettings(Guid userId)
    {
        UserId = userId;
        DarkMode = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        // Set default notification preferences
        var defaultPrefs = new NotificationPreferencesModel
        {
            EmailNotifications = true,
            SmsNotifications = false,
            PushNotifications = true,
            MarketingEmails = false
        };
        NotificationPreferences = JsonSerializer.Serialize(defaultPrefs);
    }

    // Method to update settings
    public void UpdateSettings(bool? darkMode = null, NotificationPreferencesModel? notificationPrefs = null)
    {
        if (darkMode.HasValue) 
            DarkMode = darkMode.Value;
        
        if (notificationPrefs != null)
            NotificationPreferences = JsonSerializer.Serialize(notificationPrefs);
        
        UpdatedAt = DateTime.UtcNow;
    }

    // Helper method to get typed notification preferences
    public NotificationPreferencesModel GetNotificationPreferences()
    {
        try
        {
            return JsonSerializer.Deserialize<NotificationPreferencesModel>(NotificationPreferences) 
                   ?? new NotificationPreferencesModel();
        }
        catch
        {
            return new NotificationPreferencesModel();
        }
    }
}

/// <summary>
/// Model for the JSONB notification_preferences column
/// </summary>
public class NotificationPreferencesModel
{
    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; } = false;
    public bool PushNotifications { get; set; } = true;
    public bool MarketingEmails { get; set; } = false;
}