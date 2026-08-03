namespace CarePoint.Domain.Common;

public static class AppointmentNotificationRecipients
{
    public static IReadOnlyList<string> ForActor(
        string actorRole, string patientUserId, string doctorUserId) => actorRole switch
        {
            "Patient" => new[] { doctorUserId },
            "Doctor" => new[] { patientUserId },
            "Admin" => new[] { patientUserId, doctorUserId }.Distinct().ToArray(),
            _ => Array.Empty<string>()
        };
}
