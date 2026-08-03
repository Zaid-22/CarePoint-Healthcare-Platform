using CarePoint.Application.DTOs.Auth;
using CarePoint.Application.DTOs.Common;
using CarePoint.Application.Validators;
using CarePoint.Domain.Common;

namespace CarePoint.Tests;

public class SeniorReviewRegressionTests
{
    [Fact]
    public void DoctorAccess_IsBoundToTheRequestedAppointment()
    {
        var assignedDoctor = Guid.NewGuid();

        Assert.True(ClinicalAccessRules.CanDoctorAccessAppointment(assignedDoctor, assignedDoctor));
        Assert.False(ClinicalAccessRules.CanDoctorAccessAppointment(assignedDoctor, Guid.NewGuid()));
    }

    [Fact]
    public void AppointmentScheduling_UsesProvidedClinicLocalTime()
    {
        var clinicNow = new DateTime(2026, 8, 4, 1, 30, 0);

        Assert.False(AppointmentSchedulingRules.IsInFuture(
            clinicNow, new DateTime(2026, 8, 4), new TimeOnly(1, 0)));
        Assert.True(AppointmentSchedulingRules.IsInFuture(
            clinicNow, new DateTime(2026, 8, 4), new TimeOnly(2, 0)));
    }

    [Fact]
    public void AdminAppointmentActions_NotifyBothParticipants()
    {
        var recipients = AppointmentNotificationRecipients.ForActor("Admin", "patient", "doctor");

        Assert.Equal(new[] { "patient", "doctor" }, recipients);
    }

    [Theory]
    [InlineData(-10, 0, 0, 1)]
    [InlineData(5, 500, 5, 100)]
    public void Pagination_IsBounded(int skip, int take, int expectedSkip, int expectedTake)
    {
        Assert.Equal((expectedSkip, expectedTake), Pagination.Normalize(skip, take));
    }

    [Fact]
    public void DoctorRegistration_RequiresASpecialty()
    {
        var result = new RegisterValidator().Validate(new RegisterDto
        {
            FirstName = "A",
            LastName = "Doctor",
            Email = "doctor@example.test",
            Password = "Strong#123",
            ConfirmPassword = "Strong#123",
            Role = "Doctor"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterDto.SpecialtyIds));
    }
}
