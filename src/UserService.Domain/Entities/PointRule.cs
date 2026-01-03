namespace UserService.Domain.Entities;

public class PointRule
{
    public Guid Id { get; private set; }
    public string ActionType { get; private set; } = default!;
    public int PointsValue { get; private set; }
    public string? Description { get; private set; }
    public int? MaxDailyOccurrences { get; private set; }
    public int? MaxTotalOccurrences { get; private set; }
    public int? CooldownMinutes { get; private set; }
    public bool IsActive { get; private set; }
    public bool MultiplierEligible { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected PointRule() { }

    public PointRule(
        string actionType,
        int pointsValue,
        string? description = null,
        int? maxDailyOccurrences = null,
        int? maxTotalOccurrences = null,
        int? cooldownMinutes = null,
        bool multiplierEligible = true)
    {
        Id = Guid.NewGuid();
        ActionType = actionType;
        PointsValue = pointsValue;
        Description = description;
        MaxDailyOccurrences = maxDailyOccurrences;
        MaxTotalOccurrences = maxTotalOccurrences;
        CooldownMinutes = cooldownMinutes;
        IsActive = true;
        MultiplierEligible = multiplierEligible;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        int? pointsValue = null,
        string? description = null,
        int? maxDailyOccurrences = null,
        int? cooldownMinutes = null)
    {
        if (pointsValue.HasValue) PointsValue = pointsValue.Value;
        if (description != null) Description = description;
        MaxDailyOccurrences = maxDailyOccurrences ?? MaxDailyOccurrences;
        CooldownMinutes = cooldownMinutes ?? CooldownMinutes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
}
