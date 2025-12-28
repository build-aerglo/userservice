using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.DTOs.Auth;
using UserService.Application.Services;
using UserService.Application.Services.Auth0;
using UserService.Domain.Exceptions;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuth0UserLoginService auth0Login,
    IAuth0SocialLoginService socialLogin,
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
                roles = token.Roles,
                id = token.Id
            });
        }
        catch (AuthLoginFailedException ex)
        {
            return Unauthorized(new
            {
                error = "invalid_credentials",
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "server_error",
                message = "Unexpected error occurred during login."
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = refreshCookie.GetRefreshToken(Request);

        if (refreshToken is null)
            return Unauthorized("Missing refresh token cookie");

        var token = await auth0Login.RefreshAsync(refreshToken);

        if (!string.IsNullOrWhiteSpace(token.Refresh_Token))
            refreshCookie.SetRefreshToken(Response, token.Refresh_Token);

        return Ok(new
        {
            access_token = token.Access_Token,
            expires_in = token.Expires_In
        });
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        refreshCookie.ClearRefreshToken(Response);
        return NoContent();
    }

    // ========================================================================
    // SOCIAL LOGIN ENDPOINTS
    // ========================================================================

    /// <summary>
    /// Get available social login providers
    /// </summary>
    [AllowAnonymous]
    [HttpGet("social/providers")]
    public IActionResult GetSocialProviders()
    {
        var providers = new[]
        {
            new { id = "google-oauth2", name = "Google", icon = "google" },
            new { id = "facebook", name = "Facebook", icon = "facebook" },
            new { id = "apple", name = "Apple", icon = "apple" },
            new { id = "github", name = "GitHub", icon = "github" },
            new { id = "twitter", name = "Twitter/X", icon = "twitter" },
            new { id = "linkedin", name = "LinkedIn", icon = "linkedin" }
        };

        return Ok(providers);
    }

    /// <summary>
    /// Get authorization URL for social login
    /// </summary>
    [AllowAnonymous]
    [HttpPost("social/authorize")]
    public IActionResult GetAuthorizationUrl([FromBody] SocialAuthUrlRequest request)
    {
        try
        {
            var response = socialLogin.GetAuthorizationUrl(request);
            return Ok(response);
        }
        catch (InvalidSocialProviderException ex)
        {
            return BadRequest(new
            {
                error = "invalid_provider",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Complete social login with authorization code
    /// </summary>
    [AllowAnonymous]
    [HttpPost("social/callback")]
    public async Task<IActionResult> SocialLoginCallback([FromBody] SocialLoginRequest request)
    {
        try
        {
            var response = await socialLogin.AuthenticateAsync(request);

            return Ok(new
            {
                access_token = response.AccessToken,
                id_token = response.IdToken,
                expires_in = response.ExpiresIn,
                roles = response.Roles,
                id = response.UserId,
                is_new_user = response.IsNewUser,
                provider = response.Provider,
                email = response.Email,
                name = response.Name
            });
        }
        catch (InvalidSocialProviderException ex)
        {
            return BadRequest(new
            {
                error = "invalid_provider",
                message = ex.Message
            });
        }
        catch (SocialLoginException ex)
        {
            return Unauthorized(new
            {
                error = ex.ErrorCode,
                message = ex.Message,
                provider = ex.Provider
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "server_error",
                message = "Unexpected error occurred during social login."
            });
        }
    }

    /// <summary>
    /// Link a social account to an existing user
    /// </summary>
    [Authorize]
    [HttpPost("social/link")]
    public async Task<IActionResult> LinkSocialAccount([FromBody] LinkSocialAccountRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { error = "invalid_token", message = "User ID not found in token" });

            var linkedAccount = await socialLogin.LinkAccountAsync(userId.Value, request);
            return Ok(linkedAccount);
        }
        catch (InvalidSocialProviderException ex)
        {
            return BadRequest(new
            {
                error = "invalid_provider",
                message = ex.Message
            });
        }
        catch (SocialAccountAlreadyLinkedException ex)
        {
            return Conflict(new
            {
                error = "already_linked",
                message = ex.Message,
                provider = ex.Provider
            });
        }
        catch (SocialLoginException ex)
        {
            return BadRequest(new
            {
                error = ex.ErrorCode,
                message = ex.Message,
                provider = ex.Provider
            });
        }
    }

    /// <summary>
    /// Unlink a social account from the current user
    /// </summary>
    [Authorize]
    [HttpDelete("social/link/{provider}")]
    public async Task<IActionResult> UnlinkSocialAccount(string provider)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { error = "invalid_token", message = "User ID not found in token" });

            await socialLogin.UnlinkAccountAsync(userId.Value, provider);
            return NoContent();
        }
        catch (InvalidSocialProviderException ex)
        {
            return BadRequest(new
            {
                error = "invalid_provider",
                message = ex.Message
            });
        }
        catch (SocialAccountNotLinkedException ex)
        {
            return NotFound(new
            {
                error = "not_linked",
                message = ex.Message,
                provider = ex.Provider
            });
        }
    }

    /// <summary>
    /// Get all linked social accounts for the current user
    /// </summary>
    [Authorize]
    [HttpGet("social/linked")]
    public async Task<IActionResult> GetLinkedAccounts()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { error = "invalid_token", message = "User ID not found in token" });

        var accounts = await socialLogin.GetLinkedAccountsAsync(userId.Value);
        return Ok(accounts);
    }

    private Guid? GetCurrentUserId()
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(subClaim))
            return null;

        if (Guid.TryParse(subClaim, out var userId))
            return userId;

        return null;
    }
}
