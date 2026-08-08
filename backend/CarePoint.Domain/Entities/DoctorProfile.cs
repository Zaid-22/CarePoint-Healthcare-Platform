using CarePoint.Domain.Common;
using CarePoint.Domain.Enums;

namespace CarePoint.Domain.Entities;

/// <summary>
/// Doctor profile linked to ApplicationUser via UserId.
/// Requires admin approval before the doctor can accept appointments.
/// </summary>
public class DoctorProfile : BaseEntity, ISoftDeletable
{
    public string UserId { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public decimal ConsultationFee { get; set; }
    public DoctorApprovalStatus ApprovalStatus { get; set; } = DoctorApprovalStatus.Pending;
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? ProfilePictureStorageKey { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<DoctorSpecialty> DoctorSpecialties { get; set; } = new List<DoctorSpecialty>();
    public ICollection<ClinicDoctor> ClinicDoctors { get; set; } = new List<ClinicDoctor>();
    public ICollection<DoctorAvailability> Availabilities { get; set; } = new List<DoctorAvailability>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
