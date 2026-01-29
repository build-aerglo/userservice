namespace UserService.Domain.Entities;

public class PasswordResetRequest
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Identifier { get; private set; } = default!;
    public string IdentifierType { get; private set; } = default!;
    public bool IsVerified { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Dapper needs a parameterless constructor
    protected PasswordResetRequest() { }

    public PasswordResetRequest(Guid userId, string identifier, string identifierType, int expirationMinutes = 15)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Identifier = identifier;
        IdentifierType = identifierType;
        IsVerified = false;
        ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsVerified()
    {
        IsVerified = true;
        VerifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
}
