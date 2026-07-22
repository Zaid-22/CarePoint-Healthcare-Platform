using Microsoft.EntityFrameworkCore;
using CarePoint.Application.DTOs.Appointments;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Enums;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CarePoint.Infrastructure.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;

    public AppointmentService(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        INotificationService notificationService)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    public async Task<AppointmentDto> GetByIdAsync(Guid id, string userId, string role)
    {
        var appointment = await _context.Appointments
            .Include(a => a.PatientProfile)
            .Include(a => a.DoctorProfile)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new NotFoundException("Appointment", id);

        ValidateAccess(appointment, userId, role);
        return await MapToDtoAsync(appointment);
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetAllAsync(string userId, string role)
    {
        IQueryable<Appointment> query = _context.Appointments
            .Include(a => a.PatientProfile)
            .Include(a => a.DoctorProfile);

        if (role == "Patient")
        {
            var patient = await _context.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (patient != null)
                query = query.Where(a => a.PatientProfileId == patient.Id);
        }
        else if (role == "Doctor")
        {
            var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor != null)
                query = query.Where(a => a.DoctorProfileId == doctor.Id);
        }

        var appointments = await query.OrderByDescending(a => a.AppointmentDate).ToListAsync();
        var result = new List<AppointmentDto>();
        foreach (var a in appointments) result.Add(await MapToDtoAsync(a));
        return result;
    }

    public async Task<AppointmentDto> CreateAsync(string userId, CreateAppointmentDto dto)
    {
        var patient = await _context.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null)
        {
            patient = new PatientProfile
            {
                UserId = userId
            };
            _context.PatientProfiles.Add(patient);
            await _context.SaveChangesAsync();
        }

        var doctor = await _context.DoctorProfiles.FindAsync(dto.DoctorProfileId)
            ?? throw new NotFoundException("Doctor profile not found.");

        if (doctor.ApprovalStatus != DoctorApprovalStatus.Approved)
            throw new BadRequestException("This doctor is not yet approved for booking.");

        // Check for double booking
        var conflict = await _context.Appointments.AnyAsync(a =>
            a.DoctorProfileId == dto.DoctorProfileId &&
            a.AppointmentDate.Date == dto.AppointmentDate.Date &&
            a.StartTime < dto.EndTime && a.EndTime > dto.StartTime &&
            a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.Rejected);

        if (conflict)
            throw new ConflictException("This time slot is already booked. Please select a different time slot.");

        var appointment = new Appointment
        {
            PatientProfileId = patient.Id,
            DoctorProfileId = dto.DoctorProfileId,
            AppointmentDate = dto.AppointmentDate,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Notes = dto.Notes,
            Status = AppointmentStatus.Pending
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        await _notificationService.CreateNotificationAsync(
            doctor.UserId, "New Appointment",
            "You have a new appointment request.",
            NotificationType.AppointmentBooked, appointment.Id);

        return await GetByIdAsync(appointment.Id, userId, "Patient");
    }

    public async Task<AppointmentDto> UpdateStatusAsync(Guid id, string userId, string role, UpdateAppointmentStatusDto dto)
    {
        var appointment = await _context.Appointments
            .Include(a => a.PatientProfile)
            .Include(a => a.DoctorProfile)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new NotFoundException("Appointment", id);

        ValidateAccess(appointment, userId, role);
        appointment.Status = dto.Status;
        if (dto.CancellationReason != null) appointment.CancellationReason = dto.CancellationReason;
        await _context.SaveChangesAsync();

        // Notify the other party
        var notifyUserId = role == "Doctor" ? appointment.PatientProfile!.UserId : appointment.DoctorProfile!.UserId;
        var notificationType = dto.Status switch
        {
            AppointmentStatus.Accepted => NotificationType.AppointmentAccepted,
            AppointmentStatus.Rejected => NotificationType.AppointmentRejected,
            AppointmentStatus.Cancelled => NotificationType.AppointmentCancelled,
            _ => NotificationType.AppointmentBooked
        };

        await _notificationService.CreateNotificationAsync(
            notifyUserId, $"Appointment {dto.Status}",
            $"Your appointment status has been updated to {dto.Status}.",
            notificationType, appointment.Id);

        return await MapToDtoAsync(appointment);
    }

    public async Task<AppointmentDto> RescheduleAsync(Guid id, string userId, RescheduleAppointmentDto dto)
    {
        var appointment = await _context.Appointments.FindAsync(id)
            ?? throw new NotFoundException("Appointment", id);

        var patient = await _context.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null || appointment.PatientProfileId != patient.Id)
            throw new ForbiddenException();

        appointment.AppointmentDate = dto.NewAppointmentDate;
        appointment.StartTime = dto.NewStartTime;
        appointment.EndTime = dto.NewEndTime;
        appointment.Status = AppointmentStatus.Pending;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(id, userId, "Patient");
    }

    public async Task<AppointmentDto> CancelAsync(Guid id, string userId, string? reason)
    {
        var appointment = await _context.Appointments.FindAsync(id)
            ?? throw new NotFoundException("Appointment", id);

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancellationReason = reason;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(id, userId, "Admin");
    }

    private void ValidateAccess(Appointment appointment, string userId, string role)
    {
        if (role == "Admin") return;
        if (role == "Patient" && appointment.PatientProfile?.UserId != userId)
            throw new ForbiddenException();
        if (role == "Doctor" && appointment.DoctorProfile?.UserId != userId)
            throw new ForbiddenException();
    }

    private async Task<AppointmentDto> MapToDtoAsync(Appointment a)
    {
        var patientUser = await _userManager.FindByIdAsync(a.PatientProfile?.UserId ?? "");
        var doctorUser = await _userManager.FindByIdAsync(a.DoctorProfile?.UserId ?? "");

        return new AppointmentDto
        {
            Id = a.Id,
            PatientProfileId = a.PatientProfileId,
            PatientName = patientUser != null ? $"{patientUser.FirstName} {patientUser.LastName}" : "",
            DoctorProfileId = a.DoctorProfileId,
            DoctorName = doctorUser != null ? $"{doctorUser.FirstName} {doctorUser.LastName}" : "",
            AppointmentDate = a.AppointmentDate,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            Status = a.Status,
            Notes = a.Notes,
            CancellationReason = a.CancellationReason,
            CreatedAt = a.CreatedAt
        };
    }
}
