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
        return await MapToDtoAsync(rx);
    }

    public async Task<IReadOnlyList<PrescriptionDto>> GetByAppointmentIdAsync(Guid appointmentId, string userId, string role)
    {
        var prescriptions = await _context.Prescriptions
            .Include(p => p.Items)
            .Where(p => p.AppointmentId == appointmentId)
            .ToListAsync();

        var list = new List<PrescriptionDto>();
        foreach (var p in prescriptions)
        {
            list.Add(await MapToDtoAsync(p));
        }
        return list;
    }

    public async Task<IReadOnlyList<PrescriptionDto>> GetMyPrescriptionsAsync(string userId)
    {
        var patient = await _context.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null) return new List<PrescriptionDto>();

        var prescriptions = await _context.Prescriptions
            .Include(p => p.Items)
            .Where(p => p.PatientProfileId == patient.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var list = new List<PrescriptionDto>();
        foreach (var p in prescriptions)
        {
            list.Add(await MapToDtoAsync(p));
        }
        return list;
    }

    public async Task<PrescriptionDto> CreateAsync(string userId, CreatePrescriptionDto dto)
    {
        var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId)
            ?? throw new ForbiddenException("Only doctors can create prescriptions.");

        var appointment = await _context.Appointments.FindAsync(dto.AppointmentId)
            ?? throw new NotFoundException("Appointment", dto.AppointmentId);

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
}
