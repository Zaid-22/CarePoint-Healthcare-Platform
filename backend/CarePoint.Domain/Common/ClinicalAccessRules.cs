using CarePoint.Domain.Enums;

namespace CarePoint.Domain.Common;

public static class ClinicalAccessRules
{
    public static bool CanDoctorAccessAppointment(Guid doctorProfileId, Guid appointmentDoctorProfileId) =>
        doctorProfileId == appointmentDoctorProfileId;

    public static bool CanDoctorAccessClinicalData(
        Guid doctorProfileId,
        Guid appointmentDoctorProfileId,
        DoctorApprovalStatus approvalStatus,
        AppointmentStatus appointmentStatus) =>
        approvalStatus == DoctorApprovalStatus.Approved &&
        doctorProfileId == appointmentDoctorProfileId &&
        appointmentStatus is AppointmentStatus.Accepted or
            AppointmentStatus.InProgress or AppointmentStatus.Completed;
}
