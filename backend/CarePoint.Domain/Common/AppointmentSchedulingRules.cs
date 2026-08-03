namespace CarePoint.Domain.Common;

public static class AppointmentSchedulingRules
{
    public static bool IsInFuture(DateTime clinicLocalNow, DateTime appointmentDate, TimeOnly startTime) =>
        appointmentDate.Date > clinicLocalNow.Date ||
        (appointmentDate.Date == clinicLocalNow.Date && startTime > TimeOnly.FromDateTime(clinicLocalNow));

    public static bool IsElapsed(DateTime clinicLocalNow, DateTime slotDate, TimeOnly startTime) =>
        !IsInFuture(clinicLocalNow, slotDate, startTime);
}
