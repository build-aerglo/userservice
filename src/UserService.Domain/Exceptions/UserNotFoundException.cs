namespace UserService.Domain.Exceptions;

public class UserNotFoundException(Guid userId) : Exception($"User with ID {userId} does not exist.");