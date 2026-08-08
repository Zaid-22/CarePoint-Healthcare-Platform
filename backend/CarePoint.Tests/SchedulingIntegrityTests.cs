using CarePoint.Application.DTOs.Appointments;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Enums;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
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
        var date = new DateTime(2026, 8, 10);

        context.AddRange(patient, requestedDoctor, otherDoctor);
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

        context.AddRange(patient, requestedDoctor, otherDoctor, appointment);
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

    private static DoctorProfile ApprovedDoctor(string userId) => new()
    {
        UserId = userId,
        ApprovalStatus = DoctorApprovalStatus.Approved
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
