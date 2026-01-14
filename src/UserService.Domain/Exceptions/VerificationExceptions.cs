namespace UserService.Domain.Exceptions;

public class VerificationNotFoundException(Guid userId)
    : Exception($"Verification record for user '{userId}' was not found.");

public class InvalidVerificationTokenException(string message = "Invalid or expired verification token.")
    : Exception(message);

public class VerificationTokenExpiredException()
    : Exception("Verification token has expired.");

public class MaxVerificationAttemptsExceededException()
    : Exception("Maximum verification attempts exceeded. Please request a new code.");

public class PhoneAlreadyVerifiedException(Guid userId)
    : Exception($"Phone for user '{userId}' is already verified.");

public class EmailAlreadyVerifiedException(Guid userId)
    : Exception($"Email for user '{userId}' is already verified.");

public class InvalidPhoneNumberException(string phoneNumber)
    : Exception($"Invalid phone number format: '{phoneNumber}'. Nigerian phone numbers must start with +234.");

public class VerificationSendFailedException(string message)
    : Exception(message);
