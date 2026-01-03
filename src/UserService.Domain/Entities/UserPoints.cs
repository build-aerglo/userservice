namespace UserService.Domain.Entities;

public class UserPoints
{
    public Guid UserId { get; private set; }
    public int TotalPoints { get; private set; }
    public int AvailablePoints { get; private set; }
    public int LifetimePoints { get; private set; }
    public int RedeemedPoints { get; private set; }
    public int PendingPoints { get; private set; }
    public DateTime? LastEarnedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected UserPoints() { }

    public UserPoints(Guid userId)
    {
        UserId = userId;
        TotalPoints = 0;
        AvailablePoints = 0;
        LifetimePoints = 0;
        RedeemedPoints = 0;
        PendingPoints = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddPoints(int points)
    {
        if (points <= 0) return;

        TotalPoints += points;
        AvailablePoints += points;
        LifetimePoints += points;
        LastEarnedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddPendingPoints(int points)
    {
        if (points <= 0) return;
        PendingPoints += points;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ConfirmPendingPoints(int points)
    {
        if (points <= 0 || points > PendingPoints) return;

        PendingPoints -= points;
        TotalPoints += points;
        AvailablePoints += points;
        LifetimePoints += points;
        LastEarnedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool RedeemPoints(int points)
    {
        if (points <= 0 || points > AvailablePoints) return false;

        AvailablePoints -= points;
        TotalPoints -= points;
        RedeemedPoints += points;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public void ExpirePoints(int points)
    {
        if (points <= 0) return;

        var toExpire = Math.Min(points, AvailablePoints);
        AvailablePoints -= toExpire;
        TotalPoints -= toExpire;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AdjustPoints(int adjustment)
    {
        TotalPoints += adjustment;
        AvailablePoints += adjustment;
        if (adjustment > 0) LifetimePoints += adjustment;
        UpdatedAt = DateTime.UtcNow;
    }
}
