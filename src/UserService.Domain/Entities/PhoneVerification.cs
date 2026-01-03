namespace UserService.Domain.Entities;

public class PhoneVerification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string PhoneNumber { get; private set; } = default!;
    public string CountryCode { get; private set; } = "+1";
    public string VerificationCode { get; private set; } = default!;
    public string VerificationMethod { get; private set; } = "sms";
    public bool IsVerified { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public int Attempts { get; private set; }
    public int MaxAttempts { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected PhoneVerification() { }

    public PhoneVerification(
        Guid userId,
        string phoneNumber,
        string countryCode = "+1",
        string verificationMethod = "sms",
        int expirationMinutes = 10,
        int maxAttempts = 5)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        PhoneNumber = phoneNumber;
        CountryCode = countryCode;
        VerificationCode = GenerateCode();
        VerificationMethod = verificationMethod;
        IsVerified = false;
        Attempts = 0;
        MaxAttempts = maxAttempts;
        ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public string FullPhoneNumber => $"{CountryCode}{PhoneNumber}";

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

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
    public bool HasAttemptsRemaining() => Attempts < MaxAttempts;

    public void Regenerate(int expirationMinutes = 10)
    {
        VerificationCode = GenerateCode();
        Attempts = 0;
        ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
        UpdatedAt = DateTime.UtcNow;
    }

    private static string GenerateCode() => new Random().Next(100000, 999999).ToString();
}
