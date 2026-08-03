using CarePoint.Domain.Common;

namespace CarePoint.Domain.Entities;

/// <summary>
/// Uploaded medical document (lab results, scans, reports).
/// Owned by PatientId, optionally linked to an AppointmentId. No soft delete.
/// </summary>
public class MedicalDocument : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;

    /// <summary>
    /// Optional link to a specific appointment. Null if uploaded independently.
    /// </summary>
    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public string UploadedByUserId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public string? DocumentType { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime? DeletionRequestedAt { get; set; }
}
