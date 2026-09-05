using CarePoint.Application.DTOs.Appointments;
using CarePoint.Application.DTOs.Medical;
using CarePoint.Application.Interfaces;
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

namespace CarePoint.Tests;

public class EncounterSchedulingTests
{
    [Theory]
    [InlineData("record")]
    [InlineData("prescription")]
    [InlineData("none")]
    public async Task OnlyUndocumentedAppointmentsCanBeRescheduled(string documentation)
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var patientUser = new ApplicationUser { UserName = "patient@example.test", Email = "patient@example.test" };
        var doctorUser = new ApplicationUser { UserName = "doctor@example.test", Email = "doctor@example.test" };
        await users.CreateAsync(patientUser);
        await users.CreateAsync(doctorUser);
        var patient = new PatientProfile { UserId = patientUser.Id };
        var doctor = new DoctorProfile
        {
            UserId = doctorUser.Id, ApprovalStatus = DoctorApprovalStatus.Approved,
            LicenseNumber = "TEST-123", LicensingAuthority = "Test authority",
            LicenseExpiryDate = new DateTime(2027, 8, 1), CredentialsReviewedAt = new DateTime(2026, 7, 1)
        };
        var clinic = new Clinic { Name = "Test clinic", IsActive = true };
        var originalDate = new DateTime(2026, 8, 10);
        var appointment = new Appointment
        {
            PatientProfile = patient, DoctorProfile = doctor, Clinic = clinic,
            AppointmentDate = originalDate, StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30), Status = AppointmentStatus.Accepted
        };
        context.AddRange(patient, doctor, clinic, appointment);
        context.ClinicDoctors.Add(new ClinicDoctor { Clinic = clinic, DoctorProfile = doctor });
        context.DoctorAvailabilities.Add(new DoctorAvailability
        {
            DoctorProfile = doctor, DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(12, 0), SlotDurationMinutes = 30
        });
        await context.SaveChangesAsync();
        var records = new MedicalRecordService(context);
        if (documentation == "record")
            await records.CreateAsync(doctor.UserId, new CreateMedicalRecordDto { AppointmentId = appointment.Id, Diagnosis = "Original diagnosis" });
        if (documentation == "prescription")
            await new PrescriptionService(context).CreateAsync(doctor.UserId, new CreatePrescriptionDto
            {
                AppointmentId = appointment.Id,
                Items = [new CreatePrescriptionItemDto { MedicationName = "Test medication", Dosage = "1 tablet", Frequency = "Daily" }]
            });
        var notificationsBefore = await context.Notifications.CountAsync();
        var service = new AppointmentService(context, users, new NotificationService(context), new FixedClock());
        var request = new RescheduleAppointmentDto
        {
            ClinicId = clinic.Id, NewAppointmentDate = originalDate.AddDays(7),
            NewStartTime = new TimeOnly(11, 0), NewEndTime = new TimeOnly(11, 30)
        };

        if (documentation == "none")
        {
            var rescheduled = await service.RescheduleAsync(appointment.Id, patient.UserId, request);
            Assert.Equal(originalDate.AddDays(7), rescheduled.AppointmentDate);
            Assert.Equal(AppointmentStatus.Pending, rescheduled.Status);
        }
        else
        {
            await Assert.ThrowsAsync<ConflictException>(() => service.RescheduleAsync(appointment.Id, patient.UserId, request));
            Assert.Equal(originalDate, appointment.AppointmentDate);
            Assert.Equal(new TimeOnly(10, 0), appointment.StartTime);
            Assert.Equal(AppointmentStatus.Accepted, appointment.Status);
            Assert.Equal(notificationsBefore, await context.Notifications.CountAsync());
            if (documentation == "record")
            {
                var record = await context.MedicalRecords.SingleAsync();
                var history = await records.GetByIdAsync(record.Id, doctor.UserId, "Doctor");
                Assert.Equal(originalDate, history.AppointmentDate);
            }
        }
    }

    private sealed class FixedClock : IClinicClock
    {
        public DateTime UtcNow => new(2026, 8, 1);
        public DateTime LocalNow => new(2026, 8, 1);
    }
}
