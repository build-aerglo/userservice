namespace UserService.Domain.Entities;

public class PointTransaction
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? RuleId { get; private set; }
    public string TransactionType { get; private set; } = default!;
    public int Points { get; private set; }
    public int BalanceAfter { get; private set; }
    public string? Description { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public decimal Multiplier { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public string Metadata { get; private set; } = "{}";
    public DateTime CreatedAt { get; private set; }

    protected PointTransaction() { }

    public PointTransaction(
        Guid userId,
        string transactionType,
        int points,
        int balanceAfter,
        Guid? ruleId = null,
        string? description = null,
        string? referenceType = null,
        Guid? referenceId = null,
        decimal multiplier = 1.00m,
        DateTime? expiresAt = null,
        string? metadata = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        RuleId = ruleId;
        TransactionType = transactionType;
        Points = points;
        BalanceAfter = balanceAfter;
        Description = description;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Multiplier = multiplier;
        ExpiresAt = expiresAt;
        Metadata = metadata ?? "{}";
        CreatedAt = DateTime.UtcNow;
    }

    public static PointTransaction CreateEarn(
        Guid userId,
        int points,
        int balanceAfter,
        Guid? ruleId = null,
        string? description = null,
        string? referenceType = null,
        Guid? referenceId = null,
        decimal multiplier = 1.00m)
    {
        return new PointTransaction(
            userId, "earn", points, balanceAfter, ruleId,
            description, referenceType, referenceId, multiplier);
    }

    public static PointTransaction CreateRedeem(
        Guid userId,
        int points,
        int balanceAfter,
        string? description = null,
        string? referenceType = null,
        Guid? referenceId = null)
    {
        return new PointTransaction(
            userId, "redeem", -points, balanceAfter, null,
            description, referenceType, referenceId);
    }

    public static PointTransaction CreateExpire(
        Guid userId,
        int points,
        int balanceAfter,
        string? description = null)
    {
        return new PointTransaction(
            userId, "expire", -points, balanceAfter, null, description);
    }

    public static PointTransaction CreateAdjust(
        Guid userId,
        int points,
        int balanceAfter,
        string? description = null)
    {
        return new PointTransaction(
            userId, "adjust", points, balanceAfter, null, description);
    }

    public static PointTransaction CreateBonus(
        Guid userId,
        int points,
        int balanceAfter,
        string? description = null,
        decimal multiplier = 1.00m)
    {
        return new PointTransaction(
            userId, "bonus", points, balanceAfter, null,
            description, null, null, multiplier);
    }
}
