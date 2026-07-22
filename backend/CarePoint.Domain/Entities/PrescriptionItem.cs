using CarePoint.Domain.Common;

namespace CarePoint.Domain.Entities;

/// <summary>
/// Individual medication within a prescription.
/// Contains dosage, frequency, duration, and instructions. No soft delete.
/// </summary>
public class PrescriptionItem : BaseEntity
{
    public Guid PrescriptionId { get; set; }
    public Prescription Prescription { get; set; } = null!;

    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string? Duration { get; set; }
    public string? Instructions { get; set; }
}
