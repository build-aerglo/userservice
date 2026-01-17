namespace UserService.Domain.Entities;
public class SupportUserProfile
{
   
    public Guid Id { get; private set; } 
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    
    protected SupportUserProfile() { }

    // Domain constructor - used when creating new support user profiles in code
    public SupportUserProfile(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Method to update timestamp (if needed for future updates)
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}