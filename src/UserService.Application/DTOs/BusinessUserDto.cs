using System.Text.Json.Serialization;

namespace UserService.Application.DTOs;

public record BusinessUserDto(
    string Name,
    string Email,
    string Password,
    string Phone,
    string UserType,
    string? Address,
    string? BranchName,
    string? BranchAddress,
    string? Website,
    List<string> CategoryIds
)
{
    public BusinessUserDto() : this("", "", "", "", "",  null, null, null, null, new List<string>()) { }
};

public record BusinessFetchResponseDto
(
    Guid id
);