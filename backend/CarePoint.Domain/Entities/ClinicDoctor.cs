using CarePoint.Domain.Common;

namespace CarePoint.Domain.Entities;

/// <summary>
/// Junction table for many-to-many relationship between Clinic and Doctor.
/// A doctor can work at multiple clinics, a clinic has multiple doctors.
/// </summary>
public class ClinicDoctor : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    public Guid DoctorProfileId { get; set; }
    public DoctorProfile DoctorProfile { get; set; } = null!;
}
