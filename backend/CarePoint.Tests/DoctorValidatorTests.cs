using CarePoint.Application.DTOs.Doctors;
using CarePoint.Application.DTOs.Auth;
using CarePoint.Application.Validators;
using CarePoint.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

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

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return buffer.ToArray();
    }
}
