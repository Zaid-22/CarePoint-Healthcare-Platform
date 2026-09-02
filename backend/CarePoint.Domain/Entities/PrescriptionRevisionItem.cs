using CarePoint.Domain.Common;

namespace CarePoint.Domain.Entities;

/// <summary>
/// Medication snapshot belonging to an immutable prescription revision.
/// </summary>
public class PrescriptionRevisionItem : BaseEntity
{
    public Guid PrescriptionRevisionId { get; set; }
    public PrescriptionRevision PrescriptionRevision { get; set; } = null!;

    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string? Duration { get; set; }
    public string? Instructions { get; set; }
}
