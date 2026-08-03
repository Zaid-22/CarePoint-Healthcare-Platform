using CarePoint.Domain.Enums;

namespace CarePoint.Domain.Common;

public static class AppointmentStatusTransitions
{
    public static bool IsTerminal(AppointmentStatus status) =>
        status is AppointmentStatus.Completed or AppointmentStatus.Cancelled or
            AppointmentStatus.Rejected or AppointmentStatus.NoShow;

    public static bool CanPatientReschedule(AppointmentStatus status) =>
        status is AppointmentStatus.Pending or AppointmentStatus.Accepted;

    public static bool CanCancel(AppointmentStatus status, string role) => role switch
    {
        "Patient" or "Doctor" => status is AppointmentStatus.Pending or AppointmentStatus.Accepted,
        "Admin" => CanAdminTransition(status, AppointmentStatus.Cancelled),
        _ => false
    };

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
