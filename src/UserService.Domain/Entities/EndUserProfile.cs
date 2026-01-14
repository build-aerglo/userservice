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
    
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string? SocialMedia { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void UpdateSocialMedia(string? newSocialMedia)
    {
        SocialMedia = newSocialMedia;
        UpdatedAt = DateTime.UtcNow;
    }
}