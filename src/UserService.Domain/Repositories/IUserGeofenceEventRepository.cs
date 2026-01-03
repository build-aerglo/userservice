using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IUserGeofenceEventRepository
{
    Task<UserGeofenceEvent?> GetByIdAsync(Guid id);
    Task<IEnumerable<UserGeofenceEvent>> GetByUserIdAsync(Guid userId, int limit = 100);
    Task<IEnumerable<UserGeofenceEvent>> GetByGeofenceIdAsync(Guid geofenceId, int limit = 100);
    Task<IEnumerable<UserGeofenceEvent>> GetByUserIdAndDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate);
    Task<UserGeofenceEvent?> GetLatestByUserAndGeofenceAsync(Guid userId, Guid geofenceId);
    Task AddAsync(UserGeofenceEvent geofenceEvent);
    Task DeleteOldEventsAsync(int daysToKeep);
}
