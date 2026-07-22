using CarePoint.Domain.Common;
using CarePoint.Domain.Enums;

namespace CarePoint.Domain.Entities;

/// <summary>
/// Appointment booking between a patient and a doctor.
/// Status transitions follow the defined state machine — no soft delete (historical record).
/// </summary>
public class Appointment : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;

    public Guid DoctorProfileId { get; set; }
    public DoctorProfile DoctorProfile { get; set; } = null!;

    public DateTime AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }

    // Navigation properties
    public MedicalRecord? MedicalRecord { get; set; }
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<MedicalDocument> MedicalDocuments { get; set; } = new List<MedicalDocument>();
}
