using CarePoint.Application.DTOs.Doctors;
using CarePoint.Application.DTOs.Auth;
using CarePoint.Application.Validators;

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
    public void RefreshToken_RejectsEmptyToken()
    {
        var result = new RefreshTokenRequestValidator().Validate(new RefreshTokenRequestDto());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RefreshTokenRequestDto.RefreshToken));
    }
}
