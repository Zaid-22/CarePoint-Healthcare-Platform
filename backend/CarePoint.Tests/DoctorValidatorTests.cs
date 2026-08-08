using CarePoint.Application.DTOs.Doctors;
using CarePoint.Application.DTOs.Auth;
using CarePoint.Application.Validators;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Enums;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;
using CarePoint.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarePoint.Tests;

public class DoctorValidatorTests
{
    [Fact]
    public void UpdateDoctor_RejectsEmptySpecialties()
    {
        var result = new UpdateDoctorValidator().Validate(new UpdateDoctorDto
        {
            ConsultationFee = 20,
            SpecialtyIds = new List<Guid>()
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateDoctorDto.SpecialtyIds));
    }

    [Fact]
    public void UpdateDoctor_RejectsNegativeFee()
    {
        var result = new UpdateDoctorValidator().Validate(new UpdateDoctorDto
        {
            ConsultationFee = -1,
            SpecialtyIds = new List<Guid> { Guid.NewGuid() }
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateDoctorDto.ConsultationFee));
    }

    [Fact]
    public void RegisterDoctor_RejectsNegativeFeeAndOversizedOrInlineProfileData()
    {
        var result = new RegisterValidator().Validate(new RegisterDto
        {
            FirstName = "Test",
            LastName = "Doctor",
            Email = "doctor@example.com",
            Password = "StrongPass1!",
            ConfirmPassword = "StrongPass1!",
            Role = "Doctor",
            SpecialtyIds = new List<Guid> { Guid.NewGuid() },
            ConsultationFee = -1,
            Bio = new string('b', 2001),
            PhoneNumber = new string('1', 21),
            ProfilePictureUrl = "data:image/png;base64,AAAA"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterDto.ConsultationFee));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterDto.Bio));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterDto.PhoneNumber));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterDto.ProfilePictureUrl));
    }

    [Fact]
    public void PublicDoctorContractDoesNotExposeAccountOrApprovalFields()
    {
        var propertyNames = typeof(PublicDoctorDto).GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain(nameof(DoctorDto.UserId), propertyNames);
        Assert.DoesNotContain(nameof(DoctorDto.Email), propertyNames);
        Assert.DoesNotContain(nameof(DoctorDto.PhoneNumber), propertyNames);
        Assert.DoesNotContain(nameof(DoctorDto.Gender), propertyNames);
        Assert.DoesNotContain(nameof(DoctorDto.ApprovalStatus), propertyNames);
    }

    [Fact]
    public async Task ProfileImageStoragePersistsOnlyOpaqueKeysUnderItsRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"carepoint-profile-images-{Guid.NewGuid():N}");
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ProfileImages:StoragePath"] = root
                })
                .Build();
            var storage = new LocalProfileImageStorage(configuration);

            var key = await storage.SaveAsync(new MemoryStream(new byte[] { 1, 2, 3 }), ".png");
            await using var content = await storage.OpenReadAsync(key);

            Assert.Equal(Path.GetFileName(key), key);
            Assert.Equal(new byte[] { 1, 2, 3 }, await ReadAllBytesAsync(content));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                storage.OpenReadAsync("../outside.png"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task PendingArchivedProfileImageIsLimitedToOwnerAndAdmin()
    {
        await using var context = CreateContext();
        var doctor = CreateDoctor(DoctorApprovalStatus.Pending);
        context.DoctorProfiles.Add(doctor);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetProfileImageAsync(doctor.Id));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetProfileImageAsync(doctor.Id, "another-user", "Doctor"));

        var ownerImage = await service.GetProfileImageAsync(doctor.Id, doctor.UserId, "Doctor");
        await using (ownerImage.Content)
        {
            Assert.Equal("image/png", ownerImage.ContentType);
            Assert.Equal(new byte[] { 1, 2, 3 }, await ReadAllBytesAsync(ownerImage.Content));
        }

        var adminImage = await service.GetProfileImageAsync(doctor.Id, "admin-user", "Admin");
        await using (adminImage.Content)
        {
            Assert.Equal(new byte[] { 1, 2, 3 }, await ReadAllBytesAsync(adminImage.Content));
        }
    }

    [Fact]
    public async Task ApprovedArchivedProfileImageCanBeReadAnonymously()
    {
        await using var context = CreateContext();
        var doctor = CreateDoctor(DoctorApprovalStatus.Approved);
        context.AddRange(doctor, CreateUser(doctor.UserId, lockoutEnd: null));
        await context.SaveChangesAsync();

        var image = await CreateService(context).GetProfileImageAsync(doctor.Id);
        await using (image.Content)
        {
            Assert.Equal("image/png", image.ContentType);
            Assert.Equal(new byte[] { 1, 2, 3 }, await ReadAllBytesAsync(image.Content));
        }
    }

    [Fact]
    public async Task PermanentlyDisabledDoctorIsHiddenFromPublicDirectoryAndSlots()
    {
        await using var context = CreateContext();
        var doctor = CreateDoctor(DoctorApprovalStatus.Approved);
        var user = CreateUser(doctor.UserId, DateTimeOffset.MaxValue);
        var date = new DateTime(2026, 8, 10);
        context.AddRange(user, doctor, new DoctorAvailability
        {
            DoctorProfile = doctor,
            DayOfWeek = date.DayOfWeek,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            SlotDurationMinutes = 30
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var publicDirectory = await service.GetAllAsync();
        var adminDirectory = await service.GetAllForAdminAsync();

        Assert.Empty(publicDirectory.Items);
        Assert.Equal(0, publicDirectory.TotalCount);
        Assert.Single(adminDirectory.Items);
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(doctor.Id));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetAvailableSlotsAsync(doctor.Id, date));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetProfileImageAsync(doctor.Id));
    }

    [Fact]
    public async Task TemporaryLockoutDoesNotRemoveDoctorFromDirectoryOrSlots()
    {
        await using var context = CreateContext();
        var doctor = CreateDoctor(DoctorApprovalStatus.Approved);
        var user = CreateUser(doctor.UserId, DateTimeOffset.UtcNow.AddMinutes(5));
        var date = new DateTime(2026, 8, 10);
        context.AddRange(user, doctor, new DoctorAvailability
        {
            DoctorProfile = doctor,
            DayOfWeek = date.DayOfWeek,
            StartTime = new TimeOnly(12, 0),
            EndTime = new TimeOnly(13, 0),
            SlotDurationMinutes = 30
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var publicDirectory = await service.GetAllAsync();
        var slots = await service.GetAvailableSlotsAsync(doctor.Id, date);

        Assert.Single(publicDirectory.Items);
        Assert.Equal(1, publicDirectory.TotalCount);
        Assert.Equal(2, slots.Count);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"doctor-images-{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static DoctorProfile CreateDoctor(DoctorApprovalStatus approvalStatus)
    {
        var doctorId = Guid.NewGuid();
        return new DoctorProfile
        {
            Id = doctorId,
            UserId = "doctor-user",
            ApprovalStatus = approvalStatus,
            ArchivedProfilePictureValue = "data:image/png;base64,AQID",
            ProfilePictureUrl = $"/api/doctors/{doctorId}/avatar?v=legacy"
        };
    }

    private static ApplicationUser CreateUser(string id, DateTimeOffset? lockoutEnd) => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        NormalizedUserName = $"{id.ToUpperInvariant()}@EXAMPLE.COM",
        Email = $"{id}@example.com",
        NormalizedEmail = $"{id.ToUpperInvariant()}@EXAMPLE.COM",
        FirstName = "Test",
        LastName = "Doctor",
        LockoutEnabled = true,
        LockoutEnd = lockoutEnd
    };

    private static DoctorService CreateService(ApplicationDbContext context) => new(
        context,
        null!,
        null!,
        new FixedClinicClock(),
        new UnusedProfileImageStorage(),
        NullLogger<DoctorService>.Instance);

    private sealed class FixedClinicClock : IClinicClock
    {
        public DateTime UtcNow => new(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc);
        public DateTime LocalNow => new(2026, 8, 1, 11, 0, 0, DateTimeKind.Unspecified);
    }

    private sealed class UnusedProfileImageStorage : IProfileImageStorage
    {
        public Task<string> SaveAsync(Stream content, string fileExtension,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Stream> OpenReadAsync(string storageKey,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string storageKey,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return buffer.ToArray();
    }
}
