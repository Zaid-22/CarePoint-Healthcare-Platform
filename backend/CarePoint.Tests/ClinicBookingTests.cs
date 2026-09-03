using CarePoint.Application.DTOs.Appointments;
using CarePoint.Application.Interfaces;
using CarePoint.Application.Validators;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Enums;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;
using CarePoint.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarePoint.Tests;

public class ClinicBookingTests
{
    [Fact]
    public void BookingContractRequiresClinicLocation()
    {
        var result = new CreateAppointmentValidator().Validate(new CreateAppointmentDto
        {
            DoctorProfileId = Guid.NewGuid(),
            AppointmentDate = new DateTime(2026, 8, 10),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30)
        });

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreateAppointmentDto.ClinicId));
    }

    [Fact]
    public async Task BookingRejectsDetachedAndInactiveClinicLocations()
    {
        await using var context = CreateContext();
        var doctor = new DoctorProfile
        {
            UserId = "booking-doctor",
            ApprovalStatus = DoctorApprovalStatus.Approved,
            LicenseNumber = "JMC-BOOKING-1",
            LicensingAuthority = "Jordan Medical Council",
            LicenseExpiryDate = new DateTime(2027, 8, 1),
            CredentialsReviewedAt = new DateTime(2026, 7, 1)
        };
        var otherDoctor = new DoctorProfile
        {
            UserId = "other-doctor",
            ApprovalStatus = DoctorApprovalStatus.Approved,
            LicenseNumber = "JMC-BOOKING-2",
            LicensingAuthority = "Jordan Medical Council",
            LicenseExpiryDate = new DateTime(2027, 8, 1),
            CredentialsReviewedAt = new DateTime(2026, 7, 1)
        };
        var detachedClinic = new Clinic { Name = "Detached", IsActive = true };
        var inactiveClinic = new Clinic { Name = "Inactive", IsActive = false };
        context.AddRange(
            doctor,
            otherDoctor,
            detachedClinic,
            inactiveClinic,
            EnabledUser(doctor.UserId),
            EnabledUser(otherDoctor.UserId));
        context.ClinicDoctors.AddRange(
            new ClinicDoctor { Clinic = detachedClinic, DoctorProfile = otherDoctor },
            new ClinicDoctor { Clinic = inactiveClinic, DoctorProfile = doctor });
        await context.SaveChangesAsync();
        var service = new AppointmentService(context, null!, null!, new FixedClinicClock());

        foreach (var clinicId in new[] { detachedClinic.Id, inactiveClinic.Id })
        {
            var exception = await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(
                "patient-user",
                new CreateAppointmentDto
                {
                    DoctorProfileId = doctor.Id,
                    ClinicId = clinicId,
                    AppointmentDate = new DateTime(2026, 8, 10),
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(10, 30)
                }));
            Assert.Contains("active clinic", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Empty(context.Appointments);
    }

    [Fact]
    public async Task ClinicWithFutureAppointmentsCannotBeDeactivated()
    {
        await using var context = CreateContext();
        var clinic = new Clinic { Name = "Active clinic", IsActive = true };
        var doctor = new DoctorProfile { UserId = "clinic-doctor" };
        var patient = new PatientProfile { UserId = "clinic-patient" };
        context.AddRange(clinic, doctor, patient, new Appointment
        {
            Clinic = clinic,
            DoctorProfile = doctor,
            PatientProfile = patient,
            AppointmentDate = new DateTime(2026, 8, 10),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Status = AppointmentStatus.Accepted
        });
        await context.SaveChangesAsync();
        var service = new ClinicService(context, new FixedClinicClock());

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.DeleteAsync(clinic.Id));

        Assert.Contains("future appointments", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(clinic.IsActive);
    }

    [Fact]
    public async Task ClinicAssignmentWithFutureAppointmentsCannotBeRemoved()
    {
        await using var context = CreateContext();
        var clinic = new Clinic { Name = "Assigned clinic", IsActive = true };
        var doctor = new DoctorProfile { UserId = "assigned-doctor" };
        var patient = new PatientProfile { UserId = "assigned-patient" };
        var assignment = new ClinicDoctor { Clinic = clinic, DoctorProfile = doctor };
        context.AddRange(clinic, doctor, patient, assignment, new Appointment
        {
            Clinic = clinic,
            DoctorProfile = doctor,
            PatientProfile = patient,
            AppointmentDate = new DateTime(2026, 8, 10),
            StartTime = new TimeOnly(11, 0),
            EndTime = new TimeOnly(11, 30),
            Status = AppointmentStatus.Pending
        });
        await context.SaveChangesAsync();
        var service = new DoctorService(
            context,
            null!,
            null!,
            new FixedClinicClock(),
            null!,
            NullLogger<DoctorService>.Instance);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.AssignClinicsAsync(doctor.Id, []));

        Assert.Contains(context.ClinicDoctors, current => current.Id == assignment.Id);
    }

    private static ApplicationUser EnabledUser(string id) => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        Email = $"{id}@example.com",
        FirstName = "Clinic",
        LastName = "Doctor",
        LockoutEnabled = true
    };

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"clinic-booking-{Guid.NewGuid()}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class FixedClinicClock : IClinicClock
    {
        public DateTime UtcNow => new(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc);
        public DateTime LocalNow => new(2026, 8, 1, 11, 0, 0, DateTimeKind.Unspecified);
    }
}
