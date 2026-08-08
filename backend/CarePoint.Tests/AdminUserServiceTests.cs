using CarePoint.Domain.Entities;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;
using CarePoint.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace CarePoint.Tests;

public class AdminUserServiceTests
{
    [Fact]
    public async Task DisablingAccountRevokesRefreshSessionsAndCanBeReversed()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<AdminUserService>();
        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Patient");
        var actor = await CreateUserAsync(userManager, "admin@example.com", "Admin");
        var target = await CreateUserAsync(userManager, "patient@example.com", "Patient");
        var token = new RefreshToken
        {
            UserId = target.Id,
            FamilyId = Guid.NewGuid(),
            TokenHash = "test-token-hash",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        var disabled = await service.SetDisabledAsync(target.Id, actor.Id, true);

        Assert.True(disabled.IsDisabled);
        Assert.True(disabled.IsLockedOut);
        Assert.True((await context.RefreshTokens.FindAsync(token.Id))!.IsRevoked);

        var enabled = await service.SetDisabledAsync(target.Id, actor.Id, false);

        Assert.False(enabled.IsDisabled);
        Assert.False(enabled.IsLockedOut);
    }

    [Fact]
    public async Task AdministratorCannotChangeOwnAccountState()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var service = scope.ServiceProvider.GetRequiredService<AdminUserService>();
        await EnsureRoleAsync(roleManager, "Admin");
        var actor = await CreateUserAsync(userManager, "admin@example.com", "Admin");

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.SetDisabledAsync(actor.Id, actor.Id, true));
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options
            .UseInMemoryDatabase($"admin-users-{Guid.NewGuid()}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<AdminUserService>();
        return services.BuildServiceProvider();
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string role)
    {
        if (!await roleManager.RoleExistsAsync(role))
            Assert.True((await roleManager.CreateAsync(new IdentityRole(role))).Succeeded);
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        UserManager<ApplicationUser> userManager, string email, string role)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = role,
            LastName = "Tester"
        };
        Assert.True((await userManager.CreateAsync(user)).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(user, role)).Succeeded);
        return user;
    }
}
