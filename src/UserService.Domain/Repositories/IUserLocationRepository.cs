using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IUserLocationRepository
{
    Task<UserLocation?> GetByIdAsync(Guid id);
    Task<UserLocation?> GetLatestByUserIdAsync(Guid userId);
    Task<IEnumerable<UserLocation>> GetByUserIdAsync(Guid userId, int limit = 100, int offset = 0);
    Task<IEnumerable<UserLocation>> GetByUserIdAndDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<UserLocation>> GetNearbyUsersAsync(decimal latitude, decimal longitude, decimal radiusKm, int limit = 50);
    Task AddAsync(UserLocation location);
    Task DeleteAsync(Guid id);
    Task DeleteOldLocationsAsync(Guid userId, int daysToKeep);
    Task DeleteAllByUserIdAsync(Guid userId);
}
