namespace UserService.Domain.Entities;

public class BusinessClaim
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = default!;
    public int Status { get; set; }
    public DateTime ExpiresAt { get; set; }
}
