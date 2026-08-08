using CarePoint.Application.Configuration;
using CarePoint.Application.DTOs.Auth;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Enums;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;
using CarePoint.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CarePoint.Tests;

public class SqlServerIntegrationTests
{
    private static string? ConnectionString =>
        Environment.GetEnvironmentVariable("CAREPOINT_TEST_SQL");

    [Fact]
    public async Task MedicalRecordRowVersionRejectsConcurrentSqlServerUpdate()
    {
        if (ConnectionString is null) return;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        Guid recordId;

        await using (var setup = new ApplicationDbContext(options))
        {
            await setup.Database.MigrateAsync();
            var suffix = Guid.NewGuid().ToString("N");
            var patientUser = CreateIdentityUser($"patient-{suffix}@example.com");
            var doctorUser = CreateIdentityUser($"doctor-{suffix}@example.com");
            var patient = new PatientProfile { UserId = patientUser.Id };
            var doctor = new DoctorProfile
            {
                UserId = doctorUser.Id,
                ApprovalStatus = DoctorApprovalStatus.Approved
            };
            var appointment = new Appointment
            {
                PatientProfile = patient,
                DoctorProfile = doctor,
                AppointmentDate = new DateTime(2026, 8, 1),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(9, 30),
                Status = AppointmentStatus.Completed
            };
            var record = new MedicalRecord
            {
                Appointment = appointment,
                Diagnosis = "Initial diagnosis"
            };
            setup.Users.AddRange(patientUser, doctorUser);
            setup.MedicalRecords.Add(record);
            await setup.SaveChangesAsync();
            recordId = record.Id;
        }

        await using var firstContext = new ApplicationDbContext(options);
        await using var secondContext = new ApplicationDbContext(options);
        var firstCopy = await firstContext.MedicalRecords.SingleAsync(record => record.Id == recordId);
        var secondCopy = await secondContext.MedicalRecords.SingleAsync(record => record.Id == recordId);
        firstCopy.Diagnosis = "First correction";
        secondCopy.Diagnosis = "Stale correction";

        await firstContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
    }

    [Fact]
    public async Task RefreshTokenReuseRevokesEntireFamilyOnSqlServer()
    {
        if (ConnectionString is null) return;
        using var provider = CreateAuthProvider(ConnectionString);
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync("Patient"))
            Assert.True((await roleManager.CreateAsync(new IdentityRole("Patient"))).Succeeded);
        var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
        var suffix = Guid.NewGuid().ToString("N");
        var firstSession = await authService.RegisterAsync(new RegisterDto
        {
            FirstName = "Rotation",
            LastName = "Tester",
            Email = $"rotation-{suffix}@example.com",
            Password = "Strong#Pass2026",
            ConfirmPassword = "Strong#Pass2026",
            Role = "Patient"
        });
        var rotatedSession = await authService.RefreshTokenAsync(firstSession.RefreshToken);

        var reuse = await Assert.ThrowsAsync<BadRequestException>(() =>
            authService.RefreshTokenAsync(firstSession.RefreshToken));

        Assert.Contains("reuse", reuse.Message, StringComparison.OrdinalIgnoreCase);
        await Assert.ThrowsAsync<BadRequestException>(() =>
            authService.RefreshTokenAsync(rotatedSession.RefreshToken));
    }

    private static ApplicationUser CreateIdentityUser(string email) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        FirstName = "Integration",
        LastName = "Tester",
        SecurityStamp = Guid.NewGuid().ToString("N"),
        ConcurrencyStamp = Guid.NewGuid().ToString("N")
    };

    private static ServiceProvider CreateAuthProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        services.Configure<JwtSettings>(settings =>
        {
            settings.Secret = "CarePointSqlIntegrationSigningKey-2026-AtLeast64Characters-Long";
            settings.Issuer = "CarePoint.Tests";
            settings.Audience = "CarePoint.Tests";
            settings.AccessTokenExpirationMinutes = 5;
            settings.RefreshTokenExpirationDays = 1;
        });
        services.Configure<EmailSettings>(_ => { });
        services.AddScoped<IPasswordResetEmailSender, NoOpPasswordResetEmailSender>();
        services.AddScoped<AuthService>();
        return services.BuildServiceProvider();
    }

    private sealed class NoOpPasswordResetEmailSender : IPasswordResetEmailSender
    {
        public Task SendAsync(
            string recipientEmail,
            string resetUrl,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
