namespace UserService.Domain.Entities;

public class BadgeDefinition
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? IconUrl { get; private set; }
    public int Tier { get; private set; }
    public int PointsRequired { get; private set; }
    public string Category { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected BadgeDefinition() { }

    public BadgeDefinition(
        string name,
        string displayName,
        string? description,
        string? iconUrl,
        int tier,
        int pointsRequired,
        string category)
    {
        Id = Guid.NewGuid();
        Name = name;
        DisplayName = displayName;
        Description = description;
        IconUrl = iconUrl;
        Tier = tier;
        PointsRequired = pointsRequired;
        Category = category;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        string? displayName = null,
        string? description = null,
        string? iconUrl = null,
        int? tier = null,
        int? pointsRequired = null)
    {
        if (!string.IsNullOrEmpty(displayName)) DisplayName = displayName;
        if (description != null) Description = description;
        if (iconUrl != null) IconUrl = iconUrl;
        if (tier.HasValue) Tier = tier.Value;
        if (pointsRequired.HasValue) PointsRequired = pointsRequired.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
}
