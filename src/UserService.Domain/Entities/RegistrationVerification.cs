namespace UserService.Domain.Entities;

/// <summary>
/// Represents a pending email-verification record created on user sign-up.
/// The token is the AES-encrypted email; decrypting it confirms ownership.
/// </summary>
public class RegistrationVerification
{
    protected RegistrationVerification() { }

    public RegistrationVerification(string email, string username, string token, string userType)
    {
        Id = Guid.NewGuid();
        Email = email;
        Username = username;
        Token = token;
        Expiry = DateTime.UtcNow.AddHours(24);
        UserType = userType;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = default!;
    public string Username { get; private set; } = default!;
    public string Token { get; private set; } = default!;
    public DateTime Expiry { get; private set; }
    public string UserType { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow > Expiry;
}
