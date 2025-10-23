namespace UserService.Domain.Exceptions;

public class DuplicateUserEmailException(string message) : Exception(message);