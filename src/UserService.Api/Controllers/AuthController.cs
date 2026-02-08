using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.DTOs.Auth;
using UserService.Application.Interfaces;
using UserService.Application.Services.Auth0;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuth0UserLoginService _auth0Login;
    private readonly IAuth0SocialLoginService _socialLogin;
    private readonly IRefreshTokenCookieService _refreshCookie;
    private readonly IUserRepository _userRepository;
    private readonly IPointsService _pointsService;
    private readonly IEmailUpdateRequestRepository _emailUpdateRequestRepository;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuth0UserLoginService auth0Login,
        IAuth0SocialLoginService socialLogin,
        IRefreshTokenCookieService refreshCookie,
        IUserRepository userRepository,
        IPointsService pointsService,
        IEmailUpdateRequestRepository emailUpdateRequestRepository,
        ILogger<AuthController> logger)
    {
        _auth0Login = auth0Login;
        _socialLogin = socialLogin;
        _refreshCookie = refreshCookie;
        _userRepository = userRepository;
        _pointsService = pointsService;
        _emailUpdateRequestRepository = emailUpdateRequestRepository;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest dto)
    {
        try
        {
            var token = await _auth0Login.LoginAsync(dto.Email, dto.Password);

            if (token.Refresh_Token == null)
                return Unauthorized(new { error = "Refresh token not returned. Check Auth0 config." });

            _refreshCookie.SetRefreshToken(Response, token.Refresh_Token);
            
            // Track login for streak
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user != null)
            {
                await _userRepository.UpdateLastLoginAsync(user.Id, DateTime.UtcNow);
    
                try
                {
                    await _pointsService.UpdateLoginStreakAsync(user.Id, DateTime.UtcNow);
                    await _pointsService.CheckAndAwardStreakMilestoneAsync(user.Id);
                }
                catch (Exception ex)
                {
                    // Log but don't fail login if streak update fails
                    _logger?.LogWarning(ex, "Failed to update login streak for user {UserId}", user.Id);
                }
            }
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
        var refreshToken = _refreshCookie.GetRefreshToken(Request);

        if (refreshToken is null)
            return Unauthorized("Missing refresh token cookie");

        var token = await _auth0Login.RefreshAsync(refreshToken);

        if (!string.IsNullOrWhiteSpace(token.Refresh_Token))
            _refreshCookie.SetRefreshToken(Response, token.Refresh_Token);

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
        _refreshCookie.ClearRefreshToken(Response);
        return NoContent();
    }

    // ========================================================================
    // SOCIAL LOGIN ENDPOINTS
    // ========================================================================

    [AllowAnonymous]
    [HttpGet("social/providers")]
    public IActionResult GetSocialProviders()
    {
        var providers = new[]
        {
            new { id = "google-oauth2", name = "Google", icon = "google" },
            new { id = "Facebook", name = "Facebook", icon = "facebook" },
            new { id = "Apple", name = "Apple", icon = "apple" },
            new { id = "GitHub", name = "GitHub", icon = "github" },
            new { id = "Twitter", name = "Twitter/X", icon = "twitter" },
            new { id = "linkedin", name = "LinkedIn", icon = "linkedin" }
        };

        return Ok(providers);
    }

    [AllowAnonymous]
    [HttpPost("social/authorize")]
    public IActionResult GetAuthorizationUrl([FromBody] SocialAuthUrlRequest request)
    {
        try
        {
            var response = _socialLogin.GetAuthorizationUrl(request);
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

    [AllowAnonymous]
    [HttpPost("social/callback")]
    public async Task<IActionResult> SocialLoginCallback([FromBody] SocialLoginRequest request)
    {
        try
        {
            var response = await _socialLogin.AuthenticateAsync(request);

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

    [Authorize]
    [HttpPost("social/link")]
    public async Task<IActionResult> LinkSocialAccount([FromBody] LinkSocialAccountRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { error = "invalid_token", message = "User ID not found in token" });

            var linkedAccount = await _socialLogin.LinkAccountAsync(userId.Value, request);
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

    [Authorize]
    [HttpDelete("social/link/{provider}")]
    public async Task<IActionResult> UnlinkSocialAccount(string provider)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { error = "invalid_token", message = "User ID not found in token" });

            await _socialLogin.UnlinkAccountAsync(userId.Value, provider);
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

    [Authorize]
    [HttpGet("social/linked")]
    public async Task<IActionResult> GetLinkedAccounts()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { error = "invalid_token", message = "User ID not found in token" });

        var accounts = await _socialLogin.GetLinkedAccountsAsync(userId.Value);
        return Ok(accounts);
    }

    // ========================================================================
    // EMAIL UPDATE REQUEST ENDPOINT
    // ========================================================================

    [AllowAnonymous]
    [HttpPost("request-email-update")]
    public async Task<IActionResult> RequestEmailUpdate([FromBody] RequestEmailUpdateDto dto)
    {
        try
        {
            await _emailUpdateRequestRepository.DeleteByBusinessIdAsync(dto.BusinessId);

            var request = new EmailUpdateRequest(dto.BusinessId, dto.EmailAddress, dto.Reason);
            await _emailUpdateRequestRepository.AddAsync(request);

            return Ok(new { message = "Email update request submitted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email update request for business {BusinessId}", dto.BusinessId);
            return StatusCode(500, new { error = "server_error", message = "Unexpected error occurred while processing email update request." });
        }
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