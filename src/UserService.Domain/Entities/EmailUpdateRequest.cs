namespace UserService.Domain.Entities;

public class EmailUpdateRequest
{
    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public string Email { get; private set; } = default!;
    public string? Reason { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Dapper needs a parameterless constructor
    protected EmailUpdateRequest() { }

    public EmailUpdateRequest(Guid businessId, string email, string? reason = null)
    {
        Id = Guid.NewGuid();
        BusinessId = businessId;
        Email = email;
        Reason = reason;
        CreatedAt = DateTime.UtcNow;
    }
}
