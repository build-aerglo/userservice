namespace UserService.Domain.Entities;

public class PasswordResetRequest
{
    public Guid ResetId { get; private set; }
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    // Dapper needs a parameterless constructor
    protected PasswordResetRequest() { }

    public PasswordResetRequest(Guid userId, int expirationMinutes = 15)
    {
        ResetId = Guid.NewGuid();
        Id = userId;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
}
