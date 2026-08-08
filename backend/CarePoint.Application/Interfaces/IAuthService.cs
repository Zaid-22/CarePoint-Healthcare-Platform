using CarePoint.Application.DTOs.Auth;

namespace CarePoint.Application.Interfaces;

public interface IAuthService
{
    Task<AuthSessionDto> RegisterAsync(RegisterDto dto);
    Task<AuthSessionDto> LoginAsync(LoginDto dto);
    Task<AuthSessionDto> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
    Task ChangePasswordAsync(string userId, ChangePasswordDto dto);
    Task ForgotPasswordAsync(ForgotPasswordDto dto);
    Task ResetPasswordAsync(ResetPasswordDto dto);
    Task<AuthResponseDto> GetCurrentUserAsync(string userId);
}
