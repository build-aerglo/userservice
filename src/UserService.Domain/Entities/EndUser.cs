namespace UserService.Domain.Entities;

public class EndUser
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string? Preferences { get; private set; }
    public string? Bio { get; private set; } 
    public string? SocialLinks { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }


    protected EndUser()
    {
    }

    public EndUser(Guid userId, string? preferences, string? bio, string? socialLinks)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Preferences = preferences;
        Bio = bio;
        SocialLinks = socialLinks;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Update(string? preferences, string? bio, string? socialLinks)
    {
        Preferences = preferences ?? Preferences;
        Bio = bio ?? Bio;
        SocialLinks = socialLinks ?? SocialLinks;
        UpdatedAt = DateTime.UtcNow;
    }  
    
}