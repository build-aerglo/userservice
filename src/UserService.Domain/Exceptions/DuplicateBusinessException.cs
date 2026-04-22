namespace UserService.Domain.Exceptions;

public class DuplicateBusinessException(string message) : Exception(message);