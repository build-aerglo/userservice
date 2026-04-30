namespace UserService.Application.Interfaces;

/// <summary>
/// RS-DeferredAuth: Triggers review activation in ReviewService
/// after a user successfully verifies their email.
/// </summary>
public interface IReviewActivationClient
{
    /// <summary>
    /// Calls POST /api/review/internal/activate-verified-reviews on ReviewService.
    /// Fire-and-forget — never throws. Logs failures.
    /// </summary>
    Task ActivateReviewsForUserAsync(Guid userId);
}
