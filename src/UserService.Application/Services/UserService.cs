using System.Net.Http.Json;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IBusinessRepRepository _businessRepRepository;
        private readonly HttpClient _httpClient;

        public UserService(IUserRepository userRepository, IBusinessRepRepository businessRepRepository, HttpClient httpClient)
        {
            _userRepository = userRepository;
            _businessRepRepository = businessRepRepository;
            _httpClient = httpClient;
        }
        

        public async Task<(User user, Guid businessId)> RegisterBusinessAccountAsync(User userPayload)
        {
            // Step 1: Call BusinessService API
            var response = await _httpClient.PostAsJsonAsync("https://business-service/api/businesses", userPayload);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BusinessCreatedResponse>();

            // Step 2: Save User
            await _userRepository.AddAsync(userPayload);

            // Step 3: Save BusinessRep
            var businessRep = new BusinessRep(result.BusinessId, userPayload.Id, null, null);
            await _businessRepRepository.AddAsync(businessRep);

            return (userPayload, result.BusinessId);
        }
    }
    public class BusinessCreatedResponse
    {
        public Guid BusinessId { get; set; }
    }
}