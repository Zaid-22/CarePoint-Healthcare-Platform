using Microsoft.EntityFrameworkCore;
using CarePoint.Application.DTOs.Medical;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Application.DTOs.Common;
using CarePoint.Domain.Common;
using CarePoint.Domain.Enums;

namespace CarePoint.Infrastructure.Services;

public class MedicalRecordService : IMedicalRecordService
{
    private readonly ApplicationDbContext _context;
    public MedicalRecordService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MedicalRecordDto> GetByIdAsync(Guid id, string userId, string role)
    {
        var record = await _context.MedicalRecords
            .Include(r => r.Appointment).ThenInclude(a => a.DoctorProfile)
            .Include(r => r.Appointment).ThenInclude(a => a.PatientProfile)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException("Medical Record", id);
        EnsureCanRead(record, userId, role);
        return await MapToDtoAsync(record);
    }

    public async Task<IReadOnlyList<MedicalRecordRevisionDto>> GetRevisionsAsync(
        Guid id, string userId, string role)
    {
        var record = await _context.MedicalRecords
            .Include(r => r.Appointment).ThenInclude(a => a.DoctorProfile)
            .Include(r => r.Appointment).ThenInclude(a => a.PatientProfile)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException("Medical Record", id);
        EnsureCanRead(record, userId, role);

        var revisions = await _context.MedicalRecordRevisions.AsNoTracking()
            .Where(revision => revision.MedicalRecordId == id)
            .OrderByDescending(revision => revision.CreatedAt)
            .ToListAsync();
        var editorIds = revisions.Select(revision => revision.EditedByUserId).Distinct().ToList();
        var editors = await _context.Users.AsNoTracking()
            .Where(user => editorIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id);

        return revisions.Select(revision =>
        {
            editors.TryGetValue(revision.EditedByUserId, out var editor);
            return new MedicalRecordRevisionDto
            {
                Id = revision.Id,
                EditedByName = editor != null
                    ? $"Dr. {editor.FirstName} {editor.LastName}"
                    : "Practitioner",
                ChangeReason = revision.ChangeReason,
                Diagnosis = revision.Diagnosis,
                Treatment = revision.Treatment,
                Notes = revision.Notes,
                EditedAt = revision.CreatedAt
            };
        }).ToList();
    }

