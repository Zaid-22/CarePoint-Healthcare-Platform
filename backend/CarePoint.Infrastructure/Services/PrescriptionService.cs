using Microsoft.EntityFrameworkCore;
using CarePoint.Application.DTOs.Medical;
using CarePoint.Application.Interfaces;
using CarePoint.Domain;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Application.DTOs.Common;
using CarePoint.Domain.Common;
using CarePoint.Domain.Enums;
using System.Data;

namespace CarePoint.Infrastructure.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly ApplicationDbContext _context;

    public PrescriptionService(ApplicationDbContext context) => _context = context;

    public async Task<PrescriptionDto> GetByIdAsync(Guid id, string userId, string role)
    {
        var rx = await _context.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Prescription", id);
        await EnsureCanReadAsync(rx, userId, role);
        return await MapToDtoAsync(rx);
    }

    public async Task<IReadOnlyList<PrescriptionRevisionDto>> GetRevisionsAsync(
        Guid id, string userId, string role)
    {
        var prescription = await _context.Prescriptions.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id)
            ?? throw new NotFoundException("Prescription", id);
        await EnsureCanReadAsync(prescription, userId, role);

        var revisions = await _context.PrescriptionRevisions
            .Include(revision => revision.Items)
            .AsNoTracking()
            .Where(revision => revision.PrescriptionId == id)
            .OrderByDescending(revision => revision.CreatedAt)
            .ToListAsync();
        var editorIds = revisions.Select(revision => revision.EditedByUserId).Distinct().ToList();
        var editors = await _context.Users.AsNoTracking()
            .Where(user => editorIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id);

        return revisions.Select(revision =>
        {
            editors.TryGetValue(revision.EditedByUserId, out var editor);
            return new PrescriptionRevisionDto
            {
                Id = revision.Id,
                EditedByName = editor != null
                    ? $"Dr. {editor.FirstName} {editor.LastName}"
                    : "Practitioner",
                ChangeReason = revision.ChangeReason,
                Notes = revision.Notes,
                EditedAt = revision.CreatedAt,
                Items = revision.Items
                    .OrderBy(item => item.CreatedAt)
                    .Select(item => new PrescriptionItemDto
                    {
                        Id = item.Id,
                        MedicationName = item.MedicationName,
                        Dosage = item.Dosage,
                        Frequency = item.Frequency,
                        Duration = item.Duration,
                        Instructions = item.Instructions
                    }).ToList()
            };
        }).ToList();
    }

    public async Task<PagedResult<PrescriptionDto>> GetByAppointmentIdAsync(
        Guid appointmentId, string userId, string role, int skip = 0, int take = 50)
    {
        (skip, take) = Pagination.Normalize(skip, take);
        var appointment = await _context.Appointments.FindAsync(appointmentId)
            ?? throw new NotFoundException("Appointment", appointmentId);
        await EnsureCanReadAppointmentAsync(appointment, userId, role);

        var query = _context.Prescriptions
            .Include(p => p.Items)
            .AsNoTracking()
            .Where(p => p.AppointmentId == appointmentId);
        var totalCount = await query.CountAsync();
        var prescriptions = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return PagedResult<PrescriptionDto>.Create(
            await MapManyToDtoAsync(prescriptions), totalCount, skip, take);
    }

    public async Task<PagedResult<PrescriptionDto>> GetMyPrescriptionsAsync(
        string userId, string? search = null, int skip = 0, int take = 50)
    {
        (skip, take) = Pagination.Normalize(skip, take);
        var patient = await _context.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null)
            return PagedResult<PrescriptionDto>.Create(Array.Empty<PrescriptionDto>(), 0, skip, take);

        var query = _context.Prescriptions
            .Include(p => p.Items)
            .AsNoTracking()
            .Where(p => p.PatientProfileId == patient.Id);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(prescription =>
                (prescription.Notes != null && prescription.Notes.Contains(search)) ||
                prescription.Items.Any(item => item.MedicationName.Contains(search)) ||
                _context.Users.Any(user => user.Id == prescription.DoctorProfile.UserId &&
                    (user.FirstName + " " + user.LastName).Contains(search)));
        }
        var totalCount = await query.CountAsync();
        var prescriptions = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return PagedResult<PrescriptionDto>.Create(
            await MapManyToDtoAsync(prescriptions), totalCount, skip, take);
    }

    public async Task<PrescriptionDto> CreateAsync(string userId, CreatePrescriptionDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId)
            ?? throw new ForbiddenException("Only doctors can create prescriptions.");

        var appointment = await _context.Appointments.FindAsync(dto.AppointmentId)
            ?? throw new NotFoundException("Appointment", dto.AppointmentId);
        if (doctor.ApprovalStatus != DoctorApprovalStatus.Approved ||
            !ClinicalAccessRules.CanDoctorAccessAppointment(doctor.Id, appointment.DoctorProfileId))
            throw new ForbiddenException("Only an approved treating doctor can issue this prescription.");
        if (appointment.Status is not (AppointmentStatus.Accepted or AppointmentStatus.InProgress or AppointmentStatus.Completed))
            throw new BadRequestException("A prescription can only be issued for an accepted or completed appointment.");
        ValidateItems(dto.Items);

        var rx = new Prescription
        {
            AppointmentId = dto.AppointmentId,
            DoctorProfileId = doctor.Id,
            PatientProfileId = appointment.PatientProfileId,
            Notes = dto.Notes,
            Items = dto.Items.Select(i => new PrescriptionItem
            {
                MedicationName = i.MedicationName,
                Dosage = i.Dosage,
                Frequency = i.Frequency,
                Duration = i.Duration,
                Instructions = i.Instructions
            }).ToList()
        };

        _context.Prescriptions.Add(rx);
        await _context.SaveChangesAsync();
        var result = await MapToDtoAsync(rx);
        await transaction.CommitAsync();
        return result;
    }

    public async Task<PrescriptionDto> UpdateAsync(Guid id, string userId, UpdatePrescriptionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ChangeReason))
            throw new BadRequestException("A reason is required when correcting a prescription.");
        if (dto.RowVersion == null || dto.RowVersion.Length != 8)
            throw new BadRequestException("A valid prescription row version is required.");
        ValidateItems(dto.Items);

        var rx = await _context.Prescriptions
            .Include(p => p.Items)
            .Include(p => p.Appointment)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Prescription", id);

        var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId)
            ?? throw new ForbiddenException("Only doctors can update prescriptions.");
        if (!ClinicalAccessRules.CanDoctorAccessClinicalData(
                doctor.Id, rx.DoctorProfileId, doctor.ApprovalStatus, rx.Appointment.Status))
            throw new ForbiddenException("Only an approved treating doctor can update this prescription.");

        _context.Entry(rx).Property(prescription => prescription.RowVersion).OriginalValue = dto.RowVersion;
        var previousItems = rx.Items.ToList();
        _context.PrescriptionRevisions.Add(new PrescriptionRevision
        {
            PrescriptionId = rx.Id,
            EditedByUserId = userId,
            ChangeReason = dto.ChangeReason.Trim(),
            Notes = rx.Notes,
            PreviousRowVersion = rx.RowVersion.ToArray(),
            Items = previousItems.Select(item => new PrescriptionRevisionItem
            {
                MedicationName = item.MedicationName,
                Dosage = item.Dosage,
                Frequency = item.Frequency,
                Duration = item.Duration,
                Instructions = item.Instructions
            }).ToList()
        });

        rx.Notes = dto.Notes;
        var replacementItems = dto.Items.Select(item => new PrescriptionItem
        {
            PrescriptionId = rx.Id,
            MedicationName = item.MedicationName,
            Dosage = item.Dosage,
            Frequency = item.Frequency,
            Duration = item.Duration,
            Instructions = item.Instructions
        }).ToList();
        _context.PrescriptionItems.RemoveRange(previousItems);
        _context.PrescriptionItems.AddRange(replacementItems);
        rx.UpdatedAt = DateTime.UtcNow;
        _context.Entry(rx).Property(prescription => prescription.UpdatedAt).IsModified = true;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "This prescription was changed by another user. Reload it before applying your correction.");
        }
        rx.Items = replacementItems;
        return await MapToDtoAsync(rx);
    }

    private async Task EnsureCanReadAsync(Prescription prescription, string userId, string role)
    {
        if (role == Roles.Admin) return;

        var appointment = await _context.Appointments.FindAsync(prescription.AppointmentId)
            ?? throw new NotFoundException("Appointment", prescription.AppointmentId);
        await EnsureCanReadAppointmentAsync(appointment, userId, role);
    }

    private async Task EnsureCanReadAppointmentAsync(Appointment appointment, string userId, string role)
    {
        if (role == Roles.Admin) return;

        var patient = await _context.PatientProfiles.FindAsync(appointment.PatientProfileId);
        if (role == Roles.Patient && patient?.UserId == userId) return;

        if (role == Roles.Doctor)
        {
            var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor != null && ClinicalAccessRules.CanDoctorAccessClinicalData(
                    doctor.Id, appointment.DoctorProfileId, doctor.ApprovalStatus, appointment.Status))
                return;
        }

        throw new ForbiddenException();
    }

    private static void ValidateItems(IReadOnlyCollection<CreatePrescriptionItemDto>? items)
    {
        if (items == null || items.Count == 0)
            throw new BadRequestException("At least one medication is required.");
        if (items.Any(item => string.IsNullOrWhiteSpace(item.MedicationName) ||
                              string.IsNullOrWhiteSpace(item.Dosage) ||
                              string.IsNullOrWhiteSpace(item.Frequency)))
            throw new BadRequestException("Medication name, dosage, and frequency are required.");
    }

    private async Task<PrescriptionDto> MapToDtoAsync(Prescription p)
    {
        var docName = "Practitioner";
        var patName = "Patient";

        var doctor = await _context.DoctorProfiles.FindAsync(p.DoctorProfileId);
        if (doctor != null)
        {
            var docUser = await _context.Users.FindAsync(doctor.UserId);
            if (docUser != null) docName = $"Dr. {docUser.FirstName} {docUser.LastName}";
        }

        var patient = await _context.PatientProfiles.FindAsync(p.PatientProfileId);
        if (patient != null)
        {
            var patUser = await _context.Users.FindAsync(patient.UserId);
            if (patUser != null) patName = $"{patUser.FirstName} {patUser.LastName}";
        }

        return new PrescriptionDto
        {
            Id = p.Id,
            AppointmentId = p.AppointmentId,
            DoctorProfileId = p.DoctorProfileId,
            DoctorName = docName,
            PatientProfileId = p.PatientProfileId,
            PatientName = patName,
            Notes = p.Notes,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            RowVersion = p.RowVersion,
            Items = p.Items.Select(i => new PrescriptionItemDto
            {
                Id = i.Id,
                MedicationName = i.MedicationName,
                Dosage = i.Dosage,
                Frequency = i.Frequency,
                Duration = i.Duration,
                Instructions = i.Instructions
            }).ToList()
        };
    }

    private async Task<IReadOnlyList<PrescriptionDto>> MapManyToDtoAsync(IReadOnlyCollection<Prescription> prescriptions)
    {
        var doctorIds = prescriptions.Select(p => p.DoctorProfileId).Distinct().ToList();
        var patientIds = prescriptions.Select(p => p.PatientProfileId).Distinct().ToList();
        var doctorNames = await (from doctor in _context.DoctorProfiles.AsNoTracking()
                                 join user in _context.Users.AsNoTracking() on doctor.UserId equals user.Id
                                 where doctorIds.Contains(doctor.Id)
                                 select new { doctor.Id, Name = "Dr. " + user.FirstName + " " + user.LastName })
            .ToDictionaryAsync(item => item.Id, item => item.Name);
        var patientNames = await (from patient in _context.PatientProfiles.AsNoTracking()
                                  join user in _context.Users.AsNoTracking() on patient.UserId equals user.Id
                                  where patientIds.Contains(patient.Id)
                                  select new { patient.Id, Name = user.FirstName + " " + user.LastName })
            .ToDictionaryAsync(item => item.Id, item => item.Name);

        return prescriptions.Select(p => new PrescriptionDto
        {
            Id = p.Id,
            AppointmentId = p.AppointmentId,
            DoctorProfileId = p.DoctorProfileId,
            DoctorName = doctorNames.GetValueOrDefault(p.DoctorProfileId, "Practitioner"),
            PatientProfileId = p.PatientProfileId,
            PatientName = patientNames.GetValueOrDefault(p.PatientProfileId, "Patient"),
            Notes = p.Notes,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            RowVersion = p.RowVersion,
            Items = p.Items.Select(i => new PrescriptionItemDto
            {
                Id = i.Id,
                MedicationName = i.MedicationName,
                Dosage = i.Dosage,
                Frequency = i.Frequency,
                Duration = i.Duration,
                Instructions = i.Instructions
            }).ToList()
        }).ToList();
    }
}
