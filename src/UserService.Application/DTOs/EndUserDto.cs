using System.Text.Json;
using UserService.Domain.Entities;

namespace UserService.Application.DTOs;

public record EndUserDto(
    JsonDocument? Preferences,
    string? Bio,
    JsonDocument? SocialLinks,
    string Username,
    string Email,
    string Phone,
    string UserType,
    string? Address
    );