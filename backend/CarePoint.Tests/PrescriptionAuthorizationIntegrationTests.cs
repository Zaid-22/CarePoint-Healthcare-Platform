using CarePoint.Domain.Entities;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CarePoint.Tests;

public class PrescriptionAuthorizationIntegrationTests
{
    [Fact]
    public async Task DoctorCannotReadAnotherDoctorsAppointmentForASharedPatient()
    {
        await using var context = CreateContext();
        var patient = new PatientProfile { UserId = "patient-user" };
        var requestingDoctor = new DoctorProfile { UserId = "requesting-doctor" };
        var assignedDoctor = new DoctorProfile { UserId = "assigned-doctor" };
        var priorAppointment = new Appointment
        {
            PatientProfile = patient,
            DoctorProfile = requestingDoctor,
            AppointmentDate = new DateTime(2026, 8, 1),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(9, 30)
        };
        var protectedAppointment = new Appointment
        {
            PatientProfile = patient,
            DoctorProfile = assignedDoctor,
            AppointmentDate = new DateTime(2026, 8, 2),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(9, 30)
        };

        context.AddRange(patient, requestingDoctor, assignedDoctor, priorAppointment, protectedAppointment);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);
        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByAppointmentIdAsync(
            protectedAppointment.Id, requestingDoctor.UserId, "Doctor"));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"prescription-auth-{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }
}
