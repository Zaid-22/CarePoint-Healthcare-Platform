using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CarePoint.Application.DTOs.Auth;
using CarePoint.Application.DTOs.Common;
using CarePoint.Application.Interfaces;
using Microsoft.AspNetCore.RateLimiting;

namespace CarePoint.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string RefreshCookieName = "CarePoint.Refresh";
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(IAuthService authService, IWebHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
    }

    /// <summary>
    /// Register a new user (Patient or Doctor).
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterDto dto)
    {
        var session = await _authService.RegisterAsync(dto);
        SetRefreshCookie(session);
        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(
            session.Response, "Registration successful."));
    }

    /// <summary>
    /// Login with email and password.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto dto)
    {
        var session = await _authService.LoginAsync(dto);
        SetRefreshCookie(session);
        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(
            session.Response, "Login successful."));
    }

    /// <summary>
    /// Refresh an expired access token using a valid refresh token.
    /// </summary>
    [HttpPost("refresh-token")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken()
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken) ||
            string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAccessException("No active session.");
        }

        try
        {
            var session = await _authService.RefreshTokenAsync(refreshToken);
            SetRefreshCookie(session);
            return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(session.Response));
        }
        catch
        {
            ClearRefreshCookie();
            throw;
        }
    }

    /// <summary>
    /// Logout — revoke the refresh token.
    /// </summary>
    [HttpPost("logout")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<string>>> Logout()
    {
        if (Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken) &&
            !string.IsNullOrWhiteSpace(refreshToken))
        {
            await _authService.LogoutAsync(refreshToken);
        }
        ClearRefreshCookie();
        return Ok(ApiResponse<string>.SuccessResponse("Logged out successfully."));
    }

    /// <summary>
    /// Change password for the authenticated user.
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<string>>> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _authService.ChangePasswordAsync(userId, dto);
        return Ok(ApiResponse<string>.SuccessResponse("Password changed successfully."));
    }

    /// <summary>
    /// Request a password reset email.
    /// </summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<string>>> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        await _authService.ForgotPasswordAsync(dto);
        return Ok(ApiResponse<string>.SuccessResponse("If the email exists, a reset link has been sent."));
    }

    /// <summary>
    /// Reset password using token from email.
    /// </summary>
    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<string>>> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        await _authService.ResetPasswordAsync(dto);
        return Ok(ApiResponse<string>.SuccessResponse("Password has been reset successfully."));
    }

    /// <summary>
    /// Get the currently authenticated user's info.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _authService.GetCurrentUserAsync(userId);
        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result));
    }

    private void SetRefreshCookie(AuthSessionDto session)
    {
        Response.Cookies.Append(RefreshCookieName, session.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment() || Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = session.RefreshTokenExpiration,
            IsEssential = true
        });
    }

    private void ClearRefreshCookie()
    {
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment() || Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            IsEssential = true
        });
    }
}
