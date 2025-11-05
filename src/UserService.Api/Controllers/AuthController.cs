using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Services;
using UserService.Domain.Exceptions;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuth0UserLoginService auth0Login,
    IRefreshTokenCookieService refreshCookie)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest dto)
    {
        try
        {
            var token = await auth0Login.LoginAsync(dto.Email, dto.Password);

            if (token.Refresh_Token == null)
                return Unauthorized(new { error = "Refresh token not returned. Check Auth0 config." });

            refreshCookie.SetRefreshToken(Response, token.Refresh_Token);

            return Ok(new
            {
                access_token = token.Access_Token,
                id_token = token.Id_Token,
                expires_in = token.Expires_In,
                roles = token.Roles
            });
        }
        catch (AuthLoginFailedException ex)
        {
            // Login failed — return clean error to UI
            return Unauthorized(new
            {
                error = "invalid_credentials",
                message = ex.Message // "Wrong email or password"
            });
        }
        catch (Exception ex)
        {
            //  Unexpected error 
            return StatusCode(500, new
            {
                error = "server_error",
                message = "Unexpected error occurred during login."
            });
        }
    }


    // Refresh silently (SPA uses this when access token expires)
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = refreshCookie.GetRefreshToken(Request);

        if (refreshToken is null)
            return Unauthorized("Missing refresh token cookie");

        var token = await auth0Login.RefreshAsync(refreshToken);

        // If Auth0 rotated refresh token — update cookie
        if (!string.IsNullOrWhiteSpace(token.Refresh_Token))
            refreshCookie.SetRefreshToken(Response, token.Refresh_Token);

        return Ok(new
        {
            access_token = token.Access_Token,
            expires_in = token.Expires_In
        });
    }

    //  Logout - clear cookie & revoke token in Auth0 (optional but recommended)
    [AllowAnonymous]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        refreshCookie.ClearRefreshToken(Response);
        return NoContent();
    }
}
