using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IGeofenceRepository
{
    Task<Geofence?> GetByIdAsync(Guid id);
    Task<IEnumerable<Geofence>> GetAllAsync();
    Task<IEnumerable<Geofence>> GetActiveAsync();
    Task<IEnumerable<Geofence>> GetNearbyAsync(decimal latitude, decimal longitude, decimal radiusKm);
    Task<IEnumerable<Geofence>> GetContainingPointAsync(decimal latitude, decimal longitude);
    Task AddAsync(Geofence geofence);
    Task UpdateAsync(Geofence geofence);
    Task DeleteAsync(Guid id);
}
