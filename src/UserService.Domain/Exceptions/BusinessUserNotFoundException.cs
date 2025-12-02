namespace UserService.Domain.Exceptions;

public class BusinessUserNotFoundException(Guid userId)  : Exception($"Business user with ID '{userId}' was not found.");