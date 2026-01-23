namespace UserService.Domain.Entities;

public class SocialIdentity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Provider { get; private set; } = default!;
    public string ProviderUserId { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? Name { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected SocialIdentity() { }

    public SocialIdentity(
        Guid userId,
        string provider,
        string providerUserId,
        string? email,
        string? name)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Provider = provider;
        ProviderUserId = providerUserId;
        Email = email;
        Name = name;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string? email, string? name)
    {
        if (email != null) Email = email;
        if (name != null) Name = name;
        UpdatedAt = DateTime.UtcNow;
    }
}
