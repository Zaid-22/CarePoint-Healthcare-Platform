using Microsoft.EntityFrameworkCore;
using CarePoint.Application.DTOs.Medical;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;

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

    public async Task<IReadOnlyList<PrescriptionDto>> GetByAppointmentIdAsync(
        Guid appointmentId, string userId, string role, int skip = 0, int take = 50)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 100);
        var appointment = await _context.Appointments.FindAsync(appointmentId)
            ?? throw new NotFoundException("Appointment", appointmentId);
        await EnsureCanReadAppointmentAsync(appointment, userId, role);

        var prescriptions = await _context.Prescriptions
            .Include(p => p.Items)
            .AsNoTracking()
            .Where(p => p.AppointmentId == appointmentId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return await MapManyToDtoAsync(prescriptions);
    }

    public async Task<IReadOnlyList<PrescriptionDto>> GetMyPrescriptionsAsync(string userId, int skip = 0, int take = 50)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 100);
        var patient = await _context.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null) return new List<PrescriptionDto>();

        var prescriptions = await _context.Prescriptions
            .Include(p => p.Items)
            .AsNoTracking()
            .Where(p => p.PatientProfileId == patient.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return await MapManyToDtoAsync(prescriptions);
    }

    public async Task<PrescriptionDto> CreateAsync(string userId, CreatePrescriptionDto dto)
    {
        var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId)
            ?? throw new ForbiddenException("Only doctors can create prescriptions.");

        var appointment = await _context.Appointments.FindAsync(dto.AppointmentId)
            ?? throw new NotFoundException("Appointment", dto.AppointmentId);
        if (appointment.DoctorProfileId != doctor.Id)
            throw new ForbiddenException("You can only issue prescriptions for your own appointments.");
        if (appointment.Status is not (Domain.Enums.AppointmentStatus.Accepted or Domain.Enums.AppointmentStatus.InProgress or Domain.Enums.AppointmentStatus.Completed))
            throw new BadRequestException("A prescription can only be issued for an accepted or completed appointment.");
        ValidateItems(dto);

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
        return await MapToDtoAsync(rx);
    }

    public async Task<PrescriptionDto> UpdateAsync(Guid id, string userId, CreatePrescriptionDto dto)
    {
        var rx = await _context.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Prescription", id);

        var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId)
            ?? throw new ForbiddenException("Only doctors can update prescriptions.");
        if (rx.DoctorProfileId != doctor.Id)
            throw new ForbiddenException("You can only update prescriptions that you issued.");
        ValidateItems(dto);

        rx.Notes = dto.Notes;
        _context.PrescriptionItems.RemoveRange(rx.Items);
        rx.Items = dto.Items.Select(i => new PrescriptionItem
        {
            PrescriptionId = rx.Id,
            MedicationName = i.MedicationName,
            Dosage = i.Dosage,
            Frequency = i.Frequency,
            Duration = i.Duration,
            Instructions = i.Instructions
        }).ToList();

        await _context.SaveChangesAsync();
        return await MapToDtoAsync(rx);
    }

    private async Task EnsureCanReadAsync(Prescription prescription, string userId, string role)
    {
        if (role == "Admin") return;

        var appointment = await _context.Appointments.FindAsync(prescription.AppointmentId)
            ?? throw new NotFoundException("Appointment", prescription.AppointmentId);
        await EnsureCanReadAppointmentAsync(appointment, userId, role);
    }

    private async Task EnsureCanReadAppointmentAsync(Appointment appointment, string userId, string role)
    {
        if (role == "Admin") return;

        var patient = await _context.PatientProfiles.FindAsync(appointment.PatientProfileId);
        if (role == "Patient" && patient?.UserId == userId) return;

        if (role == "Doctor")
        {
            var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor != null && await _context.Appointments.AnyAsync(a =>
                    a.DoctorProfileId == doctor.Id && a.PatientProfileId == appointment.PatientProfileId))
                return;
        }

        throw new ForbiddenException();
    }

    private static void ValidateItems(CreatePrescriptionDto dto)
    {
        if (dto.Items.Count == 0)
            throw new BadRequestException("At least one medication is required.");
        if (dto.Items.Any(item => string.IsNullOrWhiteSpace(item.MedicationName) ||
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
