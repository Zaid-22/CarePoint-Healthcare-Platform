using CarePoint.Application.DTOs.Medical;
using CarePoint.Application.Validators;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Enums;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CarePoint.Tests;

public class MedicalRecordAuditTests
{
    [Fact]
    public void MedicalRecordModelUsesOptimisticConcurrency()
    {
        using var context = CreateContext();

        var property = context.Model.FindEntityType(typeof(MedicalRecord))!
            .FindProperty(nameof(MedicalRecord.RowVersion))!;

        Assert.True(property.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, property.ValueGenerated);
    }

    [Fact]
    public void UpdateRequiresReasonAndRowVersion()
    {
        var result = new UpdateMedicalRecordValidator().Validate(new UpdateMedicalRecordDto
        {
            Diagnosis = "Updated diagnosis"
        });

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(UpdateMedicalRecordDto.ChangeReason));
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(UpdateMedicalRecordDto.RowVersion));
    }

    [Fact]
    public async Task UpdatePreservesPriorSnapshotWithEditorAndReason()
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
        var record = new MedicalRecord
        {
            Appointment = appointment,
            Diagnosis = "Original diagnosis",
            Treatment = "Original treatment",
            Notes = "Original notes"
        };
        context.Add(record);
        await context.SaveChangesAsync();

        var service = new MedicalRecordService(context);
        var updated = await service.UpdateAsync(record.Id, doctor.UserId, new UpdateMedicalRecordDto
        {
            Diagnosis = "Corrected diagnosis",
            Treatment = "Corrected treatment",
            Notes = "Corrected notes",
            ChangeReason = "Lab result correction",
            RowVersion = record.RowVersion
        });

        var revision = await context.MedicalRecordRevisions.SingleAsync();
        Assert.Equal("Corrected diagnosis", updated.Diagnosis);
        Assert.Equal("Original diagnosis", revision.Diagnosis);
        Assert.Equal("Original treatment", revision.Treatment);
        Assert.Equal("Original notes", revision.Notes);
        Assert.Equal("Lab result correction", revision.ChangeReason);
        Assert.Equal(doctor.UserId, revision.EditedByUserId);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"medical-record-audit-{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }
}
