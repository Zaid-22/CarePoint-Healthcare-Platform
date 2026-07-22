using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CarePoint.Application.DTOs.Medical;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;

namespace CarePoint.Infrastructure.Services;

public class MedicalRecordService : IMedicalRecordService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MedicalRecordService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<MedicalRecordDto> GetByIdAsync(Guid id, string userId, string role)
    {
        var record = await _context.MedicalRecords
            .Include(r => r.Appointment).ThenInclude(a => a.DoctorProfile)
            .Include(r => r.Appointment).ThenInclude(a => a.PatientProfile)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException("Medical Record", id);
        return MapToDto(record);
    }

    public async Task<IReadOnlyList<MedicalRecordDto>> GetByPatientIdAsync(Guid patientId, string userId, string role)
    {
        var records = await _context.MedicalRecords
            .Include(r => r.Appointment).ThenInclude(a => a.DoctorProfile)
            .Include(r => r.Appointment).ThenInclude(a => a.PatientProfile)
            .Where(r => r.Appointment.PatientProfileId == patientId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return records.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<MedicalRecordDto>> GetMyHistoryAsync(string userId)
    {
        var patient = await _context.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null) return new List<MedicalRecordDto>();
        return await GetByPatientIdAsync(patient.Id, userId, "Patient");
    }

    public async Task<MedicalRecordDto> CreateAsync(string userId, CreateMedicalRecordDto dto)
    {
        var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId)
            ?? throw new ForbiddenException("Only doctors can create medical records.");

        var record = new MedicalRecord
        {
            AppointmentId = dto.AppointmentId,
            Diagnosis = dto.Diagnosis,
            Treatment = dto.Treatment,
            Notes = dto.Notes
        };

        _context.MedicalRecords.Add(record);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(record.Id, userId, "Doctor");
    }

    public async Task<MedicalRecordDto> UpdateAsync(Guid id, string userId, UpdateMedicalRecordDto dto)
    {
        var record = await _context.MedicalRecords.FindAsync(id)
            ?? throw new NotFoundException("Medical Record", id);

        record.Diagnosis = dto.Diagnosis;
        record.Treatment = dto.Treatment;
        record.Notes = dto.Notes;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(record.Id, userId, "Doctor");
    }

    private MedicalRecordDto MapToDto(MedicalRecord r)
    {
        var doctorUser = r.Appointment?.DoctorProfile != null
            ? _userManager.FindByIdAsync(r.Appointment.DoctorProfile.UserId).Result
            : null;
        var patientUser = r.Appointment?.PatientProfile != null
            ? _userManager.FindByIdAsync(r.Appointment.PatientProfile.UserId).Result
            : null;

        return new MedicalRecordDto
        {
            Id = r.Id,
            AppointmentId = r.AppointmentId,
            DoctorName = doctorUser != null ? $"Dr. {doctorUser.FirstName} {doctorUser.LastName}" : "Practitioner",
            PatientName = patientUser != null ? $"{patientUser.FirstName} {patientUser.LastName}" : "Patient",
            AppointmentDate = r.Appointment?.AppointmentDate ?? r.CreatedAt,
            Diagnosis = r.Diagnosis,
            Treatment = r.Treatment,
            Notes = r.Notes,
            CreatedAt = r.CreatedAt
        };
    }
}
