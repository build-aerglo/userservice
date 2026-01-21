namespace UserService.Domain.Exceptions;

public class SupportUserNotFoundException(Guid userId) : Exception($"Support user with User ID {userId} does not exist.");