using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using CarePoint.Application.DTOs.Auth;
using CarePoint.Application.Interfaces;
using CarePoint.Application.Configuration;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;
using System.Data;

namespace CarePoint.Infrastructure.Services;

/// <summary>
/// Authentication service implementing JWT with refresh token rotation.
/// Uses ASP.NET Core Identity's PasswordHasher for secure hashing.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly EmailSettings _emailSettings;
    private readonly IPasswordResetEmailSender _passwordResetEmailSender;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings,
        IOptions<EmailSettings> emailSettings,
        IPasswordResetEmailSender passwordResetEmailSender)
    {
        _userManager = userManager;
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _emailSettings = emailSettings.Value;
        _passwordResetEmailSender = passwordResetEmailSender;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        // Check if email already exists
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            throw new ConflictException($"Email '{dto.Email}' is already registered.");

        List<Guid>? validSpecialtyIds = null;
        if (dto.Role == "Doctor" && dto.SpecialtyIds is { Count: > 0 })
        {
            validSpecialtyIds = await _context.Specialties
                .Where(s => s.IsActive && dto.SpecialtyIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();
            if (validSpecialtyIds.Count != dto.SpecialtyIds.Distinct().Count())
                throw new BadRequestException("One or more selected specialties are invalid.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new BadRequestException(string.Join("; ", errors));
        }

        // Assign role
        var role = dto.Role == "Doctor" ? "Doctor" : "Patient";
        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            var errors = roleResult.Errors.Select(error => error.Description);
            throw new BadRequestException(string.Join("; ", errors));
        }

        // Create profile based on role
        if (role == "Patient")
        {
            _context.PatientProfiles.Add(new PatientProfile { UserId = user.Id });
        }
        else
        {
            var doctorProfile = new DoctorProfile
            {
                UserId = user.Id,
                Bio = dto.Bio,
                ConsultationFee = dto.ConsultationFee ?? 0,
                PhoneNumber = dto.PhoneNumber,
                Gender = dto.Gender,
                ProfilePictureUrl = dto.ProfilePictureUrl,
                ApprovalStatus = Domain.Enums.DoctorApprovalStatus.Pending
            };
            _context.DoctorProfiles.Add(doctorProfile);

            if (validSpecialtyIds is { Count: > 0 })
            {
                foreach (var specialtyId in validSpecialtyIds)
                {
                    _context.DoctorSpecialties.Add(new DoctorSpecialty
                    {
                        DoctorProfileId = doctorProfile.Id,
                        SpecialtyId = specialtyId
                    });
                }
            }
        }
        await _context.SaveChangesAsync();

        // Generate tokens
        var accessToken = await GenerateAccessTokenAsync(user);
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);
        await transaction.CommitAsync();

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = role,
            Roles = new List<string> { role },
            AccessToken = accessToken,
            RefreshToken = refreshToken.RawToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email)
            ?? throw new BadRequestException("Invalid email or password.");

        if (await _userManager.IsLockedOutAsync(user))
            throw new BadRequestException("Account is locked. Please try again later.");

        var isValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!isValid)
        {
            await _userManager.AccessFailedAsync(user);
            throw new BadRequestException("Invalid email or password.");
        }

        // Reset failed count on success
        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Patient";

        var accessToken = await GenerateAccessTokenAsync(user);
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = role,
            Roles = roles.ToList(),
            AccessToken = accessToken,
            RefreshToken = refreshToken.RawToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var storedToken = await _context.RefreshTokens
            .AsNoTracking()
            .Where(rt => rt.TokenHash == HashRefreshToken(dto.RefreshToken))
            .Select(rt => new { rt.Id, rt.UserId })
            .FirstOrDefaultAsync()
            ?? throw new BadRequestException("Invalid refresh token.");

        var revokedAt = DateTime.UtcNow;
        var affected = await _context.RefreshTokens
            .Where(rt => rt.Id == storedToken.Id && !rt.IsRevoked && rt.ExpiresAt >= revokedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rt => rt.IsRevoked, true)
                .SetProperty(rt => rt.RevokedAt, revokedAt));

        if (affected != 1)
            throw new BadRequestException("Refresh token has expired or been revoked.");

        var user = await _userManager.FindByIdAsync(storedToken.UserId)
            ?? throw new NotFoundException("User", storedToken.UserId);

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Patient";

        var accessToken = await GenerateAccessTokenAsync(user);
        var newRefreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);

        await _context.RefreshTokens
            .Where(rt => rt.Id == storedToken.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                rt => rt.ReplacedByTokenHash, newRefreshToken.Entity.TokenHash));
        await transaction.CommitAsync();

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = role,
            Roles = roles.ToList(),
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.RawToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
        };
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var revokedAt = DateTime.UtcNow;
        await _context.RefreshTokens
            .Where(rt => rt.TokenHash == HashRefreshToken(refreshToken) && !rt.IsRevoked)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rt => rt.IsRevoked, true)
                .SetProperty(rt => rt.RevokedAt, revokedAt));
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new BadRequestException(string.Join("; ", errors));
        }

        await RevokeActiveRefreshTokensAsync(user.Id);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        // Always return success to avoid email enumeration
        if (user == null) return;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = CreatePasswordResetUrl(dto.Email, token);
        await _passwordResetEmailSender.SendAsync(user.Email!, resetUrl);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email)
            ?? throw new BadRequestException("Invalid request.");

        var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new BadRequestException(string.Join("; ", errors));
        }

        await RevokeActiveRefreshTokensAsync(user.Id);
    }

    public async Task<AuthResponseDto> GetCurrentUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        var roles = await _userManager.GetRolesAsync(user);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = roles.FirstOrDefault() ?? "Patient",
            Roles = roles.ToList()
        };
    }

    // --- Private Helpers ---

    private async Task<string> GenerateAccessTokenAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<GeneratedRefreshToken> GenerateAndStoreRefreshTokenAsync(string userId)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = HashRefreshToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return new GeneratedRefreshToken(refreshToken, rawToken);
    }

    private async Task RevokeActiveRefreshTokensAsync(string userId)
    {
        var revokedAt = DateTime.UtcNow;
        await _context.RefreshTokens
            .Where(token => token.UserId == userId && !token.IsRevoked)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.IsRevoked, true)
                .SetProperty(token => token.RevokedAt, revokedAt));
    }

    private string CreatePasswordResetUrl(string email, string token)
    {
        var separator = _emailSettings.PasswordResetUrl.Contains('?') ? '&' : '?';
        return $"{_emailSettings.PasswordResetUrl}{separator}email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }

    private static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private sealed record GeneratedRefreshToken(RefreshToken Entity, string RawToken);
}
