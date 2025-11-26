using UserService.Domain.Entities;

namespace UserService.Application.DTOs;

public class BusinessSubPubDto
{
    public User UserDto { get; set; }
    public Guid BusinessId { get; set; }
    public BusinessRep BusinessRep { get; set; }
}