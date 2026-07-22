namespace CarePoint.Domain.Common;

/// <summary>
/// Interface for entities that support soft deletion.
/// Applied selectively — NOT on historical/auditable entities (Appointment, MedicalRecord, Prescription).
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
