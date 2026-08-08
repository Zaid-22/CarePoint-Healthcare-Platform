using CarePoint.Domain.Common;

namespace CarePoint.Domain.Entities;

/// <summary>
/// Medical record — one per appointment (1:1 relationship).
/// Contains diagnosis, notes, and treatment. No soft delete (historical/auditable).
/// </summary>
public class MedicalRecord : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;

    public string Diagnosis { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Treatment { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<MedicalRecordRevision> Revisions { get; set; } = new List<MedicalRecordRevision>();
}
