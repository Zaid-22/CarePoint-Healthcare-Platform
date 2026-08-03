using Microsoft.EntityFrameworkCore;
using CarePoint.Application.DTOs.Appointments;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Enums;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using System.Data;
using CarePoint.Domain.Common;
using CarePoint.Application.DTOs.Common;

namespace CarePoint.Infrastructure.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;
    private readonly IClinicClock _clinicClock;

    public AppointmentService(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        INotificationService notificationService, IClinicClock clinicClock)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
        _clinicClock = clinicClock;
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

    public async Task<PagedResult<AppointmentDto>> GetAllAsync(
        string userId, string role, string? statusGroup = null, DateTime? date = null,
        int skip = 0, int take = 50)
    {
        (skip, take) = Pagination.Normalize(skip, take);
        var query = await GetAccessibleQueryAsync(userId, role);
        query = ApplyListFilters(query, statusGroup, date);

        var totalCount = await query.CountAsync();
        var isUpcoming = string.Equals(statusGroup, "upcoming", StringComparison.OrdinalIgnoreCase);
        var orderedQuery = isUpcoming
            ? query.OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime)
            : query.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime);
        var items = await orderedQuery
            .Skip(skip)
            .Take(take)
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                PatientProfileId = a.PatientProfileId,
                PatientName = _context.Users.Where(u => u.Id == a.PatientProfile.UserId)
                    .Select(u => u.FirstName + " " + u.LastName).FirstOrDefault() ?? string.Empty,
                DoctorProfileId = a.DoctorProfileId,
                DoctorName = _context.Users.Where(u => u.Id == a.DoctorProfile.UserId)
                    .Select(u => u.FirstName + " " + u.LastName).FirstOrDefault() ?? string.Empty,
                AppointmentDate = a.AppointmentDate,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Status = a.Status,
                Notes = a.Notes,
                CancellationReason = a.CancellationReason,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();
        return PagedResult<AppointmentDto>.Create(items, totalCount, skip, take);
    }

    public async Task<AppointmentSummaryDto> GetSummaryAsync(string userId, string role)
    {
        var query = await GetAccessibleQueryAsync(userId, role);
        var localNow = _clinicClock.LocalNow;
        var clinicToday = localNow.Date;
        var clinicTime = TimeOnly.FromDateTime(localNow);
        return new AppointmentSummaryDto
        {
            TotalCount = await query.CountAsync(),
            PendingCount = await query.CountAsync(a =>
                a.Status == AppointmentStatus.Pending &&
                (a.AppointmentDate.Date > clinicToday ||
                 (a.AppointmentDate.Date == clinicToday && a.StartTime > clinicTime))),
            UpcomingCount = await query.CountAsync(a =>
                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Accepted) &&
                (a.AppointmentDate.Date > clinicToday ||
                 (a.AppointmentDate.Date == clinicToday && a.StartTime > clinicTime))),
            TodayCount = await query.CountAsync(a =>
                a.AppointmentDate.Date == clinicToday &&
                (a.Status == AppointmentStatus.Pending ||
                 a.Status == AppointmentStatus.Accepted ||
                 a.Status == AppointmentStatus.InProgress))
        };
    }

    public async Task<AppointmentDto> CreateAsync(string userId, CreateAppointmentDto dto)
    {
        ValidateDateAndTime(dto.AppointmentDate, dto.StartTime, dto.EndTime);

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
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

        await EnsureSlotIsAvailableAsync(dto.DoctorProfileId, dto.AppointmentDate, dto.StartTime, dto.EndTime);

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
        await transaction.CommitAsync();

        return await GetByIdAsync(appointment.Id, userId, "Patient");
    }

    public async Task<AppointmentDto> UpdateStatusAsync(Guid id, string userId, string role, UpdateAppointmentStatusDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var appointment = await _context.Appointments
            .Include(a => a.PatientProfile)
            .Include(a => a.DoctorProfile)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new NotFoundException("Appointment", id);

        if (!Enum.IsDefined(dto.Status))
            throw new BadRequestException("Invalid appointment status.");

        if (role == "Doctor")
        {
            ValidateAccess(appointment, userId, role);
            if (!AppointmentStatusTransitions.CanDoctorTransition(appointment.Status, dto.Status))
                throw new BadRequestException("This appointment status transition is not allowed.");
        }
        else if (role == "Admin")
        {
            if (!AppointmentStatusTransitions.CanAdminTransition(appointment.Status, dto.Status))
                throw new BadRequestException("This appointment status transition is not allowed.");
        }
        else
        {
            throw new ForbiddenException("Only a doctor or administrator can update appointment status.");
        }

        appointment.Status = dto.Status;
        if (dto.CancellationReason != null) appointment.CancellationReason = dto.CancellationReason;
        await _context.SaveChangesAsync();

        var notificationType = dto.Status switch
        {
            AppointmentStatus.Accepted => NotificationType.AppointmentAccepted,
            AppointmentStatus.Rejected => NotificationType.AppointmentRejected,
            AppointmentStatus.Cancelled => NotificationType.AppointmentCancelled,
            _ => NotificationType.SystemAlert
        };

        foreach (var recipient in AppointmentNotificationRecipients.ForActor(
                     role, appointment.PatientProfile!.UserId, appointment.DoctorProfile!.UserId))
        {
            await _notificationService.CreateNotificationAsync(
                recipient, $"Appointment {dto.Status}",
                $"Your appointment status has been updated to {dto.Status}.",
                notificationType, appointment.Id);
        }
        await transaction.CommitAsync();

        return await MapToDtoAsync(appointment);
    }

    public async Task<AppointmentDto> RescheduleAsync(Guid id, string userId, RescheduleAppointmentDto dto)
    {
        ValidateDateAndTime(dto.NewAppointmentDate, dto.NewStartTime, dto.NewEndTime);

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var appointment = await _context.Appointments
            .Include(a => a.DoctorProfile)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new NotFoundException("Appointment", id);

        var patient = await _context.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null || appointment.PatientProfileId != patient.Id)
            throw new ForbiddenException();

        if (!AppointmentStatusTransitions.CanPatientReschedule(appointment.Status))
            throw new BadRequestException("This appointment can no longer be rescheduled.");

        await EnsureSlotIsAvailableAsync(
            appointment.DoctorProfileId,
            dto.NewAppointmentDate,
            dto.NewStartTime,
            dto.NewEndTime,
            appointment.Id);

        appointment.AppointmentDate = dto.NewAppointmentDate;
        appointment.StartTime = dto.NewStartTime;
        appointment.EndTime = dto.NewEndTime;
        appointment.Status = AppointmentStatus.Pending;
        await _context.SaveChangesAsync();
        await _notificationService.CreateNotificationAsync(
            appointment.DoctorProfile.UserId,
            "Appointment Rescheduled",
            "A patient rescheduled an appointment and it requires review.",
            NotificationType.SystemAlert,
            appointment.Id);
        await transaction.CommitAsync();

        return await GetByIdAsync(id, userId, "Patient");
    }

    public async Task<AppointmentDto> CancelAsync(Guid id, string userId, string role, CancelAppointmentDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var appointment = await _context.Appointments
            .Include(a => a.PatientProfile)
            .Include(a => a.DoctorProfile)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new NotFoundException("Appointment", id);

        ValidateAccess(appointment, userId, role);
        if (!AppointmentStatusTransitions.CanCancel(appointment.Status, role))
            throw new BadRequestException("This appointment can no longer be cancelled.");

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancellationReason = dto.CancellationReason;
        await _context.SaveChangesAsync();

        foreach (var recipient in AppointmentNotificationRecipients.ForActor(
                     role, appointment.PatientProfile.UserId, appointment.DoctorProfile.UserId))
        {
            await _notificationService.CreateNotificationAsync(
                recipient,
                "Appointment Cancelled",
                "Your appointment has been cancelled.",
                NotificationType.AppointmentCancelled,
                appointment.Id);
        }
        await transaction.CommitAsync();

        return await MapToDtoAsync(appointment);
    }

    private void ValidateAccess(Appointment appointment, string userId, string role)
    {
        if (role == "Admin") return;
        if (role == "Patient" && appointment.PatientProfile?.UserId != userId)
            throw new ForbiddenException();
        if (role == "Doctor" &&
            (appointment.DoctorProfile?.UserId != userId ||
             appointment.DoctorProfile.ApprovalStatus != DoctorApprovalStatus.Approved))
            throw new ForbiddenException("Only approved doctors can access clinical appointments.");
        if (role is not ("Admin" or "Patient" or "Doctor"))
            throw new ForbiddenException();
    }

    private async Task EnsureSlotIsAvailableAsync(
        Guid doctorProfileId,
        DateTime appointmentDate,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? excludedAppointmentId = null)
    {
        var availabilities = await _context.DoctorAvailabilities
            .Where(availability => availability.DoctorProfileId == doctorProfileId &&
                availability.DayOfWeek == appointmentDate.DayOfWeek)
            .ToListAsync();

        var hasMatchingAvailability = availabilities.Any(availability =>
            startTime >= availability.StartTime &&
            endTime <= availability.EndTime &&
            endTime.AddMinutes(-availability.SlotDurationMinutes) == startTime &&
            (startTime.ToTimeSpan() - availability.StartTime.ToTimeSpan()).Ticks %
                TimeSpan.FromMinutes(availability.SlotDurationMinutes).Ticks == 0);

        if (!hasMatchingAvailability)
            throw new BadRequestException("The requested time is not one of the doctor's available slots.");

        var conflict = await _context.Appointments.AnyAsync(a =>
            a.DoctorProfileId == doctorProfileId &&
            a.AppointmentDate.Date == appointmentDate.Date &&
            (!excludedAppointmentId.HasValue || a.Id != excludedAppointmentId.Value) &&
            a.StartTime < endTime && a.EndTime > startTime &&
            a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.Rejected);

        if (conflict)
            throw new ConflictException("This time slot is already booked. Please select a different time slot.");
    }

    private async Task<IQueryable<Appointment>> GetAccessibleQueryAsync(string userId, string role)
    {
        IQueryable<Appointment> query = _context.Appointments.AsNoTracking();
        if (role == "Patient")
        {
            var patientId = await _context.PatientProfiles.AsNoTracking()
                .Where(profile => profile.UserId == userId)
                .Select(profile => (Guid?)profile.Id)
                .FirstOrDefaultAsync();
            return patientId.HasValue
                ? query.Where(appointment => appointment.PatientProfileId == patientId.Value)
                : query.Where(_ => false);
        }

        if (role == "Doctor")
        {
            var doctorId = await _context.DoctorProfiles.AsNoTracking()
                .Where(profile => profile.UserId == userId &&
                    profile.ApprovalStatus == DoctorApprovalStatus.Approved)
                .Select(profile => (Guid?)profile.Id)
                .FirstOrDefaultAsync();
            return doctorId.HasValue
                ? query.Where(appointment => appointment.DoctorProfileId == doctorId.Value)
                : query.Where(_ => false);
        }

        if (role == "Admin") return query;
        throw new ForbiddenException();
    }

    private IQueryable<Appointment> ApplyListFilters(
        IQueryable<Appointment> query, string? statusGroup, DateTime? date)
    {
        var localNow = _clinicClock.LocalNow;
        var localDate = localNow.Date;
        var localTime = TimeOnly.FromDateTime(localNow);
        query = statusGroup?.ToLowerInvariant() switch
        {
            "upcoming" => query.Where(a =>
                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Accepted) &&
                (a.AppointmentDate.Date > localDate ||
                 (a.AppointmentDate.Date == localDate && a.StartTime > localTime))),
            "active" => query.Where(a => a.Status == AppointmentStatus.Pending ||
                                          a.Status == AppointmentStatus.Accepted ||
                                          a.Status == AppointmentStatus.InProgress),
            "completed" => query.Where(a => a.Status == AppointmentStatus.Completed),
            "cancelled" => query.Where(a => a.Status == AppointmentStatus.Rejected ||
                                            a.Status == AppointmentStatus.Cancelled ||
                                            a.Status == AppointmentStatus.NoShow),
            null or "" or "all" => query,
            _ => throw new BadRequestException("Invalid appointment status filter.")
        };

        return date.HasValue
            ? query.Where(appointment => appointment.AppointmentDate.Date == date.Value.Date)
            : query;
    }

    private void ValidateDateAndTime(DateTime appointmentDate, TimeOnly startTime, TimeOnly endTime)
    {
        if (startTime >= endTime)
            throw new BadRequestException("Start time must be before end time.");

        if (!AppointmentSchedulingRules.IsInFuture(_clinicClock.LocalNow, appointmentDate, startTime))
            throw new BadRequestException("Appointments must be scheduled in the future.");
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
