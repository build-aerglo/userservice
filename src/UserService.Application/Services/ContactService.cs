using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;

namespace UserService.Application.Services;

public class ContactService(
    INotificationServiceClient notificationClient,
    IConfiguration config,
    ILogger<ContactService> logger
) : IContactService
{
    public async Task<bool> SendContactMessageAsync(ContactDto dto)
    {
        var recipient = config["Services:ContactEmail"] ?? "contact@clereview.com";
        if (string.IsNullOrWhiteSpace(recipient))
        {
            logger.LogError("Services:ContactEmail is not configured. Cannot send contact-us message from {Email}", dto.Email);
            return false;
        }

        logger.LogInformation("Sending contact-us message from {Email} to {Recipient}", dto.Email, recipient);

        var sent = await notificationClient.SendNotificationAsync(
            template: "contact-us",
            recipient: recipient,
            channel: "email",
            payload: new { name = dto.Name, email = dto.Email, subject = dto.Subject, message = dto.Message }
        );

        if (!sent)
            logger.LogWarning("Failed to deliver contact-us message from {Email}", dto.Email);

        return sent;
    }
}
