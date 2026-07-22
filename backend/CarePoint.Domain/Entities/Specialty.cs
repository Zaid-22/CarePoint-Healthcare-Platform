using CarePoint.Domain.Common;

namespace CarePoint.Domain.Entities;

/// <summary>
/// Medical specialty (e.g., Cardiology, Dermatology).
/// Uses IsActive flag instead of hard delete to preserve referential integrity.
/// </summary>
public class Specialty : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<DoctorSpecialty> DoctorSpecialties { get; set; } = new List<DoctorSpecialty>();
}