    public async Task<PagedResult<MedicalRecordDto>> GetByPatientIdAsync(
        Guid patientId, string userId, string role, string? search = null, int skip = 0, int take = 50)
    {
        (skip, take) = Pagination.Normalize(skip, take);
        var doctorId = await GetDoctorIdForPatientHistoryAsync(patientId, userId, role);
        var query = _context.MedicalRecords
            .Include(r => r.Appointment).ThenInclude(a => a.DoctorProfile)
            .Include(r => r.Appointment).ThenInclude(a => a.PatientProfile)
            .AsNoTracking()
            .Where(r => r.Appointment.PatientProfileId == patientId);

        // A doctor may only review records from appointments they personally handled.
        if (doctorId.HasValue)
            query = query.Where(r => r.Appointment.DoctorProfileId == doctorId.Value &&
                (r.Appointment.Status == AppointmentStatus.Accepted ||
                 r.Appointment.Status == AppointmentStatus.InProgress ||
                 r.Appointment.Status == AppointmentStatus.Completed));

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(record =>
                record.Diagnosis.Contains(search) ||
                (record.Treatment != null && record.Treatment.Contains(search)) ||
                (record.Notes != null && record.Notes.Contains(search)) ||
                _context.Users.Any(user => user.Id == record.Appointment.DoctorProfile.UserId &&
                    (user.FirstName + " " + user.LastName).Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var records = await query.OrderByDescending(r => r.CreatedAt)
            .Skip(skip).Take(take).ToListAsync();
        return PagedResult<MedicalRecordDto>.Create(
            await MapManyToDtoAsync(records), totalCount, skip, take);
    }

    public async Task<PagedResult<MedicalRecordDto>> GetMyHistoryAsync(
        string userId, string? search = null, int skip = 0, int take = 50)
    {
        var patient = await _context.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        (skip, take) = Pagination.Normalize(skip, take);
        if (patient == null)
            return PagedResult<MedicalRecordDto>.Create(Array.Empty<MedicalRecordDto>(), 0, skip, take);
        return await GetByPatientIdAsync(patient.Id, userId, "Patient", search, skip, take);
    }

    public async Task<MedicalRecordDto> CreateAsync(string userId, CreateMedicalRecordDto dto)
    {
        var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId)
            ?? throw new ForbiddenException("Only doctors can create medical records.");

        var appointment = await _context.Appointments.FindAsync(dto.AppointmentId)
            ?? throw new NotFoundException("Appointment", dto.AppointmentId);
        if (doctor.ApprovalStatus != DoctorApprovalStatus.Approved ||
            !ClinicalAccessRules.CanDoctorAccessAppointment(doctor.Id, appointment.DoctorProfileId))
            throw new ForbiddenException("Only an approved treating doctor can create this record.");
        if (appointment.Status is not (AppointmentStatus.Accepted or AppointmentStatus.InProgress or AppointmentStatus.Completed))
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
        if (string.IsNullOrWhiteSpace(dto.ChangeReason))
            throw new BadRequestException("A reason is required when correcting a medical record.");

        var record = await _context.MedicalRecords
            .Include(r => r.Appointment)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException("Medical Record", id);

        var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId)
            ?? throw new ForbiddenException("Only doctors can update medical records.");
        if (!ClinicalAccessRules.CanDoctorAccessClinicalData(
                doctor.Id, record.Appointment.DoctorProfileId, doctor.ApprovalStatus, record.Appointment.Status))
            throw new ForbiddenException("Only an approved treating doctor can update this record.");

        _context.Entry(record).Property(r => r.RowVersion).OriginalValue = dto.RowVersion;
        _context.MedicalRecordRevisions.Add(new MedicalRecordRevision
        {
            MedicalRecordId = record.Id,
            EditedByUserId = userId,
            ChangeReason = dto.ChangeReason.Trim(),
            Diagnosis = record.Diagnosis,
            Treatment = record.Treatment,
            Notes = record.Notes,
            PreviousRowVersion = record.RowVersion.ToArray()
        });
        record.Diagnosis = dto.Diagnosis;
        record.Treatment = dto.Treatment;
        record.Notes = dto.Notes;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "This medical record was changed by another user. Reload it before applying your correction.");
        }
        return await GetByIdAsync(record.Id, userId, "Doctor");
    }

    private static void EnsureCanRead(MedicalRecord record, string userId, string role)
    {
        if (role == "Admin") return;

        if (role == "Patient" && record.Appointment.PatientProfile.UserId == userId) return;

        if (role == "Doctor" &&
            ClinicalAccessRules.CanDoctorAccessClinicalData(
                record.Appointment.DoctorProfile.Id,
                record.Appointment.DoctorProfileId,
                record.Appointment.DoctorProfile.ApprovalStatus,
                record.Appointment.Status) &&
            record.Appointment.DoctorProfile.UserId == userId) return;

        throw new ForbiddenException();
    }

    private async Task<Guid?> GetDoctorIdForPatientHistoryAsync(Guid patientId, string userId, string role)
    {
        var patient = await _context.PatientProfiles.FindAsync(patientId)
            ?? throw new NotFoundException("Patient", patientId);

        if (role == "Admin" || (role == "Patient" && patient.UserId == userId)) return null;

        if (role == "Doctor")
        {
            var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor?.ApprovalStatus == DoctorApprovalStatus.Approved &&
                await _context.Appointments.AnyAsync(a =>
                    a.DoctorProfileId == doctor.Id && a.PatientProfileId == patientId &&
                    (a.Status == AppointmentStatus.Accepted ||
                     a.Status == AppointmentStatus.InProgress ||
                     a.Status == AppointmentStatus.Completed)))
                return doctor.Id;
        }

        throw new ForbiddenException();
    }

    private async Task<MedicalRecordDto> MapToDtoAsync(MedicalRecord r)
    {
        var doctorUser = r.Appointment?.DoctorProfile != null
            ? await _context.Users.AsNoTracking().FirstOrDefaultAsync(
                user => user.Id == r.Appointment.DoctorProfile.UserId)
            : null;
        var patientUser = r.Appointment?.PatientProfile != null
            ? await _context.Users.AsNoTracking().FirstOrDefaultAsync(
                user => user.Id == r.Appointment.PatientProfile.UserId)
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
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            RowVersion = r.RowVersion
        };
    }

    private async Task<IReadOnlyList<MedicalRecordDto>> MapManyToDtoAsync(IReadOnlyCollection<MedicalRecord> records)
    {
        var userIds = records
            .SelectMany(record => new[]
            {
                record.Appointment.DoctorProfile.UserId,
                record.Appointment.PatientProfile.UserId
            })
            .Distinct()
            .ToList();
        var users = await _context.Users.AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id);

        return records.Select(record =>
        {
            users.TryGetValue(record.Appointment.DoctorProfile.UserId, out var doctor);
            users.TryGetValue(record.Appointment.PatientProfile.UserId, out var patient);
            return new MedicalRecordDto
            {
                Id = record.Id,
                AppointmentId = record.AppointmentId,
                DoctorName = doctor != null ? $"Dr. {doctor.FirstName} {doctor.LastName}" : "Practitioner",
                PatientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "Patient",
                AppointmentDate = record.Appointment.AppointmentDate,
                Diagnosis = record.Diagnosis,
                Treatment = record.Treatment,
                Notes = record.Notes,
                CreatedAt = record.CreatedAt,
                UpdatedAt = record.UpdatedAt,
                RowVersion = record.RowVersion
            };
        }).ToList();
    }
}
