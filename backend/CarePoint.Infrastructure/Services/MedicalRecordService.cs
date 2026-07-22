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
        await EnsureCanReadAsync(record, userId, role);
        return await MapToDtoAsync(record);
    }

    public async Task<IReadOnlyList<MedicalRecordDto>> GetByPatientIdAsync(Guid patientId, string userId, string role)
    {
        await EnsureCanReadPatientHistoryAsync(patientId, userId, role);
        var records = await _context.MedicalRecords
            .Include(r => r.Appointment).ThenInclude(a => a.DoctorProfile)
            .Include(r => r.Appointment).ThenInclude(a => a.PatientProfile)
            .Where(r => r.Appointment.PatientProfileId == patientId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        var result = new List<MedicalRecordDto>();
        foreach (var record in records)
            result.Add(await MapToDtoAsync(record));
        return result;
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

        var appointment = await _context.Appointments.FindAsync(dto.AppointmentId)
            ?? throw new NotFoundException("Appointment", dto.AppointmentId);
        if (appointment.DoctorProfileId != doctor.Id)
            throw new ForbiddenException("You can only create records for your own appointments.");
        if (appointment.Status is not (Domain.Enums.AppointmentStatus.Accepted or Domain.Enums.AppointmentStatus.InProgress or Domain.Enums.AppointmentStatus.Completed))
            throw new BadRequestException("A medical record can only be created for an accepted or completed appointment.");
        if (await _context.MedicalRecords.AnyAsync(r => r.AppointmentId == dto.AppointmentId))
            throw new ConflictException("A medical record already exists for this appointment.");

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
        var record = await _context.MedicalRecords
            .Include(r => r.Appointment)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException("Medical Record", id);

        var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId)
            ?? throw new ForbiddenException("Only doctors can update medical records.");
        if (record.Appointment.DoctorProfileId != doctor.Id)
            throw new ForbiddenException("You can only update records for your own appointments.");

        record.Diagnosis = dto.Diagnosis;
        record.Treatment = dto.Treatment;
        record.Notes = dto.Notes;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(record.Id, userId, "Doctor");
    }

    private async Task EnsureCanReadAsync(MedicalRecord record, string userId, string role)
    {
        if (role == "Admin") return;

        if (role == "Patient" && record.Appointment.PatientProfile.UserId == userId) return;

        if (role == "Doctor")
        {
            var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor != null && await _context.Appointments.AnyAsync(a =>
                    a.DoctorProfileId == doctor.Id &&
                    a.PatientProfileId == record.Appointment.PatientProfileId))
                return;
        }

        throw new ForbiddenException();
    }

    private async Task EnsureCanReadPatientHistoryAsync(Guid patientId, string userId, string role)
    {
        var patient = await _context.PatientProfiles.FindAsync(patientId)
            ?? throw new NotFoundException("Patient", patientId);

        if (role == "Admin" || (role == "Patient" && patient.UserId == userId)) return;

        if (role == "Doctor")
        {
            var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor != null && await _context.Appointments.AnyAsync(a =>
                    a.DoctorProfileId == doctor.Id && a.PatientProfileId == patientId))
                return;
        }

        throw new ForbiddenException();
    }

    private async Task<MedicalRecordDto> MapToDtoAsync(MedicalRecord r)
    {
        var doctorUser = r.Appointment?.DoctorProfile != null
            ? await _userManager.FindByIdAsync(r.Appointment.DoctorProfile.UserId)
            : null;
        var patientUser = r.Appointment?.PatientProfile != null
            ? await _userManager.FindByIdAsync(r.Appointment.PatientProfile.UserId)
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
