namespace UserService.Domain.Exceptions;

public class ReferralCodeNotFoundException : Exception
{
    public ReferralCodeNotFoundException(string code)
        : base($"Referral code '{code}' not found or inactive.") { }
}

public class InvalidReferralCodeException : Exception
{
    public InvalidReferralCodeException(string message)
        : base(message) { }
}

public class ReferralCodeAlreadyExistsException : Exception
{
    public ReferralCodeAlreadyExistsException(string code)
        : base($"Referral code '{code}' already exists.") { }
}

public class SelfReferralException : Exception
{
    public SelfReferralException()
        : base("Users cannot use their own referral code.") { }
}

public class UserAlreadyReferredException : Exception
{
    public UserAlreadyReferredException(Guid userId)
        : base($"User '{userId}' has already been referred by another user.") { }
}

public class ReferralNotFoundException : Exception
{
    public ReferralNotFoundException(Guid referralId)
        : base($"Referral with ID '{referralId}' not found.") { }
}

public class ReferralAlreadyCompletedException : Exception
{
    public ReferralAlreadyCompletedException(Guid referralId)
        : base($"Referral '{referralId}' has already been completed.") { }
}

public class ReferralExpiredException : Exception
{
    public ReferralExpiredException(Guid referralId)
        : base($"Referral '{referralId}' has expired.") { }
}

public class UserReferralCodeNotFoundException : Exception
{
    public UserReferralCodeNotFoundException(Guid userId)
        : base($"User '{userId}' does not have a referral code.") { }
}
