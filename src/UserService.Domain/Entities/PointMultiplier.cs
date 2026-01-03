namespace UserService.Domain.Entities;

public class PointMultiplier
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Multiplier { get; private set; }
    public string[]? ActionTypes { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected PointMultiplier() { }

    public PointMultiplier(
        string name,
        decimal multiplier,
        DateTime startsAt,
        DateTime endsAt,
        string? description = null,
        string[]? actionTypes = null)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Multiplier = multiplier;
        ActionTypes = actionTypes;
        StartsAt = startsAt;
        EndsAt = endsAt;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public bool IsCurrentlyActive()
    {
        var now = DateTime.UtcNow;
        return IsActive && now >= StartsAt && now <= EndsAt;
    }

    public bool AppliesToAction(string actionType)
    {
        if (ActionTypes == null || ActionTypes.Length == 0) return true;
        return ActionTypes.Contains(actionType);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
