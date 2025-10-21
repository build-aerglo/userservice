// using System.Net;
// using System.Net.Http.Json;
// using Moq;
// using Moq.Protected;
// using UserService.Application.DTOs;
// using UserService.Application.Interfaces;
// using UserService.Domain.Entities;
// using UserService.Domain.Repositories;
// using Microsoft.Extensions.Configuration;
//
// namespace UserService.Application.Tests.Services;
//
// [TestFixture]
// public class BusinessServiceTests
// {
//     private Mock<IUserRepository> _mockUserRepository = null!;
//     private Mock<IBusinessRepRepository> _mockBusinessRepRepository = null!;
//     private Mock<IBusinessServiceClient> _mockBusinessServiceClient = null!;
//     private IConfiguration _config = null!;
//     private Application.Services.UserService _service = null!;   
//     
//     [SetUp]
//     public void Setup()
//     {
//         _mockUserRepository = new Mock<IUserRepository>();
//         _mockBusinessRepRepository = new Mock<IBusinessRepRepository>();
//         _mockBusinessServiceClient = new Mock<IBusinessServiceClient>();
//         _service = new Application.Services.UserService(_mockUserRepository.Object, _mockBusinessRepRepository.Object, _mockBusinessServiceClient.Object, _mockBusinessRepository.Object, httpClient: new HttpClient(), _config);
//     }
//     
//     [Test]
//     public async Task RegisterBusinessAccountAsync()
//         {
//             // Arrange
//             var bId = Guid.NewGuid();
//             var userPayload = new BusinessUserDto
//             {
//                 Username = "sam",
//                 Email = "sam@example.com",
//                 Phone = "1234",
//                 UserType = "business_rep",
//                 Address = "Lagos",
//                 BranchAddress = "Branch Address",
//                 BranchName = "Main Branch"
//             };
//             
//             // create user
//             _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
//             
//             // confirm id exists
//             _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) =>
//                 new User("sam", "sam@example.com", "1234", "business_rep", "Lagos"));
//             
//             // add business
//             _mockBusinessRepository.Setup(r => r.AddAsync(It.IsAny<BusinessUser>())).Returns(Task.CompletedTask);
//             
//             // confirms business was added
//             _mockBusinessRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
//                 .ReturnsAsync((Guid id) => new BusinessUser(Guid.NewGuid(), bId, "Branch Address", "Branch Name"));
//             
//             
//             
//             // Act
//             var (user, businessId, businessRep) = await _service.RegisterBusinessAccountAsync(userPayload);
//             
//             // Assert
//             Assert.That(user, Is.Not.Null);
//             Assert.That(businessId, Is.EqualTo(bId));
//             Assert.That(businessRep.BranchName, Is.EqualTo(userPayload.BranchName));
//         }
// }