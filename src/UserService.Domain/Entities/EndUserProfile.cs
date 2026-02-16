using System.Text.Json;

namespace UserService.Domain.Entities;

public class EndUserProfile
{
    public EndUserProfile() { }

    public EndUserProfile(Guid userId, string? socialMedia)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        SocialMedia = socialMedia;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // ✅ Make setters public so Dapper + tests work
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? SocialMedia { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public void UpdateSocialMedia(string? newSocialMedia)
    {
        SocialMedia = newSocialMedia;
        UpdatedAt = DateTime.UtcNow;
    }

    // Optional helper methods (nice to have)

    public Dictionary<string, string>? GetSocialMediaDictionary()
    {
        return string.IsNullOrWhiteSpace(SocialMedia)
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, string>>(SocialMedia);
    }

    public void SetSocialMediaDictionary(Dictionary<string, string>? dictionary)
    {
        SocialMedia = dictionary == null
            ? null
            : JsonSerializer.Serialize(dictionary);
        UpdatedAt = DateTime.UtcNow;
    }
}