using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
            => await _userRepository.GetAllAsync();

        public async Task<User?> GetByIdAsync(Guid id)
            => await _userRepository.GetByIdAsync(id);

        public async Task<User> CreateAsync(string username, string email, string phone, string userType, string? address)
        {
            var user = new User(username, email, phone, userType, address);
            await _userRepository.AddAsync(user);
            return user;
        }

        public async Task UpdateAsync(Guid id, string? email, string? phone, string? address)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return;
            user.Update(email, phone, address);
            await _userRepository.UpdateAsync(user);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _userRepository.DeleteAsync(id);
        }
    }
}