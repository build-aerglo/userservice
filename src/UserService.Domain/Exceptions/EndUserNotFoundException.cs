namespace UserService.Domain.Exceptions;

public class EndUserNotFoundException(Guid userId) 
    : Exception($"End user with ID '{userId}' was not found.");