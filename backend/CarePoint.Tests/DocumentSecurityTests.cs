using CarePoint.Application.Configuration;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Enums;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace CarePoint.Tests;

public class DocumentSecurityTests
{
    [Fact]
    public async Task RejectedDoctorCannotDeletePreviouslyUploadedClinicalDocument()
    {
        await using var context = CreateContext();
        var patient = new PatientProfile { UserId = "patient-user" };
        var doctor = new DoctorProfile
        {
            UserId = "doctor-user",
            ApprovalStatus = DoctorApprovalStatus.Rejected
        };
        var appointment = new Appointment
        {
            PatientProfile = patient,
            DoctorProfile = doctor,
            AppointmentDate = new DateTime(2026, 7, 1),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(9, 30),
            Status = AppointmentStatus.Completed
        };
        var document = new MedicalDocument
        {
            PatientProfile = patient,
            Appointment = appointment,
            UploadedByUserId = doctor.UserId,
            FileName = "report.pdf",
            FileUrl = "stored-report",
            ContentType = "application/pdf",
            FileSizeBytes = 100
        };
        context.Add(document);
        await context.SaveChangesAsync();
        var storage = new RecordingStorage();
        var service = CreateService(context, storage);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.DeleteAsync(document.Id, doctor.UserId, "Doctor"));

        Assert.NotNull(await context.MedicalDocuments.FindAsync(document.Id));
        Assert.Equal(0, storage.DeleteCount);
    }

    [Fact]
    public async Task ApprovedAssignedDoctorCanDeleteOwnClinicalDocument()
    {
        await using var context = CreateContext();
        var patient = new PatientProfile { UserId = "patient-user" };
        var doctor = new DoctorProfile
        {
            UserId = "doctor-user",
            ApprovalStatus = DoctorApprovalStatus.Approved
        };
        var appointment = new Appointment
        {
            PatientProfile = patient,
            DoctorProfile = doctor,
            AppointmentDate = new DateTime(2026, 7, 1),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(9, 30),
            Status = AppointmentStatus.Completed
        };
        var document = new MedicalDocument
        {
            PatientProfile = patient,
            Appointment = appointment,
            UploadedByUserId = doctor.UserId,
            FileName = "report.pdf",
            FileUrl = "stored-report",
            ContentType = "application/pdf",
            FileSizeBytes = 100
        };
        context.Add(document);
        await context.SaveChangesAsync();
        var storage = new RecordingStorage();
        var service = CreateService(context, storage);

        await service.DeleteAsync(document.Id, doctor.UserId, "Doctor");

        Assert.Null(await context.MedicalDocuments.FindAsync(document.Id));
        Assert.Equal(1, storage.DeleteCount);
    }

    [Fact]
    public async Task UploadRejectsPatientQuotaBeforeWritingFile()
    {
        await using var context = CreateContext();
        var patient = new PatientProfile { UserId = "patient-user" };
        context.Add(patient);
        context.MedicalDocuments.Add(new MedicalDocument
        {
            PatientProfile = patient,
            UploadedByUserId = patient.UserId,
            FileName = "existing.pdf",
            FileUrl = "existing",
            ContentType = "application/pdf",
            FileSizeBytes = 7
        });
        await context.SaveChangesAsync();
        var storage = new RecordingStorage();
        var service = CreateService(context, storage, maxBytesPerPatient: 10);

        await Assert.ThrowsAsync<BadRequestException>(() => service.UploadAsync(
            patient.Id,
            patient.UserId,
            "new.pdf",
            new MemoryStream(new byte[4]),
            "application/pdf",
            null,
            4));

        Assert.Equal(0, storage.SaveCount);
        Assert.Single(context.MedicalDocuments);
    }

    private static DocumentService CreateService(
        ApplicationDbContext context, RecordingStorage storage, long maxBytesPerPatient = 1024) =>
        new(context, null!, storage, Options.Create(new MedicalDocumentSettings
        {
            MaxBytesPerPatient = maxBytesPerPatient
        }));

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"document-security-{Guid.NewGuid()}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class RecordingStorage : IMedicalDocumentStorage
    {
        public int SaveCount { get; private set; }
        public int DeleteCount { get; private set; }

        public Task<string> SaveAsync(
            Stream content, string fileExtension, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult("saved-file");
        }

        public Task<Stream> OpenReadAsync(
            string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream());

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            DeleteCount++;
            return Task.CompletedTask;
        }
    }
}
