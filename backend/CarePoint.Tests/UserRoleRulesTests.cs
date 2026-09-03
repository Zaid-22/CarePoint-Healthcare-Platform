using CarePoint.Domain.Common;
using CarePoint.Application.Configuration;
using CarePoint.Application.DTOs.Auth;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;
using CarePoint.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace CarePoint.Tests;

public class UserRoleRulesTests
{
    [Theory]
    [InlineData("Admin")]
    [InlineData("Doctor")]
    [InlineData("Patient")]
    public void ResolvesExactlyOneKnownRole(string assignedRole)
    {
        Assert.True(UserRoleRules.TryResolveSingleRole([assignedRole], out var role));
        Assert.Equal(assignedRole, role);
    }

    [Fact]
    public void RejectsMissingMultipleAndUnknownRoles()
    {
        Assert.False(UserRoleRules.TryResolveSingleRole([], out _));
        Assert.False(UserRoleRules.TryResolveSingleRole(["Doctor", "Admin"], out _));
        Assert.False(UserRoleRules.TryResolveSingleRole(["SuperUser"], out _));
    }

    [Fact]
    public async Task LoginFailsClosedBeforeIssuingSessionForMultiRoleAccount()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Doctor", "Admin" })
            Assert.True((await roleManager.CreateAsync(new IdentityRole(role))).Succeeded);
        var user = new ApplicationUser
        {
            UserName = "multi-role@example.com",
            Email = "multi-role@example.com",
            FirstName = "Multi",
            LastName = "Role"
        };
        Assert.True((await userManager.CreateAsync(user, "Strong#Pass2026")).Succeeded);
        Assert.True((await userManager.AddToRolesAsync(user, ["Doctor", "Admin"])).Succeeded);
        var service = scope.ServiceProvider.GetRequiredService<AuthService>();

        await Assert.ThrowsAsync<ForbiddenException>(() => service.LoginAsync(new LoginDto
        {
            Email = user.Email,
            Password = "Strong#Pass2026"
        }));

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(context.RefreshTokens);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options
            .UseInMemoryDatabase($"role-invariant-{Guid.NewGuid()}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.Configure<JwtSettings>(settings =>
        {
            settings.Secret = "CarePointRoleInvariantSigningKey-2026-AtLeast64Characters-Long";
            settings.Issuer = "CarePoint.Tests";
            settings.Audience = "CarePoint.Tests";
        });
        services.Configure<EmailSettings>(_ => { });
        services.AddScoped<IPasswordResetEmailSender, NoOpPasswordResetEmailSender>();
        services.AddScoped<AuthService>();
        return services.BuildServiceProvider();
    }

    private sealed class NoOpPasswordResetEmailSender : IPasswordResetEmailSender
    {
        public Task SendAsync(
            string recipientEmail, string resetUrl,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
