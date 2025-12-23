namespace UserService.Application.DTOs;

public record UserDto(
    string Username,
    string Email,
    string Phone,
    string UserType,
    string? Address
);

public record UpdateUserDto(
    string? Email,
    string? Phone,
    string? Address
);


//sub business User

public record CreateSubBusinessUserDto(
    Guid BusinessId,           
    string Username,          
    string Email,              
    string Phone,             
    string? Address,         
    string? BranchName,
    string? Password,
    string? BranchAddress     
);


public record UpdateSubBusinessUserDto(
    string? Email,
    string? Phone,
    string? Address,
    string? BranchName,
    string? BranchAddress
);


public record SubBusinessUserResponseDto(
    Guid UserId,              
    Guid BusinessRepId,       
    Guid BusinessId,           
    string Username,
    string Email,
    string Phone,
    string? Address,
    string? BranchName,
    string? BranchAddress,
    string Auth0UserId,
    DateTime CreatedAt
);



// Support users Dtos for requets and response
public record CreateSupportUserDto(
    string Username,
    string Email,
    string Password,
    string Phone,
    string? Address
);


public record SupportUserResponseDto(
    Guid UserId,
    Guid SupportUserProfileId,
    string Username,
    string Email,
    string Phone,
    string? Address,
    string Auth0UserId,
    DateTime CreatedAt
);