namespace UserService.Domain.Entities;

public class Review
{
    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid? LocationId { get; private set; }
    public Guid? ReviewerId { get; private set; }
    public string? Email { get; private set; }
    public decimal StarRating { get; private set; }
    public string ReviewBody { get; private set; } = default!;
    public string[]? PhotoUrls { get; private set; }
    public bool ReviewAsAnon { get; private set; }
    public bool IsGuestReview { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public string Status { get; private set; } = "PENDING";
    public string? IpAddress { get; private set; }
    public string? DeviceId { get; private set; }
    public string? Geolocation { get; private set; }
    public string? UserAgent { get; private set; }
    public string? ValidationResult { get; private set; }
    public DateTime? ValidatedAt { get; private set; }

    // RS-006: Edit tracking fields
    public bool IsEdited { get; private set; }
    public int EditCount { get; private set; }
    public DateTime? LastEditedAt { get; private set; }
    public string? OriginalReviewBody { get; private set; }

    // RS-008: Sentiment analysis fields
    public string? Sentiment { get; private set; }
    public decimal? SentimentScore { get; private set; }
    public DateTime? SentimentAnalyzedAt { get; private set; }

    // RS-005: Helpful count cache
    public int HelpfulCount { get; private set; }

    protected Review()
    {
    }

    public Review(
        Guid businessId,
        Guid? locationId,
        Guid? reviewerId,
        string? email,
        decimal starRating,
        string reviewBody,
        string[]? photoUrls,
        bool reviewAsAnon,
        string? ipAddress = null,
        string? deviceId = null,
        string? geolocation = null,
        string? userAgent = null)
    {
        if (starRating < 1 || starRating > 5)
            throw new ArgumentException("Star rating must be between 1 and 5.", nameof(starRating));

        if (string.IsNullOrWhiteSpace(reviewBody) || reviewBody.Length < 20 || reviewBody.Length > 500)
            throw new ArgumentException("Review body must be between 20 and 500 characters.", nameof(reviewBody));

        if (photoUrls is not null && photoUrls.Length > 3)
            throw new ArgumentException("Maximum 3 photos allowed.", nameof(photoUrls));

        // ✅ Determine if this is a guest review
        var isGuest = !reviewerId.HasValue;

        // If guest review, email is required
        if (isGuest && string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required for guest reviews.", nameof(email));

        // ✅ If authenticated review, force reviewAsAnon to false if they provided reviewerId
        if (!isGuest && reviewAsAnon)
        {
            // Authenticated users can choose to be anonymous
        }

        Id = Guid.NewGuid();
        BusinessId = businessId;
        LocationId = locationId;
        ReviewerId = reviewerId;
        Email = email;
        StarRating = starRating;
        ReviewBody = reviewBody;
        PhotoUrls = photoUrls;
        ReviewAsAnon = reviewAsAnon;
        IsGuestReview = isGuest;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        Status = "PENDING";
        IpAddress = ipAddress;
        DeviceId = deviceId;
        Geolocation = geolocation;
        UserAgent = userAgent;
    }

    public void Update(decimal? starRating, string? reviewBody, string[]? photoUrls, bool? reviewAsAnon)
    {
        if (starRating.HasValue)
        {
            if (starRating.Value < 1 || starRating.Value > 5)
                throw new ArgumentException("Star rating must be between 1 and 5.", nameof(starRating));
            StarRating = starRating.Value;
        }

        if (!string.IsNullOrWhiteSpace(reviewBody))
        {
            if (reviewBody.Length < 20 || reviewBody.Length > 500)
                throw new ArgumentException("Review body must be between 20 and 500 characters.", nameof(reviewBody));
            ReviewBody = reviewBody;
        }

        if (photoUrls is not null)
        {
            if (photoUrls.Length > 3)
                throw new ArgumentException("Maximum 3 photos allowed.", nameof(photoUrls));
            PhotoUrls = photoUrls;
        }

        if (reviewAsAnon.HasValue)
        {
            ReviewAsAnon = reviewAsAnon.Value;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateValidationStatus(string status, string validationResult)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status cannot be null or empty.", nameof(status));

        var validStatuses = new[] { "PENDING", "APPROVED", "REJECTED", "FLAGGED" };
        if (!validStatuses.Contains(status))
            throw new ArgumentException($"Invalid status. Must be one of: {string.Join(", ", validStatuses)}",
                nameof(status));

        Status = status;
        ValidationResult = validationResult;
        ValidatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// RS-006: Check if the review can still be edited (within 3-day window)
    /// </summary>
    public bool CanEdit()
    {
        // Anonymous/guest reviews cannot be edited
        if (IsGuestReview && ReviewerId == null)
            return false;

        // Check 3-day window
        return DateTime.UtcNow - CreatedAt <= TimeSpan.FromDays(3);
    }

    /// <summary>
    /// RS-006: Get time remaining to edit
    /// </summary>
    public TimeSpan? GetEditTimeRemaining()
    {
        if (!CanEdit()) return null;
        var remaining = CreatedAt.AddDays(3) - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : null;
    }

    /// <summary>
    /// RS-006: Records an edit and updates edit tracking fields
    /// </summary>
    public void RecordEdit(decimal? newStarRating, string? newReviewBody, string[]? newPhotoUrls, bool? newReviewAsAnon)
    {
        if (!CanEdit())
            throw new InvalidOperationException("Reviews can only be edited within 3 days of creation.");

        // Store original on first edit
        if (!IsEdited)
        {
            OriginalReviewBody = ReviewBody;
        }

        // Call base Update
        Update(newStarRating, newReviewBody, newPhotoUrls, newReviewAsAnon);

        // Track edit
        IsEdited = true;
        EditCount++;
        LastEditedAt = DateTime.UtcNow;

        // Reset status for re-validation
        Status = "PENDING";
        ValidatedAt = null;
    }

    /// <summary>
    /// RS-008: Updates sentiment analysis results
    /// </summary>
    public void UpdateSentiment(string sentiment, decimal score)
    {
        var validSentiments = new[] { "POSITIVE", "NEGATIVE", "NEUTRAL" };
        if (!validSentiments.Contains(sentiment))
            throw new ArgumentException($"Invalid sentiment. Must be one of: {string.Join(", ", validSentiments)}",
                nameof(sentiment));

        if (score < 0 || score > 1)
            throw new ArgumentException("Score must be between 0 and 1.", nameof(score));

        Sentiment = sentiment;
        SentimentScore = score;
        SentimentAnalyzedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// RS-005: Updates helpful vote count
    /// </summary>
    public void IncrementHelpfulCount()
    {
        HelpfulCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// RS-005: Decrements helpful vote count
    /// </summary>
    public void DecrementHelpfulCount()
    {
        HelpfulCount = Math.Max(0, HelpfulCount - 1);
        UpdatedAt = DateTime.UtcNow;
    }
}