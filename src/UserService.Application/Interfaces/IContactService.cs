using UserService.Application.DTOs;

namespace UserService.Application.Interfaces;

public interface IContactService
{
    Task<bool> SendContactMessageAsync(ContactDto dto);
}
