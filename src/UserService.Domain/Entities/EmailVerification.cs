namespace UserService.Domain.Entities;

public class EmailVerification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Email { get; private set; } = default!;
    public string VerificationCode { get; private set; } = default!;
    public Guid VerificationToken { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public int Attempts { get; private set; }
    public int MaxAttempts { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected EmailVerification() { }

    public EmailVerification(Guid userId, string email, int expirationMinutes = 30, int maxAttempts = 5)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Email = email;
        VerificationCode = GenerateCode();
        VerificationToken = Guid.NewGuid();
        IsVerified = false;
        Attempts = 0;
        MaxAttempts = maxAttempts;
        ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool Verify(string code)
    {
        if (IsVerified) return true;
        if (IsExpired()) return false;
        if (Attempts >= MaxAttempts) return false;

        Attempts++;
        UpdatedAt = DateTime.UtcNow;

        if (VerificationCode == code)
        {
            IsVerified = true;
            VerifiedAt = DateTime.UtcNow;
            return true;
        }

        return false;
    }

    public bool VerifyByToken(Guid token)
    {
        if (IsVerified) return true;
        if (IsExpired()) return false;

        if (VerificationToken == token)
        {
            IsVerified = true;
            VerifiedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            return true;
        }

        return false;
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
    public bool HasAttemptsRemaining() => Attempts < MaxAttempts;

    public void Regenerate(int expirationMinutes = 30)
    {
        VerificationCode = GenerateCode();
        VerificationToken = Guid.NewGuid();
        Attempts = 0;
        ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
        UpdatedAt = DateTime.UtcNow;
    }

    private static string GenerateCode() => new Random().Next(100000, 999999).ToString();
}
