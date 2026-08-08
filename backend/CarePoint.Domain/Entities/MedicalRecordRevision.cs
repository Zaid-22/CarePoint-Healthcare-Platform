using CarePoint.Domain.Common;

namespace CarePoint.Domain.Entities;

/// <summary>
/// Immutable snapshot of a medical record immediately before an authorized correction.
/// </summary>
public class MedicalRecordRevision : BaseEntity
{
    public Guid MedicalRecordId { get; set; }
    public MedicalRecord MedicalRecord { get; set; } = null!;

    public string EditedByUserId { get; set; } = string.Empty;
    public string ChangeReason { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Treatment { get; set; }
    public byte[] PreviousRowVersion { get; set; } = Array.Empty<byte>();
}
