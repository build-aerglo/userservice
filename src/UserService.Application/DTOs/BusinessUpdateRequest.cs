using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs;

public class BusinessUpdateRequest
{
    public required string Name { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    public string? Website { get; set; }
    public string? Description { get; set; }

    public required Guid CategoryId { get; set; }

    public required List<Guid> TagIds { get; set; }
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }

}