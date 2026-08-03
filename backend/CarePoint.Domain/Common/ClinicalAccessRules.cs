namespace CarePoint.Domain.Common;

public static class ClinicalAccessRules
{
    public static bool CanDoctorAccessAppointment(Guid doctorProfileId, Guid appointmentDoctorProfileId) =>
        doctorProfileId == appointmentDoctorProfileId;
}
