using CarePoint.Application.DTOs.Doctors;
using CarePoint.Application.Interfaces;
using CarePoint.Application.Validators;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Enums;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;
using CarePoint.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarePoint.Tests;

public class DoctorCredentialTests
{
    [Fact]
    public void DoctorRegistrationRequiresLicenseEvidence()
    {
        var result = new CreateDoctorValidator().Validate(new CreateDoctorDto
        {
            ConsultationFee = 20,
            SpecialtyIds = { Guid.NewGuid() }
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateDoctorDto.LicenseNumber));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateDoctorDto.LicensingAuthority));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateDoctorDto.LicenseExpiryDate));
    }

    [Fact]
    public async Task ApprovalPersistsReviewerTimestampAndFindings()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var doctorUser = new ApplicationUser
        {
            UserName = "credential-doctor@example.com",
            Email = "credential-doctor@example.com",
            FirstName = "Credential",
            LastName = "Doctor"
        };
        Assert.True((await userManager.CreateAsync(doctorUser)).Succeeded);
        var doctor = new DoctorProfile
        {
            UserId = doctorUser.Id,
            LicenseNumber = "JMC-12345",
            LicensingAuthority = "Jordan Medical Council",
            LicenseExpiryDate = new DateTime(2027, 8, 1),
            ApprovalStatus = DoctorApprovalStatus.Pending
        };
        context.DoctorProfiles.Add(doctor);
        await context.SaveChangesAsync();
        var service = new DoctorService(
            context,
            userManager,
            new RecordingNotificationService(),
            new FixedClinicClock(),
            null!,
            NullLogger<DoctorService>.Instance);

        var approved = await service.ApproveAsync(
            doctor.Id,
            "reviewing-admin",
            new ReviewDoctorCredentialsDto
            {
                Notes = "  License verified with issuing authority.  ",
                Credentials = Snapshot(doctor)
            });

        Assert.Equal(DoctorApprovalStatus.Approved, approved.ApprovalStatus);
        Assert.Equal("reviewing-admin", approved.CredentialsReviewedByUserId);
        Assert.Equal("License verified with issuing authority.", approved.CredentialReviewNotes);
        Assert.NotNull(approved.CredentialsReviewedAt);
        var audit = await context.DoctorCredentialReviews.SingleAsync();
        Assert.Equal(DoctorApprovalStatus.Approved, audit.Decision);
        Assert.Equal("reviewing-admin", audit.ReviewedByUserId);
        Assert.Equal("JMC-12345", audit.LicenseNumber);
        Assert.Equal("License verified with issuing authority.", audit.Notes);
    }

    [Fact]
    public async Task ExpiredAndUnreviewedApprovalsAreNotPubliclyBookable()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var expiredUser = CreateUser("expired-doctor@example.com");
        var unreviewedUser = CreateUser("unreviewed-doctor@example.com");
        Assert.True((await userManager.CreateAsync(expiredUser)).Succeeded);
        Assert.True((await userManager.CreateAsync(unreviewedUser)).Succeeded);
        var expiredDoctor = ApprovedDoctor(expiredUser.Id, new DateTime(2026, 8, 1), DateTime.UtcNow);
        var unreviewedDoctor = ApprovedDoctor(unreviewedUser.Id, new DateTime(2027, 8, 1), null);
        context.DoctorProfiles.AddRange(expiredDoctor, unreviewedDoctor);
        await context.SaveChangesAsync();
        var service = new DoctorService(
            context,
            userManager,
            new RecordingNotificationService(),
            new FixedClinicClock(),
            null!,
            NullLogger<DoctorService>.Instance);

        var directory = await service.GetAllAsync();

        Assert.Empty(directory.Items);
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(expiredDoctor.Id));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetAvailableSlotsAsync(unreviewedDoctor.Id, new DateTime(2026, 8, 10)));
        var appointmentService = new AppointmentService(
            context, null!, null!, new FixedClinicClock());
        await Assert.ThrowsAsync<BadRequestException>(() => appointmentService.CreateAsync(
            "patient-user",
            new CarePoint.Application.DTOs.Appointments.CreateAppointmentDto
            {
                DoctorProfileId = expiredDoctor.Id,
                ClinicId = Guid.NewGuid(),
                AppointmentDate = new DateTime(2026, 8, 10),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(9, 30)
            }));
    }

    [Theory]
    [InlineData("number")]
    [InlineData("authority")]
    [InlineData("expiry")]
    public async Task StaleCredentialReviewCannotApproveOrRejectReplacementCredentials(string changedField)
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = CreateUser("stale-review@example.com");
        await userManager.CreateAsync(user);
        var doctor = ApprovedDoctor(user.Id, new DateTime(2027, 8, 1), null);
        doctor.ApprovalStatus = DoctorApprovalStatus.Pending;
        context.DoctorProfiles.Add(doctor);
        await context.SaveChangesAsync();
        var request = new ReviewDoctorCredentialsDto { Notes = "Verified the displayed license", Credentials = Snapshot(doctor) };
        switch (changedField)
        {
            case "number": doctor.LicenseNumber = "Replacement number"; break;
            case "authority": doctor.LicensingAuthority = "Replacement authority"; break;
            case "expiry": doctor.LicenseExpiryDate = new DateTime(2028, 8, 1); break;
        }
        await context.SaveChangesAsync();
        var service = new DoctorService(context, userManager, new RecordingNotificationService(),
            new FixedClinicClock(), null!, NullLogger<DoctorService>.Instance);

        await Assert.ThrowsAsync<ConflictException>(() => service.ApproveAsync(doctor.Id, "admin", request));
        await Assert.ThrowsAsync<ConflictException>(() => service.RejectAsync(doctor.Id, "admin", request));

        Assert.Equal(DoctorApprovalStatus.Pending, doctor.ApprovalStatus);
        Assert.Empty(context.DoctorCredentialReviews);
        Assert.Null(doctor.CredentialsReviewedAt);
    }

    [Fact]
    public void CredentialReviewRequiresTheDisplayedSnapshot()
    {
        var validator = new ReviewDoctorCredentialsValidator();
        Assert.False(validator.Validate(new ReviewDoctorCredentialsDto { Notes = "Verified" }).IsValid);
        // Incomplete legacy credentials must still be reviewable for rejection.
        Assert.True(validator.Validate(new ReviewDoctorCredentialsDto
        {
            Notes = "Missing license", Credentials = new DoctorCredentialSnapshotDto()
        }).IsValid);
    }

    private static DoctorCredentialSnapshotDto Snapshot(DoctorProfile doctor) => new()
    {
        LicenseNumber = doctor.LicenseNumber,
        LicensingAuthority = doctor.LicensingAuthority,
        LicenseExpiryDate = doctor.LicenseExpiryDate
    };

    private static ApplicationUser CreateUser(string email) => new()
    {
        UserName = email,
        Email = email,
        FirstName = "Credential",
        LastName = "Doctor"
    };

    private static DoctorProfile ApprovedDoctor(
        string userId, DateTime expiryDate, DateTime? reviewedAt) => new()
    {
        UserId = userId,
        ApprovalStatus = DoctorApprovalStatus.Approved,
        LicenseNumber = $"JMC-{Guid.NewGuid():N}",
        LicensingAuthority = "Jordan Medical Council",
        LicenseExpiryDate = expiryDate,
        CredentialsReviewedAt = reviewedAt
    };

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options
            .UseInMemoryDatabase($"doctor-credentials-{Guid.NewGuid()}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        return services.BuildServiceProvider();
    }

    private sealed class FixedClinicClock : IClinicClock
    {
        public DateTime UtcNow => new(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc);
        public DateTime LocalNow => new(2026, 8, 1, 11, 0, 0, DateTimeKind.Unspecified);
    }

    private sealed class RecordingNotificationService : INotificationService
    {
        public Task<IReadOnlyList<CarePoint.Application.DTOs.Medical.NotificationDto>> GetByUserIdAsync(
            string userId) => Task.FromResult<IReadOnlyList<CarePoint.Application.DTOs.Medical.NotificationDto>>([]);

        public Task MarkAsReadAsync(Guid id, string userId) => Task.CompletedTask;
        public Task MarkAllAsReadAsync(string userId) => Task.CompletedTask;

        public Task CreateNotificationAsync(
            string userId, string title, string message, NotificationType type,
            Guid? referenceId = null) => Task.CompletedTask;
    }
}
