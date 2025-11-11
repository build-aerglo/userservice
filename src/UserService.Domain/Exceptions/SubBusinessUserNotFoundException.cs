namespace UserService.Domain.Exceptions;

public class SubBusinessUserNotFoundException(Guid userId)  : Exception($"Sub-business user with ID '{userId}' was not found.");