namespace UserService.Domain.Entities;

public class EndUserProfile
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string? SocialMedia { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public EndUserProfile(Guid userId, string? socialMedia)
    {
        UserId = userId;
        SocialMedia = socialMedia;
    }

    public void UpdateSocialMedia(string? newSocialMedia)
    {
        SocialMedia = newSocialMedia;
        UpdatedAt = DateTime.UtcNow;
    }
}