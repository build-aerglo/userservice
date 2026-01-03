namespace UserService.Domain.Entities;

public class UserBadge
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid BadgeId { get; private set; }
    public DateTime EarnedAt { get; private set; }
    public string? Source { get; private set; }
    public string Metadata { get; private set; } = "{}";

    protected UserBadge() { }

    public UserBadge(Guid userId, Guid badgeId, string? source = null, string? metadata = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        BadgeId = badgeId;
        EarnedAt = DateTime.UtcNow;
        Source = source;
        Metadata = metadata ?? "{}";
    }
}
