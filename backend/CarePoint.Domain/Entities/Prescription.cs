using CarePoint.Domain.Common;

namespace CarePoint.Domain.Entities;

/// <summary>
/// Prescription header linked to an appointment.
/// Contains PrescriptionItems for individual medications. No soft delete (historical).
/// </summary>
public class Prescription : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;

    public Guid DoctorProfileId { get; set; }
    public DoctorProfile DoctorProfile { get; set; } = null!;

    public Guid PatientProfileId { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;

    public string? Notes { get; set; }

    // Navigation properties
    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}
