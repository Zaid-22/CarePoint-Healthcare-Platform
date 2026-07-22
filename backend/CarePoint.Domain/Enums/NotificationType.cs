namespace CarePoint.Domain.Enums;

public enum NotificationType
{
    AppointmentBooked = 0,
    AppointmentAccepted = 1,
    AppointmentRejected = 2,
    AppointmentCancelled = 3,
    UpcomingReminder = 4,
    NewMedicalRecord = 5,
    NewPrescription = 6,
    DoctorApproved = 7,
    DoctorRegistrationPending = 8,
    SystemAlert = 9
}
