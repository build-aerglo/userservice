namespace UserService.Domain.Enums;

public enum VerificationMethod
{
    Sms,
    Voice,
    WhatsApp,
    Email
}

public enum VerificationLevel
{
    None,
    Basic,      // Email only
    Verified,   // Email + Phone
    Trusted     // Email + Phone + Identity
}

public enum VerificationType
{
    Email,
    Phone,
    Identity
}
