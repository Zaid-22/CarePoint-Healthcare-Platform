using CarePoint.Domain.Common;

namespace CarePoint.Domain.Entities;

/// <summary>
/// Doctor's recurring weekly schedule.
/// Slots are generated dynamically from this data — not stored per slot.
/// </summary>
public class DoctorAvailability : BaseEntity
{
    public Guid DoctorProfileId { get; set; }
    public DoctorProfile DoctorProfile { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    /// <summary>
    /// Duration of each slot in minutes (default 30).
    /// </summary>
    public int SlotDurationMinutes { get; set; } = 30;
}
