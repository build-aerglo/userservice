namespace UserService.Application.Interfaces;

public interface IReviewServiceClient
{
    Task<int> GetTotalHelpfulVotesForUserAsync(Guid userId);
    Task<int> GetApprovedReviewCountAsync(Guid userId);
}