using CarePoint.Application.DTOs.Medical;
using CarePoint.Application.Validators;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Enums;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;
using CarePoint.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CarePoint.Tests;

public class PrescriptionAuditTests
{
    [Fact]
    public void PrescriptionModelUsesOptimisticConcurrency()
    {
        using var context = CreateModelContext();

        var property = context.Model.FindEntityType(typeof(Prescription))!
            .FindProperty(nameof(Prescription.RowVersion))!;

        Assert.True(property.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, property.ValueGenerated);
    }

    [Fact]
    public void UpdateRequiresReasonAndEightByteRowVersionWithoutAppointmentId()
    {
        var result = new UpdatePrescriptionValidator().Validate(new UpdatePrescriptionDto
        {
            Items =
            {
                new CreatePrescriptionItemDto
                {
                    MedicationName = "Medication",
                    Dosage = "One tablet",
                    Frequency = "Daily"
                }
            },
            RowVersion = new byte[7]
        });

        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(UpdatePrescriptionDto.ChangeReason));
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(UpdatePrescriptionDto.RowVersion));
        Assert.Null(typeof(UpdatePrescriptionDto).GetProperty(nameof(CreatePrescriptionDto.AppointmentId)));
    }

    [Fact]
    public async Task UpdatePreservesPriorNotesAndEveryMedicationWithEditorAndReason()
    {
        await using var context = CreateContext();
        var fixture = await SeedPrescriptionAsync(context);
        context.ChangeTracker.Clear();
        var storedPrescription = await context.Prescriptions.AsNoTracking()
            .SingleAsync(candidate => candidate.Id == fixture.Prescription.Id);
        var originalVersion = storedPrescription.RowVersion;
        Assert.Equal(8, originalVersion.Length);
        var service = new PrescriptionService(context);

        var updated = await service.UpdateAsync(
            fixture.Prescription.Id,
            fixture.Doctor.UserId,
            new UpdatePrescriptionDto
            {
                Notes = "Corrected notes",
                ChangeReason = "  Corrected dosage from pharmacy call  ",
                RowVersion = originalVersion,
                Items =
                {
                    new CreatePrescriptionItemDto
                    {
                        MedicationName = "Replacement medication",
                        Dosage = "20 mg",
                        Frequency = "Twice daily",
                        Duration = "5 days",
                        Instructions = "With food"
                    }
                }
            });

        var revision = await context.PrescriptionRevisions
            .Include(candidate => candidate.Items)
            .SingleAsync();

        Assert.Equal("Corrected notes", updated.Notes);
        Assert.Equal("Original notes", revision.Notes);
        Assert.Equal(fixture.Doctor.UserId, revision.EditedByUserId);
        Assert.Equal("Corrected dosage from pharmacy call", revision.ChangeReason);
        Assert.Equal(originalVersion, revision.PreviousRowVersion);
        Assert.Equal(2, revision.Items.Count);
        Assert.Contains(revision.Items, item =>
            item.MedicationName == "Original medication" && item.Dosage == "10 mg");
        Assert.Contains(revision.Items, item =>
            item.MedicationName == "Second medication" && item.Frequency == "Nightly");
        Assert.Single(updated.Items);
        Assert.Equal("Replacement medication", updated.Items[0].MedicationName);
        Assert.Equal(8, updated.RowVersion.Length);
    }

    [Fact]
    public async Task PatientAssignedDoctorAndAdminCanReadRevisionsButUnrelatedDoctorCannot()
    {
        await using var context = CreateContext();
        var fixture = await SeedPrescriptionAsync(context);
        var unrelatedDoctor = new DoctorProfile
        {
            UserId = "unrelated-doctor-user",
            ApprovalStatus = DoctorApprovalStatus.Approved
        };
        context.AddRange(
            unrelatedDoctor,
            IdentityUser(unrelatedDoctor.UserId, "Unrelated", "Doctor"));
        context.PrescriptionRevisions.Add(new PrescriptionRevision
        {
            PrescriptionId = fixture.Prescription.Id,
            EditedByUserId = fixture.Doctor.UserId,
            ChangeReason = "Prior correction",
            Notes = "Earlier notes",
            PreviousRowVersion = fixture.Prescription.RowVersion.ToArray(),
            Items =
            {
                new PrescriptionRevisionItem
                {
                    MedicationName = "Archived medication",
                    Dosage = "5 mg",
                    Frequency = "Daily"
                }
            }
        });
        await context.SaveChangesAsync();
        var service = new PrescriptionService(context);

        var patientView = await service.GetRevisionsAsync(
            fixture.Prescription.Id, fixture.Patient.UserId, "Patient");
        var doctorView = await service.GetRevisionsAsync(
            fixture.Prescription.Id, fixture.Doctor.UserId, "Doctor");
        var adminView = await service.GetRevisionsAsync(
            fixture.Prescription.Id, "admin-user", "Admin");

        Assert.Single(patientView);
        Assert.Single(doctorView);
        Assert.Single(adminView);
        Assert.Equal("Dr. Assigned Doctor", doctorView[0].EditedByName);
        Assert.Single(doctorView[0].Items);
        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetRevisionsAsync(
            fixture.Prescription.Id, unrelatedDoctor.UserId, "Doctor"));
    }

    private static async Task<PrescriptionFixture> SeedPrescriptionAsync(ApplicationDbContext context)
    {
        var patient = new PatientProfile { UserId = "patient-user" };
        var doctor = new DoctorProfile
        {
            UserId = "assigned-doctor-user",
            ApprovalStatus = DoctorApprovalStatus.Approved
        };
        var appointment = new Appointment
        {
            PatientProfile = patient,
            DoctorProfile = doctor,
            AppointmentDate = new DateTime(2026, 8, 1),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(9, 30),
            Status = AppointmentStatus.Completed
        };
        var prescription = new Prescription
        {
            Appointment = appointment,
            DoctorProfile = doctor,
            PatientProfile = patient,
            Notes = "Original notes",
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            Items =
            {
                new PrescriptionItem
                {
                    MedicationName = "Original medication",
                    Dosage = "10 mg",
                    Frequency = "Daily"
                },
                new PrescriptionItem
                {
                    MedicationName = "Second medication",
                    Dosage = "2 tablets",
                    Frequency = "Nightly"
                }
            }
        };
        context.AddRange(
            IdentityUser(patient.UserId, "Test", "Patient"),
            IdentityUser(doctor.UserId, "Assigned", "Doctor"),
            prescription);
        await context.SaveChangesAsync();
        return new PrescriptionFixture(patient, doctor, prescription);
    }

    private static ApplicationUser IdentityUser(string id, string firstName, string lastName) => new()
    {
        Id = id,
        UserName = $"{id}@example.com",
        Email = $"{id}@example.com",
        FirstName = firstName,
        LastName = lastName
    };

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WritablePrescriptionRowVersionContext>()
            .UseInMemoryDatabase($"prescription-audit-{Guid.NewGuid()}")
            .Options;
        return new WritablePrescriptionRowVersionContext(options);
    }

    private static ApplicationDbContext CreateModelContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"prescription-model-{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class WritablePrescriptionRowVersionContext(
        DbContextOptions<WritablePrescriptionRowVersionContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            var property = builder.Entity<Prescription>()
                .Property(prescription => prescription.RowVersion)
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

    private sealed record PrescriptionFixture(
        PatientProfile Patient,
        DoctorProfile Doctor,
        Prescription Prescription);
}
