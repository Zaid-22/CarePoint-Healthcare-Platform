using CarePoint.Application.DTOs.Appointments;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Enums;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;
using CarePoint.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CarePoint.Tests;

public class SchedulingIntegrityTests
{
    [Fact]
    public async Task CreateRejectsPatientOverlapWithDifferentDoctor()
    {
        await using var context = CreateContext();
        var patient = new PatientProfile { UserId = "patient-user" };
        var requestedDoctor = ApprovedDoctor("requested-doctor");
        var otherDoctor = ApprovedDoctor("other-doctor");
        var clinic = ActiveClinic("Requested clinic");
        var date = new DateTime(2026, 8, 10);

        context.AddRange(
            patient,
            requestedDoctor,
            otherDoctor,
            clinic,
            EnabledUser(requestedDoctor.UserId),
            EnabledUser(otherDoctor.UserId));
        context.ClinicDoctors.Add(new ClinicDoctor
        {
            Clinic = clinic,
            DoctorProfile = requestedDoctor
        });
        context.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorProfile = requestedDoctor,
            DayOfWeek = date.DayOfWeek,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            SlotDurationMinutes = 30
        });
        context.Appointments.Add(new Appointment
        {
            PatientProfile = patient,
            DoctorProfile = otherDoctor,
            AppointmentDate = date,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Status = AppointmentStatus.Accepted
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            patient.UserId,
            new CreateAppointmentDto
            {
                DoctorProfileId = requestedDoctor.Id,
                ClinicId = clinic.Id,
                AppointmentDate = date,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30)
            }));
    }

    [Fact]
    public async Task RescheduleRejectsPatientOverlapWithDifferentDoctor()
    {
        await using var context = CreateContext();
        var patient = new PatientProfile { UserId = "patient-user" };
        var requestedDoctor = ApprovedDoctor("requested-doctor");
        var otherDoctor = ApprovedDoctor("other-doctor");
        var newDate = new DateTime(2026, 8, 10);
        var appointment = new Appointment
        {
            PatientProfile = patient,
            DoctorProfile = requestedDoctor,
            AppointmentDate = new DateTime(2026, 8, 8),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(9, 30),
            Status = AppointmentStatus.Pending
        };

        context.AddRange(
            patient,
            requestedDoctor,
            otherDoctor,
            appointment,
            EnabledUser(requestedDoctor.UserId),
            EnabledUser(otherDoctor.UserId));
        context.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorProfile = requestedDoctor,
            DayOfWeek = newDate.DayOfWeek,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            SlotDurationMinutes = 30
        });
        context.Appointments.Add(new Appointment
        {
            PatientProfile = patient,
            DoctorProfile = otherDoctor,
            AppointmentDate = newDate,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Status = AppointmentStatus.Pending
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await Assert.ThrowsAsync<ConflictException>(() => service.RescheduleAsync(
            appointment.Id,
            patient.UserId,
            new RescheduleAppointmentDto
            {
                NewAppointmentDate = newDate,
                NewStartTime = new TimeOnly(10, 0),
                NewEndTime = new TimeOnly(10, 30)
            }));
    }

    [Fact]
    public async Task DisabledDoctorRejectsNewBookingAndReschedule()
    {
        await using var context = CreateContext();
        var patient = new PatientProfile { UserId = "patient-user" };
        var doctor = ApprovedDoctor("disabled-doctor");
        var clinic = ActiveClinic("Disabled doctor clinic");
        var disabledUser = new ApplicationUser
        {
            Id = doctor.UserId,
            UserName = "disabled-doctor@example.com",
            Email = "disabled-doctor@example.com",
            FirstName = "Disabled",
            LastName = "Doctor",
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.MaxValue
        };
        var newDate = new DateTime(2026, 8, 10);
        var existingAppointment = new Appointment
        {
            PatientProfile = patient,
            DoctorProfile = doctor,
            AppointmentDate = new DateTime(2026, 8, 8),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(9, 30),
            Status = AppointmentStatus.Pending
        };
        context.AddRange(patient, doctor, clinic, disabledUser, existingAppointment);
        context.ClinicDoctors.Add(new ClinicDoctor
        {
            Clinic = clinic,
            DoctorProfile = doctor
        });
        context.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorProfile = doctor,
            DayOfWeek = newDate.DayOfWeek,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            SlotDurationMinutes = 30
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(
            patient.UserId,
            new CreateAppointmentDto
            {
                DoctorProfileId = doctor.Id,
                ClinicId = clinic.Id,
                AppointmentDate = newDate,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 30)
            }));
        await Assert.ThrowsAsync<BadRequestException>(() => service.RescheduleAsync(
            existingAppointment.Id,
            patient.UserId,
            new RescheduleAppointmentDto
            {
                NewAppointmentDate = newDate,
                NewStartTime = new TimeOnly(10, 0),
                NewEndTime = new TimeOnly(10, 30)
            }));

        Assert.Single(context.Appointments);
    }

    private static DoctorProfile ApprovedDoctor(string userId) => new()
    {
        UserId = userId,
        ApprovalStatus = DoctorApprovalStatus.Approved,
        LicenseNumber = $"JMC-{userId}",
        LicensingAuthority = "Jordan Medical Council",
        LicenseExpiryDate = new DateTime(2027, 8, 1),
        CredentialsReviewedAt = new DateTime(2026, 7, 1)
    };

    private static Clinic ActiveClinic(string name) => new()
    {
        Name = name,
        IsActive = true
    };

    private static ApplicationUser EnabledUser(string id) => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        Email = $"{id}@example.com",
        FirstName = "Enabled",
        LastName = "Doctor",
        LockoutEnabled = true
    };

    private static AppointmentService CreateService(ApplicationDbContext context) =>
        new(context, null!, null!, new FixedClinicClock());

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"scheduling-{Guid.NewGuid()}")
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
