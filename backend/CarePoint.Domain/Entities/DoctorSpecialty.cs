using CarePoint.Domain.Common;

namespace CarePoint.Domain.Entities;

/// <summary>
/// Junction table for many-to-many relationship between Doctor and Specialty.
/// A doctor can have multiple specialties.
/// </summary>
public class DoctorSpecialty : BaseEntity
{
    public Guid DoctorProfileId { get; set; }
    public DoctorProfile DoctorProfile { get; set; } = null!;

    public Guid SpecialtyId { get; set; }
    public Specialty Specialty { get; set; } = null!;
}
