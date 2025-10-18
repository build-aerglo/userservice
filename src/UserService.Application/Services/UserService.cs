using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IBusinessRepRepository _businessRepRepository;

        
        public UserService(
            IUserRepository userRepository,
            IBusinessRepRepository businessRepRepository)
        {
            _userRepository = userRepository;
            _businessRepRepository = businessRepRepository;
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

        /// Create Sub BusinessUser 
        public async Task<SubBusinessUserResponseDto> CreateSubBusinessUserAsync(CreateSubBusinessUserDto dto)
        {
         

            //  Check if business exists in database
            // TODO: Replace this with API call to BusinessService when it's ready
            // Example: var businessExists = await _businessServiceClient.CheckBusinessExists(dto.BusinessId);
            var businessExists = await _businessRepRepository.CheckBusinessExistsInDatabase(dto.BusinessId); 
            if (!businessExists)
                throw new InvalidOperationException($"Business with ID {dto.BusinessId} does not exist");


            // Create the user 
            var user = new User(
                username: dto.Username,
                email: dto.Email,
                phone: dto.Phone,
                userType: "business_user",
                address: dto.Address
            );

            // Save user to database
            await _userRepository.AddAsync(user);

            //  Verify user was saved successfully before creating relationship
            var savedUser = await _userRepository.GetByIdAsync(user.Id);
            if (savedUser == null)
                throw new InvalidOperationException("Failed to create user - user not found after save");

            //  Create the business rep relationship
            var businessRep = new BusinessRep(
                businessId: dto.BusinessId,
                userId: user.Id,
                branchName: dto.BranchName,
                branchAddress: dto.BranchAddress
            );

            // Save business rep link to database
            await _businessRepRepository.AddAsync(businessRep);

            // Verify business rep was saved successfully
            var savedBusinessRep = await _businessRepRepository.GetByIdAsync(businessRep.Id);
            if (savedBusinessRep == null)
                throw new InvalidOperationException("Failed to create business representative relationship");

            //  Return response DTO with all the info
            return new SubBusinessUserResponseDto(
                UserId: user.Id,
                BusinessRepId: businessRep.Id,
                BusinessId: businessRep.BusinessId,
                Username: user.Username,
                Email: user.Email,
                Phone: user.Phone,
                Address: user.Address,
                BranchName: businessRep.BranchName,
                BranchAddress: businessRep.BranchAddress,
                CreatedAt: user.CreatedAt
            );
        }  
} 
}