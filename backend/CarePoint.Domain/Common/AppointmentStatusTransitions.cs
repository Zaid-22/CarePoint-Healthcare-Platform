using CarePoint.Domain.Enums;

namespace CarePoint.Domain.Common;

public static class AppointmentStatusTransitions
{
    public static bool CanDoctorTransition(AppointmentStatus current, AppointmentStatus requested) =>
        (current, requested) switch
        {
            (AppointmentStatus.Pending, AppointmentStatus.Accepted or AppointmentStatus.Rejected) => true,
            (AppointmentStatus.Accepted, AppointmentStatus.InProgress or AppointmentStatus.Completed) => true,
            (AppointmentStatus.InProgress, AppointmentStatus.Completed) => true,
            _ => false
        };

    public static bool CanAdminTransition(AppointmentStatus current, AppointmentStatus requested) =>
        (current, requested) switch
        {
            (AppointmentStatus.Pending, AppointmentStatus.Accepted or AppointmentStatus.Rejected or AppointmentStatus.Cancelled) => true,
            (AppointmentStatus.Accepted, AppointmentStatus.InProgress or AppointmentStatus.Completed or AppointmentStatus.Cancelled or AppointmentStatus.NoShow) => true,
            (AppointmentStatus.InProgress, AppointmentStatus.Completed or AppointmentStatus.Cancelled or AppointmentStatus.NoShow) => true,
            _ => false
        };
}
