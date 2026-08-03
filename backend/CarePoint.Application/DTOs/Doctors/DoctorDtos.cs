using CarePoint.Domain.Enums;

namespace CarePoint.Application.DTOs.Doctors;

public class DoctorDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public decimal ConsultationFee { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DoctorApprovalStatus ApprovalStatus { get; set; }
    public List<SpecialtyDto> Specialties { get; set; } = new();
    public List<ClinicDto> Clinics { get; set; } = new();
}

public sealed class DoctorAdminSummaryDto
{
    public int TotalRegistered { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
}

public class CreateDoctorDto
{
    public string? Bio { get; set; }
    public decimal ConsultationFee { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public List<Guid> SpecialtyIds { get; set; } = new();
}

public class UpdateDoctorDto
{
    public string? Bio { get; set; }
    public decimal ConsultationFee { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public List<Guid> SpecialtyIds { get; set; } = new();
}

public class DoctorAvailabilityDto
{
    public Guid Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotDurationMinutes { get; set; }
}

public class CreateAvailabilityDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotDurationMinutes { get; set; } = 30;
}

public class AvailableSlotDto
{
    public DateTime Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsAvailable { get; set; }
}

public class SpecialtyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DoctorCount { get; set; }
}

public class CreateSpecialtyDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ClinicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; }
}

public class CreateClinicDto
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? City { get; set; }
}
