namespace CarePoint.Application.DTOs.Patients;

public class PatientDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? BloodType { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Gender { get; set; }
    public string? EmergencyContact { get; set; }
}

public class UpdatePatientDto
{
    public DateTime? DateOfBirth { get; set; }
    public string? BloodType { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Gender { get; set; }
    public string? EmergencyContact { get; set; }
}
