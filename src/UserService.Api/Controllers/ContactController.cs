using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController(
    IContactService contactService,
    ILogger<ContactController> logger
) : ControllerBase
{
    /// <summary>
    /// Sends a contact-us message to the support team.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> ContactUs([FromBody] ContactDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto?.Email))
            return BadRequest(new { error = "Email is required." });

        if (string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest(new { error = "Message is required." });

        try
        {
            var sent = await contactService.SendContactMessageAsync(dto);
            if (!sent)
                return StatusCode(500, new { error = "Failed to send message. Please try again later." });

            return Ok(new { message = "Your message has been sent. We'll be in touch soon." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending contact-us message from {Email}", dto.Email);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
