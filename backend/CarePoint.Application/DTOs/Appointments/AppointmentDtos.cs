using CarePoint.Domain.Enums;

namespace CarePoint.Application.DTOs.Appointments;

public sealed class AppointmentSummaryDto
{
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int UpcomingCount { get; set; }
    public int TodayCount { get; set; }
}

public class AppointmentDto
{
    public Guid Id { get; set; }
    public Guid PatientProfileId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid DoctorProfileId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAppointmentDto
{
    public Guid DoctorProfileId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Notes { get; set; }
}

public class UpdateAppointmentStatusDto
{
    public AppointmentStatus Status { get; set; }
    public string? CancellationReason { get; set; }
}

public class RescheduleAppointmentDto
{
    public DateTime NewAppointmentDate { get; set; }
    public TimeOnly NewStartTime { get; set; }
    public TimeOnly NewEndTime { get; set; }
}

public class CancelAppointmentDto
{
    public string? CancellationReason { get; set; }
}
