using CarePoint.Domain.Common;

namespace CarePoint.Domain.Entities;

/// <summary>
/// Patient profile linked to ApplicationUser via UserId.
/// Implements ISoftDeletable per the selective soft-delete policy.
/// </summary>
public class PatientProfile : BaseEntity, ISoftDeletable
{
    public string UserId { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? BloodType { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContact { get; set; }
    public string? Gender { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<MedicalDocument> MedicalDocuments { get; set; } = new List<MedicalDocument>();
}
