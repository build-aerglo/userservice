namespace UserService.Domain.Entities;

public class UserDailyPoints
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string ActionType { get; private set; } = default!;
    public DateTime OccurrenceDate { get; private set; }
    public int OccurrenceCount { get; private set; }
    public DateTime LastOccurrenceAt { get; private set; }

    protected UserDailyPoints() { }

    public UserDailyPoints(Guid userId, string actionType)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        ActionType = actionType;
        OccurrenceDate = DateTime.UtcNow.Date;
        OccurrenceCount = 1;
        LastOccurrenceAt = DateTime.UtcNow;
    }

    public void IncrementOccurrence()
    {
        OccurrenceCount++;
        LastOccurrenceAt = DateTime.UtcNow;
    }

    public bool CanEarnMore(int? maxDaily)
    {
        if (!maxDaily.HasValue) return true;
        return OccurrenceCount < maxDaily.Value;
    }

    public bool IsCooldownExpired(int? cooldownMinutes)
    {
        if (!cooldownMinutes.HasValue) return true;
        return DateTime.UtcNow > LastOccurrenceAt.AddMinutes(cooldownMinutes.Value);
    }
}
