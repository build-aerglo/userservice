using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IUserGeolocationRepository
{
    Task<UserGeolocation?> GetByIdAsync(Guid id);
    Task<UserGeolocation?> GetByUserIdAsync(Guid userId);
    Task AddAsync(UserGeolocation geolocation);
    Task UpdateAsync(UserGeolocation geolocation);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<UserGeolocation>> GetByStateAsync(string state, int limit = 100, int offset = 0);
    Task<IEnumerable<UserGeolocation>> GetByLgaAsync(string lga, int limit = 100, int offset = 0);
    Task<int> GetUserCountByStateAsync(string state);
}

public interface IGeolocationHistoryRepository
{
    Task<GeolocationHistory?> GetByIdAsync(Guid id);
    Task<IEnumerable<GeolocationHistory>> GetByUserIdAsync(Guid userId, int limit = 50, int offset = 0);
    Task AddAsync(GeolocationHistory history);
    Task<IEnumerable<GeolocationHistory>> GetByUserIdAndDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate);
    Task<GeolocationHistory?> GetLatestByUserIdAsync(Guid userId);
    Task<int> GetVpnDetectionCountAsync(Guid userId);
    Task DeleteOldHistoryAsync(DateTime cutoffDate);
}
