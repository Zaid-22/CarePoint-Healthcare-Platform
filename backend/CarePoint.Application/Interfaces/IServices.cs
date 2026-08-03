using CarePoint.Application.DTOs.Doctors;
using CarePoint.Application.DTOs.Appointments;
using CarePoint.Application.DTOs.Patients;
using CarePoint.Application.DTOs.Medical;

namespace CarePoint.Application.Interfaces;

public interface IPasswordResetEmailSender
{
    Task SendAsync(string recipientEmail, string resetUrl, CancellationToken cancellationToken = default);
}

public interface IDoctorService
{
    Task<DoctorDto> GetByIdAsync(Guid id);
    Task<DoctorDto> GetProfileByUserIdAsync(string userId);
    Task<IReadOnlyList<DoctorDto>> GetAllAsync(string? specialtyFilter = null, string? nameFilter = null, int skip = 0, int take = 50);
    Task<IReadOnlyList<DoctorDto>> GetAllForAdminAsync(int skip = 0, int take = 50);
    Task<DoctorDto> CreateProfileAsync(string userId, CreateDoctorDto dto);
    Task<DoctorDto> UpdateProfileAsync(Guid id, string userId, UpdateDoctorDto dto);
    Task<DoctorDto> UpdateProfileByUserIdAsync(string userId, UpdateDoctorDto dto);
    Task DeleteAsync(Guid id);
    Task<DoctorDto> ApproveAsync(Guid id);
    Task<DoctorDto> RejectAsync(Guid id);
    Task<IReadOnlyList<DoctorAvailabilityDto>> GetAvailabilityAsync(Guid doctorId);
    Task<DoctorAvailabilityDto> AddAvailabilityAsync(Guid doctorId, string userId, CreateAvailabilityDto dto);
    Task<DoctorAvailabilityDto> UpdateAvailabilityAsync(Guid doctorId, Guid slotId, string userId, CreateAvailabilityDto dto);
    Task DeleteAvailabilityAsync(Guid doctorId, Guid slotId, string userId);
    Task<IReadOnlyList<AvailableSlotDto>> GetAvailableSlotsAsync(Guid doctorId, DateTime date);
}

public interface IAppointmentService
{
    Task<AppointmentDto> GetByIdAsync(Guid id, string userId, string role);
    Task<IReadOnlyList<AppointmentDto>> GetAllAsync(string userId, string role, int skip = 0, int take = 50);
    Task<AppointmentDto> CreateAsync(string userId, CreateAppointmentDto dto);
    Task<AppointmentDto> UpdateStatusAsync(Guid id, string userId, string role, UpdateAppointmentStatusDto dto);
    Task<AppointmentDto> RescheduleAsync(Guid id, string userId, RescheduleAppointmentDto dto);
    Task<AppointmentDto> CancelAsync(Guid id, string userId, string role, CancelAppointmentDto dto);
}

public interface IPatientService
{
    Task<PatientDto> GetByIdAsync(Guid id, string userId, string role);
    Task<IReadOnlyList<PatientDto>> GetAllAsync(int skip = 0, int take = 50);
    Task<PatientDto> GetByUserIdAsync(string userId);
    Task<PatientDto> UpdateProfileAsync(Guid id, string userId, UpdatePatientDto dto);
    Task<PatientDto> UpdateMyProfileAsync(string userId, UpdatePatientDto dto);
}

public interface IMedicalRecordService
{
    Task<MedicalRecordDto> GetByIdAsync(Guid id, string userId, string role);
    Task<IReadOnlyList<MedicalRecordDto>> GetByPatientIdAsync(Guid patientId, string userId, string role, int skip = 0, int take = 50);
    Task<IReadOnlyList<MedicalRecordDto>> GetMyHistoryAsync(string userId, int skip = 0, int take = 50);
    Task<MedicalRecordDto> CreateAsync(string userId, CreateMedicalRecordDto dto);
    Task<MedicalRecordDto> UpdateAsync(Guid id, string userId, UpdateMedicalRecordDto dto);
}

public interface IPrescriptionService
{
    Task<PrescriptionDto> GetByIdAsync(Guid id, string userId, string role);
    Task<IReadOnlyList<PrescriptionDto>> GetByAppointmentIdAsync(Guid appointmentId, string userId, string role, int skip = 0, int take = 50);
    Task<IReadOnlyList<PrescriptionDto>> GetMyPrescriptionsAsync(string userId, int skip = 0, int take = 50);
    Task<PrescriptionDto> CreateAsync(string userId, CreatePrescriptionDto dto);
    Task<PrescriptionDto> UpdateAsync(Guid id, string userId, CreatePrescriptionDto dto);
}

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetByUserIdAsync(string userId);
    Task MarkAsReadAsync(Guid id, string userId);
    Task MarkAllAsReadAsync(string userId);
    Task CreateNotificationAsync(string userId, string title, string message,
        Domain.Enums.NotificationType type, Guid? referenceId = null);
}

public interface ISpecialtyService
{
    Task<SpecialtyDto> GetByIdAsync(Guid id);
    Task<IReadOnlyList<SpecialtyDto>> GetAllAsync();
    Task<SpecialtyDto> CreateAsync(CreateSpecialtyDto dto);
    Task<SpecialtyDto> UpdateAsync(Guid id, CreateSpecialtyDto dto);
    Task DeleteAsync(Guid id);
    Task<int> SeedSpecialtiesAsync();
}

public interface IClinicService
{
    Task<ClinicDto> GetByIdAsync(Guid id);
    Task<IReadOnlyList<ClinicDto>> GetAllAsync();
    Task<ClinicDto> CreateAsync(CreateClinicDto dto);
    Task<ClinicDto> UpdateAsync(Guid id, CreateClinicDto dto);
    Task DeleteAsync(Guid id);
}

public interface IDocumentService
{
    Task<MedicalDocumentDto> GetByIdAsync(Guid id, string userId, string role);
    Task<IReadOnlyList<MedicalDocumentDto>> GetByPatientIdAsync(Guid patientId, string userId, string role);
    Task<MedicalDocumentDto> UploadAsync(Guid patientProfileId, string userId, string fileName,
        string fileUrl, string? documentType, long fileSizeBytes, Guid? appointmentId = null);
    Task DeleteAsync(Guid id, string userId);
}
