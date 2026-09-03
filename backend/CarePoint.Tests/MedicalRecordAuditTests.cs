using CarePoint.Application.DTOs.Medical;
using CarePoint.Application.Validators;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Enums;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CarePoint.Tests;

public class MedicalRecordAuditTests
{
    [Fact]
    public void MedicalRecordModelUsesOptimisticConcurrency()
    {
        using var context = CreateModelContext();

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
            Notes = "Original notes",
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
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

    [Fact]
    public async Task IdenticalClinicalCorrectionStillPerformsVersionedAggregateUpdate()
    {
        await using var context = CreateContext();
        var patient = new PatientProfile { UserId = "patient-user" };
        var doctor = new DoctorProfile
        {
            UserId = "doctor-user",
            ApprovalStatus = DoctorApprovalStatus.Approved
        };
        var record = new MedicalRecord
        {
            Appointment = new Appointment
            {
                PatientProfile = patient,
                DoctorProfile = doctor,
                AppointmentDate = new DateTime(2026, 7, 1),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(9, 30),
                Status = AppointmentStatus.Completed
            },
            Diagnosis = "Unchanged diagnosis",
            Treatment = "Unchanged treatment",
            Notes = "Unchanged notes",
            RowVersion = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 }
        };
        context.Add(record);
        await context.SaveChangesAsync();
        var originalUpdatedAt = record.UpdatedAt;
        var service = new MedicalRecordService(context);

        var updated = await service.UpdateAsync(record.Id, doctor.UserId, new UpdateMedicalRecordDto
        {
            Diagnosis = record.Diagnosis,
            Treatment = record.Treatment,
            Notes = record.Notes,
            ChangeReason = "Audit-only clarification",
            RowVersion = record.RowVersion.ToArray()
        });

        Assert.True(updated.UpdatedAt > originalUpdatedAt);
        Assert.Equal("Audit-only clarification",
            (await context.MedicalRecordRevisions.SingleAsync()).ChangeReason);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WritableMedicalRecordRowVersionContext>()
            .UseInMemoryDatabase($"medical-record-audit-{Guid.NewGuid()}")
            .Options;
        return new WritableMedicalRecordRowVersionContext(options);
    }

    private static ApplicationDbContext CreateModelContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"medical-record-model-{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class WritableMedicalRecordRowVersionContext(
        DbContextOptions<WritableMedicalRecordRowVersionContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            var property = builder.Entity<MedicalRecord>()
                .Property(record => record.RowVersion)
                .ValueGeneratedNever()
                .IsConcurrencyToken()
                .Metadata;
            property.SetBeforeSaveBehavior(PropertySaveBehavior.Save);
            property.SetAfterSaveBehavior(PropertySaveBehavior.Save);
            property.SetValueComparer(new ValueComparer<byte[]>(
                (left, right) => left.SequenceEqual(right),
                value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
                value => value.ToArray()));
        }
    }
}
