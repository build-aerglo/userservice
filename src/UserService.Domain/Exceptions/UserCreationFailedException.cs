namespace UserService.Domain.Exceptions;

public class UserCreationFailedException(string message) : Exception(message);