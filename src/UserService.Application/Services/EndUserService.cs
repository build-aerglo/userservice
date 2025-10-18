using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Services
{
    public class EndUserService : IEndUserService
    {
        private readonly IEndUserRepository _endUserRepository;
        private readonly IUserRepository _userRepository;

        public EndUserService(IEndUserRepository endUserRepository, IUserRepository userRepository)
        {
            _endUserRepository = endUserRepository;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<EndUser>> GetAllAsync()
            => await _endUserRepository.GetAllAsync();

        public async Task<EndUser?> GetByIdAsync(Guid id)
            => await _endUserRepository.GetByIdAsync(id);

        public async Task<EndUser?> GetByUserIdAsync(Guid userId)
            => await _endUserRepository.GetByUserIdAsync(userId);

        public async Task<EndUser> CreateAsync(EndUserDto endUser)
        {
            var user = new User(endUser.Username, endUser.Email, endUser.Phone, endUser.UserType, endUser.Address);
            await _userRepository.AddAsync(user);
            
            // confirm save
            var savedUser = await _userRepository.GetByIdAsync(user.Id);
            if (savedUser == null) throw new InvalidOperationException("Failed to create user - user not found after save");

            var createEndUser = new EndUser(user.Id, endUser.Preferences, endUser.Bio, endUser.SocialLinks);
            await _endUserRepository.AddAsync(createEndUser);
            return createEndUser;
        }

        public async Task UpdateAsync(Guid id, string? preferences, string? bio, string? socialLinks)
        {
            var profile = await _endUserRepository.GetByIdAsync(id);
            if (profile == null) return;

            profile.Update(preferences, bio, socialLinks);
            await _endUserRepository.UpdateAsync(profile);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _endUserRepository.DeleteAsync(id);
        }
    }
}