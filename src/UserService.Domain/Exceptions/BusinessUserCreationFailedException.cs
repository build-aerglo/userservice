namespace UserService.Domain.Exceptions;

public class BusinessUserCreationFailedException(string message) : Exception(message);