namespace UserService.Domain.Entities;

public class SocialIdentity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Provider { get; private set; } = default!;
    public string ProviderUserId { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? Name { get; private set; }
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? TokenExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected SocialIdentity() { }

    public SocialIdentity(
        Guid userId,
        string provider,
        string providerUserId,
        string? email,
        string? name,
        string? accessToken = null,
        string? refreshToken = null,
        DateTime? tokenExpiresAt = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Provider = provider;
        ProviderUserId = providerUserId;
        Email = email;
        Name = name;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        TokenExpiresAt = tokenExpiresAt;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTokens(string? accessToken, string? refreshToken, DateTime? expiresAt)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        TokenExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string? email, string? name)
    {
        if (email != null) Email = email;
        if (name != null) Name = name;
        UpdatedAt = DateTime.UtcNow;
    }
}
