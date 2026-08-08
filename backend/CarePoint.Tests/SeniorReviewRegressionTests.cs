using CarePoint.Application.DTOs.Auth;
using CarePoint.Application.DTOs.Common;
using CarePoint.Application.Validators;
using CarePoint.Domain.Common;
using CarePoint.Domain.Enums;
using CarePoint.Infrastructure.Data;

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

    [Fact]
    public void RefreshTokenHash_UsesStableUtf8Sha256()
    {
        Assert.Equal(
            "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD",
            RefreshTokenSecurity.Hash("abc"));
    }

    [Fact]
    public void PublicAuthenticationResponseNeverContainsRefreshToken()
    {
        Assert.DoesNotContain(typeof(AuthResponseDto).GetProperties(),
            property => property.Name.Contains("RefreshToken", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true, 1, true)]
    [InlineData(false, 0, true)]
    [InlineData(false, 1, false)]
    public void RefreshTokenReuse_IsDetectedAcrossTheFamily(
        bool alreadyRevoked, int affectedRows, bool expected)
    {
        Assert.Equal(expected, RefreshTokenSecurity.IsReuseDetected(alreadyRevoked, affectedRows));
    }

    [Fact]
    public void ProductionDatabaseInitialization_IsExplicit()
    {
        Assert.True(DatabaseInitializationCommand.IsRequested(new[] { "--INITIALIZE-DATABASE" }));
        Assert.False(DatabaseInitializationCommand.IsRequested(new[] { "--urls", "http://localhost" }));
    }

    [Fact]
    public void PendingDoctorAvailability_IsHiddenFromPublicCallers()
    {
        Assert.False(DoctorDirectoryAccessRules.CanViewAvailability(
            DoctorApprovalStatus.Pending, "doctor-user", null, null));
        Assert.True(DoctorDirectoryAccessRules.CanViewAvailability(
            DoctorApprovalStatus.Pending, "doctor-user", "doctor-user", "Doctor"));
        Assert.True(DoctorDirectoryAccessRules.CanViewAvailability(
            DoctorApprovalStatus.Pending, "doctor-user", "admin-user", "Admin"));
    }

    [Fact]
    public void AvailabilityChange_CannotOrphanFutureBooking()
    {
        var appointments = new[]
        {
            new AppointmentWindow(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(10, 30))
        };

        Assert.True(AvailabilityCoverageRules.WouldInvalidateBooking(
            appointments,
            new[] { new AvailabilityWindow(DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(12, 0), 30) }));
        Assert.False(AvailabilityCoverageRules.WouldInvalidateBooking(
            appointments,
            new[] { new AvailabilityWindow(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0), 30) }));
        Assert.True(AvailabilityCoverageRules.WouldInvalidateBooking(
            appointments,
            new[] { new AvailabilityWindow(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0), 45) }));
    }
}
