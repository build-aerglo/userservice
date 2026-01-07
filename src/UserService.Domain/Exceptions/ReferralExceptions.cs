namespace UserService.Domain.Exceptions;

public class ReferralCodeNotFoundException(string code)
    : Exception($"Referral code '{code}' was not found.");

public class ReferralCodeAlreadyExistsException(Guid userId)
    : Exception($"User '{userId}' already has a referral code.");

public class InvalidReferralCodeException(string code)
    : Exception($"Invalid referral code: '{code}'.");

public class ReferralNotFoundException(Guid referralId)
    : Exception($"Referral with ID '{referralId}' was not found.");

public class UserAlreadyReferredException(Guid userId)
    : Exception($"User '{userId}' has already been referred by another user.");

public class SelfReferralException()
    : Exception("Users cannot refer themselves.");

public class ReferralCodeInactiveException(string code)
    : Exception($"Referral code '{code}' is no longer active.");

public class ReferralAlreadyCompletedException(Guid referralId)
    : Exception($"Referral '{referralId}' has already been completed.");

public class ReferralCodeTakenException(String message)
    : Exception($"Referral code '{message}' was taken.");
