using CarePoint.Application.DTOs.Auth;

namespace CarePoint.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto);
    Task LogoutAsync(string refreshToken);
    Task ChangePasswordAsync(string userId, ChangePasswordDto dto);
    Task ForgotPasswordAsync(ForgotPasswordDto dto);
    Task ResetPasswordAsync(ResetPasswordDto dto);
    Task<AuthResponseDto> GetCurrentUserAsync(string userId);
}
