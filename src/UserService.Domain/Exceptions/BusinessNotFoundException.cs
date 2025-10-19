namespace UserService.Domain.Exceptions;

public class BusinessNotFoundException(Guid businessId) : Exception($"Business with ID {businessId} does not exist.");