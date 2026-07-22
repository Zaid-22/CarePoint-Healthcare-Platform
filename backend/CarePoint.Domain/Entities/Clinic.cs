using CarePoint.Domain.Common;

namespace CarePoint.Domain.Entities;

/// <summary>
/// Clinic/branch information and location.
/// Uses IsActive flag instead of hard delete to preserve referential integrity.
/// </summary>
public class Clinic : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; } = true;

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<ClinicDoctor> ClinicDoctors { get; set; } = new List<ClinicDoctor>();
}
