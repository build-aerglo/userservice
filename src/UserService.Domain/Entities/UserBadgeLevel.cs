namespace UserService.Domain.Entities;

public class UserBadgeLevel
{
    public Guid UserId { get; private set; }
    public string CurrentLevel { get; private set; } = "Pioneer";
    public int LevelProgress { get; private set; }
    public int TotalBadgesEarned { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected UserBadgeLevel() { }

    public UserBadgeLevel(Guid userId)
    {
        UserId = userId;
        CurrentLevel = "Pioneer";
        LevelProgress = 0;
        TotalBadgesEarned = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementBadgeCount()
    {
        TotalBadgesEarned++;
        RecalculateLevel();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProgress(int progress)
    {
        LevelProgress = Math.Clamp(progress, 0, 100);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetLevel(string level, int progress)
    {
        CurrentLevel = level;
        LevelProgress = Math.Clamp(progress, 0, 100);
        UpdatedAt = DateTime.UtcNow;
    }

    private void RecalculateLevel()
    {
        var (level, progress) = TotalBadgesEarned switch
        {
            >= 50 => ("Legend", CalculateProgress(50, 100)),
            >= 30 => ("Master", CalculateProgress(30, 50)),
            >= 15 => ("Pro", CalculateProgress(15, 30)),
            >= 5 => ("Expert", CalculateProgress(5, 15)),
            >= 1 => ("Explorer", CalculateProgress(1, 5)),
            _ => ("Pioneer", 0)
        };

        CurrentLevel = level;
        LevelProgress = progress;
    }

    private int CalculateProgress(int currentThreshold, int nextThreshold)
    {
        if (TotalBadgesEarned >= nextThreshold) return 100;
        var range = nextThreshold - currentThreshold;
        var progress = TotalBadgesEarned - currentThreshold;
        return (int)((double)progress / range * 100);
    }
}
