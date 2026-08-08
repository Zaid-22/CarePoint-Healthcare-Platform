using CarePoint.Application.DTOs.Admin;
using CarePoint.Application.DTOs.Common;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CarePoint.Infrastructure.Services;

public sealed class AdminUserService : IAdminUserService
{
    private static readonly string[] KnownRoles = ["Admin", "Doctor", "Patient"];

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<PagedResult<AdminUserDto>> GetAllAsync(
        string? search = null, string? role = null, int skip = 0, int take = 50)
    {
        (skip, take) = Pagination.Normalize(skip, take);
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        role = string.IsNullOrWhiteSpace(role) ? null : role.Trim();

        if (search?.Length > 200)
            throw new BadRequestException("Search text must be 200 characters or fewer.");
        if (role is not null && !KnownRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            throw new BadRequestException("Role must be Admin, Doctor, or Patient.");

        var usersQuery = _context.Users.AsNoTracking();
        if (search is not null)
        {
            usersQuery = usersQuery.Where(user =>
                (user.Email != null && user.Email.Contains(search)) ||
                (user.FirstName + " " + user.LastName).Contains(search));
        }

        if (role is not null)
        {
            usersQuery = usersQuery.Where(user =>
                _context.UserRoles.Any(userRole =>
                    userRole.UserId == user.Id &&
                    _context.Roles.Any(identityRole =>
                        identityRole.Id == userRole.RoleId && identityRole.Name == role)));
        }

        var totalCount = await usersQuery.CountAsync();
        var users = await usersQuery
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.Email)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        var userIds = users.Select(user => user.Id).ToArray();
        var roleAssignments = await (
            from userRole in _context.UserRoles.AsNoTracking()
            join identityRole in _context.Roles.AsNoTracking()
                on userRole.RoleId equals identityRole.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, Role = identityRole.Name! })
            .ToListAsync();
        var rolesByUser = roleAssignments
            .GroupBy(assignment => assignment.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(item => item.Role).Order().ToArray());

        var now = DateTimeOffset.UtcNow;
        var items = users.Select(user => Map(
            user,
            rolesByUser.GetValueOrDefault(user.Id) ?? Array.Empty<string>(),
            now)).ToList();
        return PagedResult<AdminUserDto>.Create(items, totalCount, skip, take);
    }

    public async Task<AdminUserDto> SetDisabledAsync(
        string id, string actorUserId, bool disabled)
    {
        if (id == actorUserId)
            throw new ForbiddenException("You cannot disable or enable your own administrator account.");

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await _userManager.FindByIdAsync(id)
            ?? throw new NotFoundException("User", id);
        var roles = await _userManager.GetRolesAsync(user);

        if (disabled && roles.Contains("Admin", StringComparer.Ordinal))
        {
            var adminRoleId = await _context.Roles
                .Where(identityRole => identityRole.Name == "Admin")
                .Select(identityRole => identityRole.Id)
                .SingleAsync();
            var anotherEnabledAdminExists = await (
                from userRole in _context.UserRoles
                join candidate in _context.Users on userRole.UserId equals candidate.Id
                where userRole.RoleId == adminRoleId && candidate.Id != id &&
                      candidate.LockoutEnd != DateTimeOffset.MaxValue
                select candidate.Id).AnyAsync();
            if (!anotherEnabledAdminExists)
                throw new BadRequestException("The last enabled administrator cannot be disabled.");
        }

        user.LockoutEnabled = true;
        user.LockoutEnd = disabled ? DateTimeOffset.MaxValue : null;
        if (!disabled) user.AccessFailedCount = 0;
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new BadRequestException(string.Join("; ", updateResult.Errors.Select(error => error.Description)));

        if (disabled)
        {
            var revokedAt = DateTime.UtcNow;
            var activeTokens = await _context.RefreshTokens
                .Where(token => token.UserId == id && !token.IsRevoked)
                .ToListAsync();
            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = revokedAt;
            }
            await _context.SaveChangesAsync();
        }

        await transaction.CommitAsync();
        return Map(user, roles.ToArray(), DateTimeOffset.UtcNow);
    }

    private static AdminUserDto Map(
        ApplicationUser user, IReadOnlyList<string> roles, DateTimeOffset now) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email ?? string.Empty,
        Roles = roles,
        IsDisabled = user.LockoutEnd == DateTimeOffset.MaxValue,
        IsLockedOut = user.LockoutEnabled && user.LockoutEnd > now,
        CreatedAt = user.CreatedAt
    };
}
