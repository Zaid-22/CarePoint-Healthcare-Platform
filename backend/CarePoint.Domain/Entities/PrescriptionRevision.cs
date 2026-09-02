using CarePoint.Domain.Common;

namespace CarePoint.Domain.Entities;

/// <summary>
/// Immutable snapshot of a prescription immediately before an authorized correction.
/// </summary>
public class PrescriptionRevision : BaseEntity
{
    public Guid PrescriptionId { get; set; }
    public Prescription Prescription { get; set; } = null!;

    public string EditedByUserId { get; set; } = string.Empty;
    public string ChangeReason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public byte[] PreviousRowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<PrescriptionRevisionItem> Items { get; set; } =
        new List<PrescriptionRevisionItem>();
}
