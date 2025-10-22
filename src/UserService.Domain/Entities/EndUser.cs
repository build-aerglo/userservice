using System.Text.Json;

namespace UserService.Domain.Entities;

public class EndUser
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public JsonDocument? Preferences { get; private set; }
    public string? Bio { get; private set; } 
    public JsonDocument? SocialLinks { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }


    protected EndUser()
    {
    }

    public EndUser(Guid userId, string? bio, JsonDocument? preferences = null, JsonDocument? socialLinks = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Preferences = preferences;
        Bio = bio;
        SocialLinks = socialLinks;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Update(JsonDocument? preferences, string? bio, JsonDocument? socialLinks)
    {
        Preferences = preferences ?? Preferences;
        Bio = bio ?? Bio;
        SocialLinks = socialLinks ?? SocialLinks;
        UpdatedAt = DateTime.UtcNow;
    }  
    
}