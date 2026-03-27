namespace UserService.Application.DTOs;

public record BusinessClaimDto(
    Guid BusinessId,
    int Status,
    DateTime ExpiresAt
);
