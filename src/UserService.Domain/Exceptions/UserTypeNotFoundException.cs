namespace UserService.Domain.Exceptions;

public class UserTypeNotFoundException(string message) : Exception(message);