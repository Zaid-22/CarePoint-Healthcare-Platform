using CarePoint.Domain.Common;
using CarePoint.Domain.Enums;

namespace CarePoint.Domain.Entities;

/// <summary>
/// System notification for a user. No soft delete.
/// </summary>
public class Notification : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public NotificationType Type { get; set; }

    /// <summary>
    /// Optional reference to the related entity (e.g., AppointmentId).
    /// </summary>
    public Guid? ReferenceId { get; set; }
}
