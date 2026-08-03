using CarePoint.Application.DTOs.Appointments;
using CarePoint.Application.DTOs.Medical;
using CarePoint.Application.DTOs.Patients;
using CarePoint.Application.Validators;
using CarePoint.Domain.Common;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Enums;
using CarePoint.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CarePoint.Tests;

public class ClinicalHardeningTests
{
    [Theory]
    [InlineData(DoctorApprovalStatus.Approved, AppointmentStatus.Accepted, true)]
    [InlineData(DoctorApprovalStatus.Approved, AppointmentStatus.InProgress, true)]
    [InlineData(DoctorApprovalStatus.Approved, AppointmentStatus.Completed, true)]
    [InlineData(DoctorApprovalStatus.Pending, AppointmentStatus.Accepted, false)]
    [InlineData(DoctorApprovalStatus.Rejected, AppointmentStatus.Completed, false)]
    [InlineData(DoctorApprovalStatus.Approved, AppointmentStatus.Pending, false)]
    [InlineData(DoctorApprovalStatus.Approved, AppointmentStatus.Cancelled, false)]
    public void ClinicalAccess_RequiresApprovalAssignmentAndCareStatus(
        DoctorApprovalStatus approval, AppointmentStatus status, bool expected)
    {
        var doctorId = Guid.NewGuid();

        Assert.Equal(expected, ClinicalAccessRules.CanDoctorAccessClinicalData(
            doctorId, doctorId, approval, status));
        Assert.False(ClinicalAccessRules.CanDoctorAccessClinicalData(
            doctorId, Guid.NewGuid(), approval, status));
    }

    [Fact]
    public void CreateMedicalRecord_EnforcesDatabaseTextLimits()
    {
        var result = new CreateMedicalRecordValidator().Validate(new CreateMedicalRecordDto
        {
            AppointmentId = Guid.NewGuid(),
            Diagnosis = "Diagnosis",
            Notes = new string('n', 4001),
            Treatment = new string('t', 4001)
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateMedicalRecordDto.Notes));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateMedicalRecordDto.Treatment));
    }

    [Fact]
    public void PrescriptionAndPatientValidators_EnforceDatabaseTextLimits()
    {
        var prescription = new CreatePrescriptionValidator().Validate(new CreatePrescriptionDto
        {
            AppointmentId = Guid.NewGuid(),
            Notes = new string('n', 2001),
            Items =
            {
                new CreatePrescriptionItemDto
                {
                    MedicationName = "Medication",
                    Dosage = "1 tablet",
                    Frequency = "Daily",
                    Duration = new string('d', 101),
                    Instructions = new string('i', 1001)
                }
            }
        });
        var patient = new UpdatePatientValidator().Validate(new UpdatePatientDto
        {
            Address = new string('a', 501)
        });

        Assert.False(prescription.IsValid);
        Assert.False(patient.IsValid);
    }

    [Fact]
    public void AppointmentModel_UsesOptimisticConcurrency()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"model-{Guid.NewGuid()}")
            .Options;
        using var context = new ApplicationDbContext(options);

        var property = context.Model.FindEntityType(typeof(Appointment))!
            .FindProperty(nameof(Appointment.RowVersion))!;

        Assert.True(property.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, property.ValueGenerated);
    }

    [Fact]
    public void AppointmentInputValidators_RejectOversizedReasonsAndNotes()
    {
        var create = new CreateAppointmentValidator().Validate(new CreateAppointmentDto
        {
            DoctorProfileId = Guid.NewGuid(),
            AppointmentDate = DateTime.UtcNow.AddDays(1),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(9, 30),
            Notes = new string('n', 2001)
        });
        var cancel = new CancelAppointmentValidator().Validate(new CancelAppointmentDto
        {
            CancellationReason = new string('r', 501)
        });

        Assert.False(create.IsValid);
        Assert.False(cancel.IsValid);
    }
}
