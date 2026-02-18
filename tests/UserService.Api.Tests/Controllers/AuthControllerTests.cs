using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Application.DTOs.Auth;
using UserService.Application.DTOs.Points;
using UserService.Application.Interfaces;
using UserService.Application.Services.Auth0;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;


[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuth0UserLoginService _auth0Login;
    private readonly IAuth0SocialLoginService _socialLogin;
    private readonly IRefreshTokenCookieService _refreshCookie;
    private readonly IUserRepository _userRepository;
    private readonly IPointsService _pointsService;
    private readonly ILogger<AuthController> _logger;
    private readonly IEmailUpdateRequestRepository _mockEmailUpdateRequestRepository;

    public AuthController(
        IAuth0UserLoginService auth0Login,
        IAuth0SocialLoginService socialLogin,
        IRefreshTokenCookieService refreshCookie,
        IUserRepository userRepository,
        IPointsService pointsService,
        ILogger<AuthController> logger,
        IEmailUpdateRequestRepository mockEmailUpdateRequestRepository)
    {
        _auth0Login = auth0Login ?? throw new ArgumentNullException(nameof(auth0Login));
        _socialLogin = socialLogin ?? throw new ArgumentNullException(nameof(socialLogin));
        _refreshCookie = refreshCookie ?? throw new ArgumentNullException(nameof(refreshCookie));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _pointsService = pointsService ?? throw new ArgumentNullException(nameof(pointsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mockEmailUpdateRequestRepository = mockEmailUpdateRequestRepository ?? throw new ArgumentNullException(nameof(mockEmailUpdateRequestRepository));
    }

    // ========================================================================
    // STANDARD AUTH ENDPOINTS
    // ========================================================================

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest dto)
    {
        // --- 400: Null body ---
        if (dto == null)
            return BadRequest(ErrorResponse("invalid_request", "Please provide your login details."));

        // --- 400: Missing email ---
        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(ErrorResponse("email_required", "Please enter your email address."));

        // --- 400: Invalid email format ---
        if (!IsValidEmail(dto.Email))
            return BadRequest(ErrorResponse("invalid_email", "Please enter a valid email address."));

        // --- 400: Missing password ---
        if (string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(ErrorResponse("password_required", "Please enter your password."));

        // --- 400: Password too short ---
        if (dto.Password.Length < 6)
            return BadRequest(ErrorResponse("invalid_password", "Password must be at least 6 characters."));

        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", MaskEmail(dto.Email));

            var token = await _auth0Login.LoginAsync(dto.Email, dto.Password);

            // --- 502: Auth provider returned nothing ---
            if (token == null)
            {
                _logger.LogError("Authentication provider returned null for email: {Email}", MaskEmail(dto.Email));
                return StatusCode(502, ErrorResponse("service_error", "We're having trouble connecting to our sign-in service. Please try again in a moment."));
            }

            // --- 401: No refresh token (misconfiguration) ---
            if (token.Refresh_Token == null)
            {
                _logger.LogWarning("No refresh token returned for email: {Email}. Check Auth0 offline_access scope and rotation settings.", MaskEmail(dto.Email));
                return Unauthorized(ErrorResponse("login_incomplete", "Login could not be completed. Please try again or contact support if the issue persists."));
            }

            _refreshCookie.SetRefreshToken(Response, token.Refresh_Token);

            // Track login streak — non-critical, must not fail the login
            await TrackLoginStreakAsync(dto.Email);

            _logger.LogInformation("Successful login for email: {Email}", MaskEmail(dto.Email));

            return Ok(new
            {
                access_token = token.Access_Token,
                id_token = token.Id_Token,
                expires_in = token.Expires_In,
                roles = token.Roles,
                id = token.Id
            });
        }
        // --- 401: Wrong email or password ---
        catch (AuthLoginFailedException ex)
        {
            _logger.LogWarning("Login failed for email: {Email}. Reason: {Reason}", MaskEmail(dto.Email), ex.Message);
            return Unauthorized(ErrorResponse("invalid_credentials", "The email or password you entered is incorrect. Please try again."));
        }
        // --- 403: Account blocked/disabled ---
        catch (AccountBlockedException ex)
        {
            _logger.LogWarning("Blocked account login attempt for email: {Email}. Reason: {Reason}", MaskEmail(dto.Email), ex.Message);
            return StatusCode(403, ErrorResponse("account_blocked", "Your account has been temporarily locked due to too many failed login attempts. Please try again later or reset your password."));
        }
        // --- 403: Email not verified ---
        catch (EmailNotVerifiedException ex)
        {
            _logger.LogWarning("Unverified email login attempt: {Email}. Reason: {Reason}", MaskEmail(dto.Email), ex.Message);
            return StatusCode(403, ErrorResponse("email_not_verified", "Please verify your email address before logging in. Check your inbox for a verification link."));
        }
        // --- 429: Too many login attempts ---
        catch (RateLimitExceededException ex)
        {
            _logger.LogWarning("Rate limit exceeded for login. Email: {Email}. RetryAfter: {RetryAfter}", MaskEmail(dto.Email), ex.RetryAfterSeconds);
            if (ex.RetryAfterSeconds > 0)
                Response.Headers["Retry-After"] = ex.RetryAfterSeconds.ToString();
            return StatusCode(429, ErrorResponse("too_many_attempts", "You've made too many login attempts. Please wait a few minutes before trying again."));
        }
        // --- 502: Network error reaching auth provider ---
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during login for email: {Email}", MaskEmail(dto.Email));
            return StatusCode(502, ErrorResponse("service_unavailable", "We're having trouble connecting to our sign-in service. Please try again in a moment."));
        }
        // --- 504: Auth provider timeout ---
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout during login for email: {Email}", MaskEmail(dto.Email));
            return StatusCode(504, ErrorResponse("request_timeout", "The login request took too long to process. Please try again."));
        }
        // --- 499: Client disconnected ---
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Login request cancelled for email: {Email}", MaskEmail(dto.Email));
            return StatusCode(499, ErrorResponse("request_cancelled", "The request was cancelled."));
        }
        // --- 500: Catch-all ---
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for email: {Email}", MaskEmail(dto.Email));
            return StatusCode(500, ErrorResponse("server_error", "Something went wrong on our end. Please try again later."));
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        try
        {
            var refreshToken = _refreshCookie.GetRefreshToken(Request);

            // --- 401: No refresh token cookie ---
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("Refresh attempt with missing or empty refresh token cookie.");
                return Unauthorized(ErrorResponse("session_expired", "Your session has expired. Please log in again."));
            }

            _logger.LogInformation("Token refresh attempt.");

            var token = await _auth0Login.RefreshAsync(refreshToken);

            // --- 502: Auth provider returned nothing ---
            if (token == null)
            {
                _logger.LogError("Authentication provider returned null during token refresh.");
                return StatusCode(502, ErrorResponse("service_error", "We're having trouble refreshing your session. Please log in again."));
            }

            if (!string.IsNullOrWhiteSpace(token.Refresh_Token))
                _refreshCookie.SetRefreshToken(Response, token.Refresh_Token);

            _logger.LogInformation("Token refresh successful.");

            return Ok(new
            {
                access_token = token.Access_Token,
                expires_in = token.Expires_In
            });
        }
        // --- 401: Refresh token expired or revoked ---
        catch (AuthLoginFailedException ex)
        {
            _logger.LogWarning("Token refresh failed — token expired or revoked. Reason: {Reason}", ex.Message);
            _refreshCookie.ClearRefreshToken(Response);
            return Unauthorized(ErrorResponse("session_expired", "Your session has expired. Please log in again."));
        }
        // --- 429: Rate limited ---
        catch (RateLimitExceededException ex)
        {
            _logger.LogWarning("Rate limit exceeded during token refresh. RetryAfter: {RetryAfter}", ex.RetryAfterSeconds);
            if (ex.RetryAfterSeconds > 0)
                Response.Headers["Retry-After"] = ex.RetryAfterSeconds.ToString();
            return StatusCode(429, ErrorResponse("too_many_attempts", "Too many requests. Please wait a moment before trying again."));
        }
        // --- 502: Network error ---
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during token refresh.");
            return StatusCode(502, ErrorResponse("service_unavailable", "We're having trouble connecting. Please try again in a moment."));
        }
        // --- 504: Timeout ---
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout during token refresh.");
            return StatusCode(504, ErrorResponse("request_timeout", "The request took too long. Please try again."));
        }
        // --- 499: Client disconnected ---
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Token refresh request cancelled.");
            return StatusCode(499, ErrorResponse("request_cancelled", "The request was cancelled."));
        }
        // --- 500: Catch-all ---
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token refresh.");
            return StatusCode(500, ErrorResponse("server_error", "Something went wrong on our end. Please try again later."));
        }
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        try
        {
            _refreshCookie.ClearRefreshToken(Response);
            _logger.LogInformation("User logged out successfully.");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout — clearing cookie on best-effort basis.");
            return NoContent();
        }
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
        // --- 400: Null body ---
        if (request == null)
            return BadRequest(ErrorResponse("invalid_request", "Please select a social login provider."));

        // --- 400: Missing provider ---
        if (string.IsNullOrWhiteSpace(request.Provider))
            return BadRequest(ErrorResponse("provider_required", "Please select a social login provider."));

        try
        {
            _logger.LogInformation("Social authorization URL requested for provider: {Provider}", request.Provider);

            var response = _socialLogin.GetAuthorizationUrl(request);
            return Ok(response);
        }
        // --- 400: Unsupported provider ---
        catch (InvalidSocialProviderException ex)
        {
            _logger.LogWarning("Invalid social provider requested: {Provider}. Reason: {Reason}", request.Provider, ex.Message);
            return BadRequest(ErrorResponse("unsupported_provider", $"The provider '{request.Provider}' is not supported. Please choose from the available options."));
        }
        // --- 500: Catch-all ---
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating social authorization URL for provider: {Provider}", request.Provider);
            return StatusCode(500, ErrorResponse("server_error", "Something went wrong while setting up social login. Please try again."));
        }
    }

    [AllowAnonymous]
    [HttpPost("social/callback")]
    public async Task<IActionResult> SocialLoginCallback([FromBody] SocialLoginRequest request)
    {
        // --- 400: Null body ---
        if (request == null)
            return BadRequest(ErrorResponse("invalid_request", "Social login information is missing. Please try again."));

        // --- 400: Missing provider ---
        if (string.IsNullOrWhiteSpace(request.Provider))
            return BadRequest(ErrorResponse("provider_required", "Social login provider is required."));

        // --- 400: Missing authorization code ---
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(ErrorResponse("code_required", "Authorization was not completed. Please try signing in again."));

        try
        {
            _logger.LogInformation("Social login callback for provider: {Provider}", request.Provider);

            var response = await _socialLogin.AuthenticateAsync(request);

            // --- 502: Null response from provider ---
            if (response == null)
            {
                _logger.LogError("Social login service returned null for provider: {Provider}", request.Provider);
                return StatusCode(502, ErrorResponse("service_error", "We couldn't complete the sign-in with your social account. Please try again."));
            }

            _logger.LogInformation("Social login successful — provider: {Provider}, userId: {UserId}, isNew: {IsNew}",
                response.Provider, response.UserId, response.IsNewUser);

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
        // --- 400: Unsupported provider ---
        catch (InvalidSocialProviderException ex)
        {
            _logger.LogWarning("Social login failed — invalid provider: {Provider}. Reason: {Reason}", request.Provider, ex.Message);
            return BadRequest(ErrorResponse("unsupported_provider", $"The provider '{request.Provider}' is not supported."));
        }
        // --- 409: Email already registered with different provider ---
        catch (EmailAlreadyRegisteredException ex)
        {
            _logger.LogWarning("Social login conflict — email already registered. Provider: {Provider}, Email: {Email}", request.Provider, MaskEmail(ex.Email));
            return Conflict(ErrorResponse("email_already_registered",
                "An account with this email already exists. Please log in with your original method or link this social account from your profile settings."));
        }
        // --- 401: Social auth failed (bad code, denied, etc.) ---
        catch (SocialLoginException ex)
        {
            _logger.LogWarning("Social login failed — provider: {Provider}, errorCode: {ErrorCode}, reason: {Reason}",
                ex.Provider, ex.ErrorCode, ex.Message);
            return Unauthorized(new
            {
                error = ex.ErrorCode,
                message = MapSocialLoginError(ex.ErrorCode),
                provider = ex.Provider
            });
        }
        // --- 403: Social account blocked ---
        catch (AccountBlockedException ex)
        {
            _logger.LogWarning("Blocked account social login attempt. Provider: {Provider}. Reason: {Reason}", request.Provider, ex.Message);
            return StatusCode(403, ErrorResponse("account_blocked", "Your account has been suspended. Please contact support for assistance."));
        }
        // --- 429: Rate limited ---
        catch (RateLimitExceededException ex)
        {
            _logger.LogWarning("Rate limit exceeded during social login callback. Provider: {Provider}", request.Provider);
            if (ex.RetryAfterSeconds > 0)
                Response.Headers["Retry-After"] = ex.RetryAfterSeconds.ToString();
            return StatusCode(429, ErrorResponse("too_many_attempts", "Too many sign-in attempts. Please wait a few minutes before trying again."));
        }
        // --- 502: Network error ---
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during social login callback. Provider: {Provider}", request.Provider);
            return StatusCode(502, ErrorResponse("service_unavailable", "We're having trouble connecting to the sign-in service. Please try again in a moment."));
        }
        // --- 504: Timeout ---
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout during social login callback. Provider: {Provider}", request.Provider);
            return StatusCode(504, ErrorResponse("request_timeout", "The sign-in request took too long. Please try again."));
        }
        // --- 499: Client disconnected ---
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Social login callback cancelled. Provider: {Provider}", request.Provider);
            return StatusCode(499, ErrorResponse("request_cancelled", "The request was cancelled."));
        }
        // --- 500: Catch-all ---
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during social login callback. Provider: {Provider}", request.Provider);
            return StatusCode(500, ErrorResponse("server_error", "Something went wrong during sign-in. Please try again later."));
        }
    }

    [Authorize]
    [HttpPost("social/link")]
    public async Task<IActionResult> LinkSocialAccount([FromBody] LinkSocialAccountRequest request)
    {
        // --- 400: Null body ---
        if (request == null)
            return BadRequest(ErrorResponse("invalid_request", "Please select a social account to link."));

        // --- 400: Missing provider ---
        if (string.IsNullOrWhiteSpace(request.Provider))
            return BadRequest(ErrorResponse("provider_required", "Please select which social account to link."));

        try
        {
            var userId = GetCurrentUserId();

            // --- 401: No valid user in token ---
            if (userId == null)
                return Unauthorized(ErrorResponse("authentication_required", "Please log in again to link a social account."));

            _logger.LogInformation("User {UserId} linking social account: {Provider}", userId, request.Provider);

            var linkedAccount = await _socialLogin.LinkAccountAsync(userId.Value, request);

            // --- 502: Null response ---
            if (linkedAccount == null)
            {
                _logger.LogError("LinkAccountAsync returned null for user {UserId}, provider: {Provider}", userId, request.Provider);
                return StatusCode(502, ErrorResponse("service_error", "We couldn't link your social account. Please try again."));
            }

            _logger.LogInformation("User {UserId} successfully linked: {Provider}", userId, request.Provider);
            return Ok(linkedAccount);
        }
        // --- 400: Unsupported provider ---
        catch (InvalidSocialProviderException ex)
        {
            _logger.LogWarning("Invalid provider during account linking: {Reason}", ex.Message);
            return BadRequest(ErrorResponse("unsupported_provider", $"The provider '{request.Provider}' is not supported."));
        }
        // --- 409: Already linked ---
        catch (SocialAccountAlreadyLinkedException ex)
        {
            _logger.LogWarning("Social account already linked — provider: {Provider}. Reason: {Reason}", ex.Provider, ex.Message);
            return Conflict(new
            {
                error = "already_linked",
                message = $"Your {ex.Provider} account is already linked to your profile.",
                provider = ex.Provider
            });
        }
        // --- 409: Email conflict ---
        catch (EmailAlreadyRegisteredException ex)
        {
            _logger.LogWarning("Cannot link — email from social account already used by another user. Email: {Email}", MaskEmail(ex.Email));
            return Conflict(ErrorResponse("email_conflict", "The email associated with this social account is already in use by another account."));
        }
        // --- 400/401: General social login failure during linking ---
        catch (SocialLoginException ex)
        {
            _logger.LogWarning("Social login error during account linking — provider: {Provider}, errorCode: {ErrorCode}, reason: {Reason}",
                ex.Provider, ex.ErrorCode, ex.Message);
            return BadRequest(new
            {
                error = ex.ErrorCode,
                message = "We couldn't link your social account. Please try again.",
                provider = ex.Provider
            });
        }
        // --- 429: Rate limited ---
        catch (RateLimitExceededException ex)
        {
            _logger.LogWarning("Rate limit exceeded during account linking. Provider: {Provider}", request.Provider);
            if (ex.RetryAfterSeconds > 0)
                Response.Headers["Retry-After"] = ex.RetryAfterSeconds.ToString();
            return StatusCode(429, ErrorResponse("too_many_attempts", "Too many requests. Please wait a moment before trying again."));
        }
        // --- 502: Network error ---
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during social account linking. Provider: {Provider}", request.Provider);
            return StatusCode(502, ErrorResponse("service_unavailable", "We're having trouble connecting. Please try again in a moment."));
        }
        // --- 504: Timeout ---
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout during social account linking. Provider: {Provider}", request.Provider);
            return StatusCode(504, ErrorResponse("request_timeout", "The request took too long. Please try again."));
        }
        // --- 499: Client disconnected ---
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Social account linking cancelled. Provider: {Provider}", request.Provider);
            return StatusCode(499, ErrorResponse("request_cancelled", "The request was cancelled."));
        }
        // --- 500: Catch-all ---
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during social account linking. Provider: {Provider}", request.Provider);
            return StatusCode(500, ErrorResponse("server_error", "Something went wrong while linking your account. Please try again later."));
        }
    }

    [Authorize]
    [HttpDelete("social/link/{provider}")]
    public async Task<IActionResult> UnlinkSocialAccount(string provider)
    {
        // --- 400: Empty provider ---
        if (string.IsNullOrWhiteSpace(provider))
            return BadRequest(ErrorResponse("provider_required", "Please specify which social account to unlink."));

        try
        {
            var userId = GetCurrentUserId();

            // --- 401: No valid user in token ---
            if (userId == null)
                return Unauthorized(ErrorResponse("authentication_required", "Please log in again to manage your linked accounts."));

            _logger.LogInformation("User {UserId} unlinking social account: {Provider}", userId, provider);

            await _socialLogin.UnlinkAccountAsync(userId.Value, provider);

            _logger.LogInformation("User {UserId} successfully unlinked: {Provider}", userId, provider);
            return NoContent();
        }
        // --- 400: Unsupported provider ---
        catch (InvalidSocialProviderException ex)
        {
            _logger.LogWarning("Invalid provider during account unlinking: {Provider}. Reason: {Reason}", provider, ex.Message);
            return BadRequest(ErrorResponse("unsupported_provider", $"The provider '{provider}' is not supported."));
        }
        // --- 404: Not linked ---
        catch (SocialAccountNotLinkedException ex)
        {
            _logger.LogWarning("Tried to unlink non-linked provider: {Provider}. Reason: {Reason}", ex.Provider, ex.Message);
            return NotFound(new
            {
                error = "not_linked",
                message = $"No {ex.Provider} account is currently linked to your profile.",
                provider = ex.Provider
            });
        }
        // --- 400: Cannot unlink last auth method ---
        catch (CannotUnlinkLastAuthMethodException ex)
        {
            _logger.LogWarning("User attempted to unlink last auth method. Provider: {Provider}. Reason: {Reason}", provider, ex.Message);
            return BadRequest(ErrorResponse("cannot_unlink", "You can't remove your last sign-in method. Please add another way to log in first."));
        }
        // --- 429: Rate limited ---
        catch (RateLimitExceededException ex)
        {
            _logger.LogWarning("Rate limit exceeded during account unlinking. Provider: {Provider}", provider);
            if (ex.RetryAfterSeconds > 0)
                Response.Headers["Retry-After"] = ex.RetryAfterSeconds.ToString();
            return StatusCode(429, ErrorResponse("too_many_attempts", "Too many requests. Please wait a moment before trying again."));
        }
        // --- 502: Network error ---
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during social account unlinking. Provider: {Provider}", provider);
            return StatusCode(502, ErrorResponse("service_unavailable", "We're having trouble connecting. Please try again in a moment."));
        }
        // --- 504: Timeout ---
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout during social account unlinking. Provider: {Provider}", provider);
            return StatusCode(504, ErrorResponse("request_timeout", "The request took too long. Please try again."));
        }
        // --- 499: Client disconnected ---
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Social account unlinking cancelled. Provider: {Provider}", provider);
            return StatusCode(499, ErrorResponse("request_cancelled", "The request was cancelled."));
        }
        // --- 500: Catch-all ---
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during social account unlinking. Provider: {Provider}", provider);
            return StatusCode(500, ErrorResponse("server_error", "Something went wrong while unlinking your account. Please try again later."));
        }
    }

    [Authorize]
    [HttpGet("social/linked")]
    public async Task<IActionResult> GetLinkedAccounts()
    {
        try
        {
            var userId = GetCurrentUserId();

            // --- 401: No valid user in token ---
            if (userId == null)
                return Unauthorized(ErrorResponse("authentication_required", "Please log in to view your linked accounts."));

            _logger.LogInformation("User {UserId} fetching linked social accounts.", userId);

            var accounts = await _socialLogin.GetLinkedAccountsAsync(userId.Value);
            return Ok(accounts ?? Enumerable.Empty<LinkedSocialAccountDto>());
        }
        // --- 429: Rate limited ---
        catch (RateLimitExceededException ex)
        {
            _logger.LogWarning("Rate limit exceeded fetching linked accounts.");
            if (ex.RetryAfterSeconds > 0)
                Response.Headers["Retry-After"] = ex.RetryAfterSeconds.ToString();
            return StatusCode(429, ErrorResponse("too_many_attempts", "Too many requests. Please wait a moment before trying again."));
        }
        // --- 502: Network error ---
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error fetching linked social accounts.");
            return StatusCode(502, ErrorResponse("service_unavailable", "We're having trouble loading your linked accounts. Please try again."));
        }
        // --- 504: Timeout ---
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout fetching linked social accounts.");
            return StatusCode(504, ErrorResponse("request_timeout", "The request took too long. Please try again."));
        }
        // --- 499: Client disconnected ---
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Fetching linked accounts was cancelled.");
            return StatusCode(499, ErrorResponse("request_cancelled", "The request was cancelled."));
        }
        // --- 500: Catch-all ---
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching linked social accounts.");
            return StatusCode(500, ErrorResponse("server_error", "Something went wrong while loading your linked accounts. Please try again later."));
        }
    }

    // ========================================================================
    // PRIVATE HELPERS
    // ========================================================================

    private Guid? GetCurrentUserId()
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(subClaim))
        {
            _logger.LogWarning("No NameIdentifier or sub claim found in token. Claims present: {Claims}",
                string.Join(", ", User.Claims.Select(c => c.Type)));
            return null;
        }

        if (Guid.TryParse(subClaim, out var userId))
            return userId;

        _logger.LogWarning("Could not parse user ID from claim value: {ClaimValue}", subClaim);
        return null;
    }

    private async Task TrackLoginStreakAsync(string email)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("User not found in local DB for streak tracking. Email: {Email}", MaskEmail(email));
                return;
            }

            await _userRepository.UpdateLastLoginAsync(user.Id, DateTime.UtcNow);

            try
            {
                await _pointsService.UpdateLoginStreakAsync(user.Id, DateTime.UtcNow);
                await _pointsService.CheckAndAwardStreakMilestoneAsync(user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update login streak/milestone for user {UserId}", user.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to track login streak. Email: {Email}", MaskEmail(email));
        }
    }

    /// <summary>
    /// Maps internal social login error codes to user-friendly messages.
    /// </summary>
    private static string MapSocialLoginError(string errorCode) => errorCode switch
    {
        "access_denied" => "You declined the sign-in request. Please try again if this was a mistake.",
        "invalid_grant" => "The sign-in session has expired. Please try again.",
        "consent_required" => "Permission is required to continue. Please approve access when prompted.",
        "interaction_required" => "Additional verification is needed. Please try signing in again.",
        "account_suspended" => "This social account has been suspended by the provider.",
        "token_exchange_failed" => "We couldn't complete the sign-in. Please try again.",
        _ => "Sign-in with this social account failed. Please try again or use a different method."
    };

    /// <summary>
    /// Creates a consistent error response for the frontend.
    /// </summary>
    private static object ErrorResponse(string error, string message) => new { error, message };

    /// <summary>
    /// Masks email for safe logging (e.g., "t***@example.com").
    /// </summary>
    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "[empty]";

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return "***";

        return $"{email[0]}***{email[atIndex..]}";
    }

    /// <summary>
    /// Basic email format validation.
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return false;

        var dotIndex = email.LastIndexOf('.');
        return dotIndex > atIndex + 1 && dotIndex < email.Length - 1;
    }

    // ========================================================================
    // REQUEST EMAIL UPDATE TESTS
    // ========================================================================

    [Test]
    public async Task RequestEmailUpdate_Success_ShouldDeleteExistingAndInsertNew()
    {
        // ARRANGE
        var businessId = Guid.NewGuid();
        var dto = new RequestEmailUpdateDto
        {
            BusinessId = businessId,
            EmailAddress = "newemail@example.com",
            Reason = "Changing business email"
        };

        _mockEmailUpdateRequestRepository
            .Setup(r => r.DeleteByBusinessIdAsync(businessId))
            .Returns(Task.CompletedTask);
        _mockEmailUpdateRequestRepository
            .Setup(r => r.AddAsync(It.IsAny<EmailUpdateRequest>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _controller.RequestEmailUpdate(dto);

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _mockEmailUpdateRequestRepository.Verify(r => r.DeleteByBusinessIdAsync(businessId), Times.Once);
        _mockEmailUpdateRequestRepository.Verify(r => r.AddAsync(It.IsAny<EmailUpdateRequest>()), Times.Once);
    }

    [Test]
    public async Task RequestEmailUpdate_Success_ShouldDeleteBeforeInsert()
    {
        // ARRANGE
        var callOrder = new List<string>();
        var businessId = Guid.NewGuid();
        var dto = new RequestEmailUpdateDto
        {
            BusinessId = businessId,
            EmailAddress = "newemail@example.com"
        };

        _mockEmailUpdateRequestRepository
            .Setup(r => r.DeleteByBusinessIdAsync(businessId))
            .Callback(() => callOrder.Add("delete"))
            .Returns(Task.CompletedTask);
        _mockEmailUpdateRequestRepository
            .Setup(r => r.AddAsync(It.IsAny<EmailUpdateRequest>()))
            .Callback(() => callOrder.Add("add"))
            .Returns(Task.CompletedTask);

        // ACT
        await _controller.RequestEmailUpdate(dto);

        // ASSERT
        Assert.That(callOrder, Has.Count.EqualTo(2));
        Assert.That(callOrder[0], Is.EqualTo("delete"));
        Assert.That(callOrder[1], Is.EqualTo("add"));
    }

    [Test]
    public async Task RequestEmailUpdate_WithoutReason_ShouldSucceed()
    {
        // ARRANGE
        var businessId = Guid.NewGuid();
        var dto = new RequestEmailUpdateDto
        {
            BusinessId = businessId,
            EmailAddress = "newemail@example.com",
            Reason = null
        };

        _mockEmailUpdateRequestRepository
            .Setup(r => r.DeleteByBusinessIdAsync(businessId))
            .Returns(Task.CompletedTask);
        _mockEmailUpdateRequestRepository
            .Setup(r => r.AddAsync(It.IsAny<EmailUpdateRequest>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _controller.RequestEmailUpdate(dto);

        // ASSERT
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _mockEmailUpdateRequestRepository.Verify(r => r.AddAsync(It.IsAny<EmailUpdateRequest>()), Times.Once);
    }

    [Test]
    public async Task RequestEmailUpdate_RepositoryThrows_ShouldReturn500()
    {
        // ARRANGE
        var businessId = Guid.NewGuid();
        var dto = new RequestEmailUpdateDto
        {
            BusinessId = businessId,
            EmailAddress = "newemail@example.com"
        };

        _mockEmailUpdateRequestRepository
            .Setup(r => r.DeleteByBusinessIdAsync(businessId))
            .ThrowsAsync(new Exception("Database error"));

        // ACT
        var result = await _controller.RequestEmailUpdate(dto);

        // ASSERT
        var statusResult = result as ObjectResult;
        Assert.That(statusResult, Is.Not.Null);
        Assert.That(statusResult!.StatusCode, Is.EqualTo(500));
    }
}