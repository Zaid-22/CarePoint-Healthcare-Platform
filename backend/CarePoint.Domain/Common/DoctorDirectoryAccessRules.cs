using CarePoint.Domain.Enums;

namespace CarePoint.Domain.Common;

public static class DoctorDirectoryAccessRules
{
    public static bool CanViewAvailability(
        DoctorApprovalStatus approvalStatus,
        string profileUserId,
        string? requesterUserId,
        string? requesterRole) =>
        approvalStatus == DoctorApprovalStatus.Approved ||
        requesterRole == "Admin" ||
        (requesterRole == "Doctor" && profileUserId == requesterUserId);
}

public readonly record struct AppointmentWindow(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);
public readonly record struct AvailabilityWindow(
    DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, int SlotDurationMinutes);

public static class AvailabilityCoverageRules
{
    public static bool WouldInvalidateBooking(
        IEnumerable<AppointmentWindow> appointments,
        IEnumerable<AvailabilityWindow> remainingAvailability)
    {
        var availability = remainingAvailability.ToList();
        return appointments.Any(appointment => !availability.Any(schedule =>
        {
            if (schedule.SlotDurationMinutes <= 0) return false;
            var durationMatches = appointment.EndTime.AddMinutes(-schedule.SlotDurationMinutes) ==
                                  appointment.StartTime;
            var offset = appointment.StartTime.ToTimeSpan() - schedule.StartTime.ToTimeSpan();
            var alignsToGrid = offset.Ticks >= 0 &&
                offset.Ticks % TimeSpan.FromMinutes(schedule.SlotDurationMinutes).Ticks == 0;
            return schedule.DayOfWeek == appointment.DayOfWeek &&
                   schedule.StartTime <= appointment.StartTime &&
                   schedule.EndTime >= appointment.EndTime &&
                   durationMatches && alignsToGrid;
        }));
    }
}
