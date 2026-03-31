namespace UserService.Domain.Exceptions;

public class BusinessClaimExpiredException(Guid businessId)
    : Exception($"The business claim for business '{businessId}' has expired.");

public class BusinessClaimNotApprovedException(Guid businessId)
    : Exception($"The business claim for business '{businessId}' has not been approved.");
