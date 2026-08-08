using Microsoft.EntityFrameworkCore;
using CarePoint.Application.DTOs.Doctors;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Enums;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using CarePoint.Application.DTOs.Common;
using CarePoint.Domain.Common;
using System.Data;
using Microsoft.Extensions.Logging;

namespace CarePoint.Infrastructure.Services;

public class DoctorService : IDoctorService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;
    private readonly IClinicClock _clinicClock;
    private readonly IProfileImageStorage _profileImageStorage;
    private readonly ILogger<DoctorService> _logger;

    public DoctorService(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        INotificationService notificationService, IClinicClock clinicClock,
        IProfileImageStorage profileImageStorage, ILogger<DoctorService> logger)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
        _clinicClock = clinicClock;
        _profileImageStorage = profileImageStorage;
        _logger = logger;
    }

    public async Task<PublicDoctorDto> GetByIdAsync(Guid id)
    {
        var doctor = await _context.DoctorProfiles
            .Include(d => d.DoctorSpecialties).ThenInclude(ds => ds.Specialty)
            .Include(d => d.ClinicDoctors).ThenInclude(cd => cd.Clinic)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id &&
                d.ApprovalStatus == DoctorApprovalStatus.Approved)
            ?? throw new NotFoundException("Doctor", id);
        var user = await _userManager.FindByIdAsync(doctor.UserId)
            ?? throw new NotFoundException("User", doctor.UserId);
        return MapToPublicDto(doctor, user);
    }

    private async Task<DoctorDto> GetByIdInternalAsync(Guid id, bool requireApproved)
    {
        var doctor = await _context.DoctorProfiles
            .Include(d => d.DoctorSpecialties).ThenInclude(ds => ds.Specialty)
            .Include(d => d.ClinicDoctors).ThenInclude(cd => cd.Clinic)
            .FirstOrDefaultAsync(d => d.Id == id &&
                (!requireApproved || d.ApprovalStatus == DoctorApprovalStatus.Approved))
            ?? throw new NotFoundException("Doctor", id);

        var user = await _userManager.FindByIdAsync(doctor.UserId)
            ?? throw new NotFoundException("User", doctor.UserId);

        return MapToDto(doctor, user);
    }

    public async Task<PagedResult<PublicDoctorDto>> GetAllAsync(
        string? specialtyFilter = null, string? nameFilter = null, int skip = 0, int take = 50)
    {
        (skip, take) = Pagination.Normalize(skip, take);
        var query = _context.DoctorProfiles
            .Include(d => d.DoctorSpecialties).ThenInclude(ds => ds.Specialty)
            .Include(d => d.ClinicDoctors).ThenInclude(cd => cd.Clinic)
            .Where(d => d.ApprovalStatus == DoctorApprovalStatus.Approved)
            .AsNoTracking()
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(specialtyFilter))
        {
            query = query.Where(d => d.DoctorSpecialties
                .Any(ds => ds.Specialty.Name.Contains(specialtyFilter)));
        }

        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            query = query.Where(d => _context.Users.Any(user =>
                user.Id == d.UserId && (user.FirstName + " " + user.LastName).Contains(nameFilter)));
        }

        var totalCount = await query.CountAsync();
        var doctors = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        var users = await LoadUsersAsync(doctors.Select(d => d.UserId));
        var result = new List<PublicDoctorDto>();

        foreach (var doctor in doctors)
        {
            if (users.TryGetValue(doctor.UserId, out var user))
                result.Add(MapToPublicDto(doctor, user));
        }

        return PagedResult<PublicDoctorDto>.Create(result, totalCount, skip, take);
    }

    public async Task<PagedResult<DoctorDto>> GetAllForAdminAsync(
        DoctorApprovalStatus? status = null, int skip = 0, int take = 50)
    {
        (skip, take) = Pagination.Normalize(skip, take);
        var query = _context.DoctorProfiles
            .Include(d => d.DoctorSpecialties).ThenInclude(ds => ds.Specialty)
            .Include(d => d.ClinicDoctors).ThenInclude(cd => cd.Clinic)
            .AsNoTracking()
            .AsSplitQuery();
        if (status.HasValue)
            query = query.Where(doctor => doctor.ApprovalStatus == status.Value);

        var totalCount = await query.CountAsync();
        var doctors = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        var users = await LoadUsersAsync(doctors.Select(d => d.UserId));
        var result = new List<DoctorDto>();
        foreach (var doctor in doctors)
        {
            if (users.TryGetValue(doctor.UserId, out var user)) result.Add(MapToDto(doctor, user));
        }
        return PagedResult<DoctorDto>.Create(result, totalCount, skip, take);
    }

    public async Task<DoctorAdminSummaryDto> GetAdminSummaryAsync()
    {
        var counts = await _context.DoctorProfiles
            .AsNoTracking()
            .GroupBy(doctor => doctor.ApprovalStatus)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count);

        return new DoctorAdminSummaryDto
        {
            TotalRegistered = counts.Values.Sum(),
            PendingCount = counts.GetValueOrDefault(DoctorApprovalStatus.Pending),
            ApprovedCount = counts.GetValueOrDefault(DoctorApprovalStatus.Approved),
            RejectedCount = counts.GetValueOrDefault(DoctorApprovalStatus.Rejected)
        };
    }

    public async Task<DoctorDto> GetProfileByUserIdAsync(string userId)
    {
        var doctor = await _context.DoctorProfiles
            .Include(d => d.DoctorSpecialties).ThenInclude(ds => ds.Specialty)
            .Include(d => d.ClinicDoctors).ThenInclude(cd => cd.Clinic)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
        {
            doctor = new DoctorProfile
            {
                UserId = userId,
                ApprovalStatus = DoctorApprovalStatus.Pending
            };
            _context.DoctorProfiles.Add(doctor);
            await _context.SaveChangesAsync();
        }

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        return MapToDto(doctor, user);
    }

    public async Task<DoctorDto> CreateProfileAsync(string userId, CreateDoctorDto dto)
    {
        var existing = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
        if (existing != null)
            throw new ConflictException("Doctor profile already exists.");

        var doctor = new DoctorProfile
        {
            UserId = userId,
            Bio = dto.Bio,
            ConsultationFee = dto.ConsultationFee,
            PhoneNumber = dto.PhoneNumber,
            Gender = dto.Gender,
            ProfilePictureUrl = NormalizeExternalProfilePictureUrl(dto.ProfilePictureUrl),
            ApprovalStatus = DoctorApprovalStatus.Pending
        };

        _context.DoctorProfiles.Add(doctor);

        if (dto.SpecialtyIds != null && dto.SpecialtyIds.Count > 0)
        {
            var validSpecialties = await _context.Specialties
                .Where(s => s.IsActive && dto.SpecialtyIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();

            if (validSpecialties.Count != dto.SpecialtyIds.Distinct().Count())
                throw new BadRequestException("One or more selected specialties are invalid or inactive.");

            foreach (var specialtyId in validSpecialties)
            {
                _context.DoctorSpecialties.Add(new DoctorSpecialty
                {
                    DoctorProfileId = doctor.Id,
                    SpecialtyId = specialtyId
                });
            }
        }

        await _context.SaveChangesAsync();
        return await GetByIdInternalAsync(doctor.Id, requireApproved: false);
    }

    public async Task<DoctorDto> UpdateProfileAsync(Guid id, string userId, UpdateDoctorDto dto)
    {
        var doctor = await _context.DoctorProfiles
            .Include(d => d.DoctorSpecialties)
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new NotFoundException("Doctor", id);

        if (doctor.UserId != userId)
            throw new ForbiddenException("You can only update your own profile.");

        doctor.Bio = dto.Bio;
        doctor.ConsultationFee = dto.ConsultationFee;
        doctor.PhoneNumber = dto.PhoneNumber;
        doctor.Gender = dto.Gender;
        var oldStorageKey = doctor.ProfilePictureStorageKey;
        var profilePicture = ResolveProfilePictureUpdate(doctor, dto.ProfilePictureUrl);
        doctor.ProfilePictureUrl = profilePicture.Url;
        doctor.ProfilePictureStorageKey = profilePicture.KeepStoredImage
            ? doctor.ProfilePictureStorageKey
            : null;

        var requestedSpecialtyIds = dto.SpecialtyIds.Distinct().ToList();
        var validSpecialties = await _context.Specialties
            .Where(s => s.IsActive && requestedSpecialtyIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        if (validSpecialties.Count != requestedSpecialtyIds.Count)
            throw new BadRequestException("One or more selected specialties are invalid or inactive.");

        _context.DoctorSpecialties.RemoveRange(doctor.DoctorSpecialties);
        foreach (var specialtyId in validSpecialties)
        {
            _context.DoctorSpecialties.Add(new DoctorSpecialty
            {
                DoctorProfileId = doctor.Id,
                SpecialtyId = specialtyId
            });
        }

        await _context.SaveChangesAsync();
        if (oldStorageKey != null && doctor.ProfilePictureStorageKey == null)
            await DeleteReplacedProfileImageAsync(oldStorageKey);
        return await GetByIdInternalAsync(doctor.Id, requireApproved: false);
    }

    public async Task<DoctorDto> UpdateProfileByUserIdAsync(string userId, UpdateDoctorDto dto)
    {
        var doctor = await _context.DoctorProfiles
            .Include(d => d.DoctorSpecialties)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
        {
            return await CreateProfileAsync(userId, new CreateDoctorDto
            {
                Bio = dto.Bio,
                ConsultationFee = dto.ConsultationFee,
                PhoneNumber = dto.PhoneNumber,
                Gender = dto.Gender,
                ProfilePictureUrl = dto.ProfilePictureUrl,
                SpecialtyIds = dto.SpecialtyIds
            });
        }

        return await UpdateProfileAsync(doctor.Id, userId, dto);
    }

    public async Task<DoctorDto> UploadProfileImageAsync(
        string userId, Stream content, string fileExtension)
    {
        var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId)
            ?? throw new NotFoundException("Doctor profile not found.");

        string? newStorageKey = null;
        var persisted = false;
        try
        {
            newStorageKey = await _profileImageStorage.SaveAsync(content, fileExtension);
            var oldStorageKey = doctor.ProfilePictureStorageKey;
            doctor.ProfilePictureStorageKey = newStorageKey;
            doctor.ProfilePictureUrl =
                $"/api/doctors/{doctor.Id}/avatar?v={Guid.NewGuid():N}";
            await _context.SaveChangesAsync();
            persisted = true;

            if (oldStorageKey != null)
                await DeleteReplacedProfileImageAsync(oldStorageKey);
        }
        catch
        {
            if (newStorageKey != null && !persisted)
                await _profileImageStorage.DeleteAsync(newStorageKey);
            throw;
        }

        return await GetByIdInternalAsync(doctor.Id, requireApproved: false);
    }

    public async Task<ProfileImageContent> GetProfileImageAsync(Guid doctorId)
    {
        var storageKey = await _context.DoctorProfiles.AsNoTracking()
            .Where(doctor => doctor.Id == doctorId)
            .Select(doctor => doctor.ProfilePictureStorageKey)
            .FirstOrDefaultAsync();
        if (storageKey == null)
            throw new NotFoundException("Profile image", doctorId);

        Stream content;
        try
        {
            content = await _profileImageStorage.OpenReadAsync(storageKey);
        }
        catch (FileNotFoundException)
        {
            throw new NotFoundException("Profile image", doctorId);
        }

        var contentType = Path.GetExtension(storageKey).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
        return new ProfileImageContent(content, contentType);
    }

    public async Task DeleteAsync(Guid id)
    {
        var doctor = await _context.DoctorProfiles.FindAsync(id)
            ?? throw new NotFoundException("Doctor", id);
        doctor.IsDeleted = true;
        doctor.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<DoctorDto> ApproveAsync(Guid id)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var doctor = await _context.DoctorProfiles.FindAsync(id)
            ?? throw new NotFoundException("Doctor", id);
        doctor.ApprovalStatus = DoctorApprovalStatus.Approved;
        await _context.SaveChangesAsync();

        await _notificationService.CreateNotificationAsync(
            doctor.UserId,
            "Application Verified & Approved",
            "Your practitioner credentials have been verified and approved by System Administration.",
            NotificationType.DoctorApproved,
            id
        );
        await transaction.CommitAsync();

        return await GetByIdInternalAsync(id, requireApproved: false);
    }

    public async Task<DoctorDto> RejectAsync(Guid id)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var doctor = await _context.DoctorProfiles.FindAsync(id)
            ?? throw new NotFoundException("Doctor", id);
        doctor.ApprovalStatus = DoctorApprovalStatus.Rejected;
        var revokedAt = DateTime.UtcNow;
        await _context.RefreshTokens
            .Where(token => token.UserId == doctor.UserId && !token.IsRevoked)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.IsRevoked, true)
                .SetProperty(token => token.RevokedAt, revokedAt));
        await _context.SaveChangesAsync();

        await _notificationService.CreateNotificationAsync(
            doctor.UserId,
            "Application Status Update",
            "Your practitioner registration requires additional credential verification.",
            NotificationType.SystemAlert,
            id
        );
        await transaction.CommitAsync();

        return await GetByIdInternalAsync(id, requireApproved: false);
    }

    public async Task<IReadOnlyList<DoctorAvailabilityDto>> GetAvailabilityAsync(
        Guid doctorId, string? requesterUserId = null, string? requesterRole = null)
    {
        var doctor = await _context.DoctorProfiles.AsNoTracking()
            .Where(d => d.Id == doctorId)
            .Select(d => new { d.UserId, d.ApprovalStatus })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Doctor", doctorId);

        if (!DoctorDirectoryAccessRules.CanViewAvailability(
                doctor.ApprovalStatus, doctor.UserId, requesterUserId, requesterRole))
            throw new NotFoundException("Doctor", doctorId);

        var slots = await _context.DoctorAvailabilities
            .Where(da => da.DoctorProfileId == doctorId)
            .OrderBy(da => da.DayOfWeek).ThenBy(da => da.StartTime)
            .ToListAsync();

        return slots.Select(s => new DoctorAvailabilityDto
        {
            Id = s.Id,
            DayOfWeek = s.DayOfWeek,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            SlotDurationMinutes = s.SlotDurationMinutes
        }).ToList();
    }

    public async Task<DoctorAvailabilityDto> AddAvailabilityAsync(Guid doctorId, string userId, CreateAvailabilityDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var doctor = await _context.DoctorProfiles.FindAsync(doctorId)
            ?? throw new NotFoundException("Doctor", doctorId);
        if (doctor.UserId != userId)
            throw new ForbiddenException();

        ValidateAvailability(dto);
        await EnsureAvailabilityDoesNotOverlapAsync(doctorId, dto);

        var availability = new DoctorAvailability
        {
            DoctorProfileId = doctorId,
            DayOfWeek = dto.DayOfWeek,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            SlotDurationMinutes = dto.SlotDurationMinutes
        };

        _context.DoctorAvailabilities.Add(availability);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new DoctorAvailabilityDto
        {
            Id = availability.Id,
            DayOfWeek = availability.DayOfWeek,
            StartTime = availability.StartTime,
            EndTime = availability.EndTime,
            SlotDurationMinutes = availability.SlotDurationMinutes
        };
    }

    public async Task<DoctorAvailabilityDto> UpdateAvailabilityAsync(Guid doctorId, Guid slotId, string userId, CreateAvailabilityDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var doctor = await _context.DoctorProfiles.FindAsync(doctorId)
            ?? throw new NotFoundException("Doctor", doctorId);
        if (doctor.UserId != userId)
            throw new ForbiddenException();

        var slot = await _context.DoctorAvailabilities.FindAsync(slotId)
            ?? throw new NotFoundException("Availability", slotId);
        if (slot.DoctorProfileId != doctorId)
            throw new ForbiddenException();

        ValidateAvailability(dto);
        await EnsureAvailabilityDoesNotOverlapAsync(doctorId, dto, slotId);
        await EnsureBookedAppointmentsRemainCoveredAsync(doctorId, slotId, dto);

        slot.DayOfWeek = dto.DayOfWeek;
        slot.StartTime = dto.StartTime;
        slot.EndTime = dto.EndTime;
        slot.SlotDurationMinutes = dto.SlotDurationMinutes;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new DoctorAvailabilityDto
        {
            Id = slot.Id, DayOfWeek = slot.DayOfWeek,
            StartTime = slot.StartTime, EndTime = slot.EndTime,
            SlotDurationMinutes = slot.SlotDurationMinutes
        };
    }

    public async Task DeleteAvailabilityAsync(Guid doctorId, Guid slotId, string userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var doctor = await _context.DoctorProfiles.FindAsync(doctorId)
            ?? throw new NotFoundException("Doctor", doctorId);
        if (doctor.UserId != userId)
            throw new ForbiddenException();

        var slot = await _context.DoctorAvailabilities.FindAsync(slotId)
            ?? throw new NotFoundException("Availability", slotId);
        if (slot.DoctorProfileId != doctorId)
            throw new ForbiddenException();

        await EnsureBookedAppointmentsRemainCoveredAsync(doctorId, slotId, replacement: null);
        _context.DoctorAvailabilities.Remove(slot);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<IReadOnlyList<AvailableSlotDto>> GetAvailableSlotsAsync(Guid doctorId, DateTime date)
    {
        var isApproved = await _context.DoctorProfiles.AsNoTracking()
            .AnyAsync(d => d.Id == doctorId && d.ApprovalStatus == DoctorApprovalStatus.Approved);
        if (!isApproved)
            throw new NotFoundException("Doctor", doctorId);

        var dayOfWeek = date.DayOfWeek;
        var schedules = await _context.DoctorAvailabilities
            .Where(da => da.DoctorProfileId == doctorId && da.DayOfWeek == dayOfWeek)
            .ToListAsync();

        var bookedSlots = await _context.Appointments
            .Where(a => a.DoctorProfileId == doctorId
                && a.AppointmentDate.Date == date.Date
                && a.Status != AppointmentStatus.Cancelled
                && a.Status != AppointmentStatus.Rejected)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync();

        var slots = new List<AvailableSlotDto>();
        foreach (var schedule in schedules)
        {
            var current = schedule.StartTime;
            while (current.AddMinutes(schedule.SlotDurationMinutes) <= schedule.EndTime)
            {
                var slotEnd = current.AddMinutes(schedule.SlotDurationMinutes);
                var isBooked = bookedSlots.Any(b => b.StartTime < slotEnd && b.EndTime > current);
                var isElapsed = AppointmentSchedulingRules.IsElapsed(_clinicClock.LocalNow, date, current);

                slots.Add(new AvailableSlotDto
                {
                    Date = date.Date,
                    StartTime = current,
                    EndTime = slotEnd,
                    IsAvailable = !isBooked && !isElapsed
                });

                current = slotEnd;
            }
        }

        return slots;
    }

    private static void ValidateAvailability(CreateAvailabilityDto dto)
    {
        if (dto.StartTime >= dto.EndTime)
            throw new BadRequestException("Start time must be before end time.");
        if (dto.SlotDurationMinutes is < 10 or > 120)
            throw new BadRequestException("Slot duration must be between 10 and 120 minutes.");
    }

    private async Task EnsureAvailabilityDoesNotOverlapAsync(
        Guid doctorId, CreateAvailabilityDto dto, Guid? excludedSlotId = null)
    {
        var overlaps = await _context.DoctorAvailabilities.AnyAsync(slot =>
            slot.DoctorProfileId == doctorId &&
            slot.DayOfWeek == dto.DayOfWeek &&
            (!excludedSlotId.HasValue || slot.Id != excludedSlotId.Value) &&
            slot.StartTime < dto.EndTime && slot.EndTime > dto.StartTime);

        if (overlaps)
            throw new ConflictException("Availability periods on the same day cannot overlap.");
    }

    private async Task EnsureBookedAppointmentsRemainCoveredAsync(
        Guid doctorId, Guid changedSlotId, CreateAvailabilityDto? replacement)
    {
        var localNow = _clinicClock.LocalNow;
        var localDate = localNow.Date;
        var localTime = TimeOnly.FromDateTime(localNow);

        var appointments = await _context.Appointments.AsNoTracking()
            .Where(a => a.DoctorProfileId == doctorId &&
                (a.Status == AppointmentStatus.Pending ||
                 a.Status == AppointmentStatus.Accepted ||
                 a.Status == AppointmentStatus.InProgress) &&
                (a.AppointmentDate.Date > localDate ||
                 (a.AppointmentDate.Date == localDate && a.StartTime > localTime)))
            .Select(a => new { a.AppointmentDate, a.StartTime, a.EndTime })
            .ToListAsync();

        if (appointments.Count == 0) return;

        var schedules = await _context.DoctorAvailabilities.AsNoTracking()
            .Where(slot => slot.DoctorProfileId == doctorId && slot.Id != changedSlotId)
            .Select(slot => new
            {
                slot.DayOfWeek, slot.StartTime, slot.EndTime, slot.SlotDurationMinutes
            })
            .ToListAsync();

        var remainingAvailability = schedules
            .Select(schedule => new AvailabilityWindow(
                schedule.DayOfWeek, schedule.StartTime, schedule.EndTime,
                schedule.SlotDurationMinutes))
            .ToList();
        if (replacement != null)
        {
            remainingAvailability.Add(new AvailabilityWindow(
                replacement.DayOfWeek, replacement.StartTime, replacement.EndTime,
                replacement.SlotDurationMinutes));
        }
        var invalidatesBooking = AvailabilityCoverageRules.WouldInvalidateBooking(
            appointments.Select(appointment => new AppointmentWindow(
                appointment.AppointmentDate.DayOfWeek, appointment.StartTime, appointment.EndTime)),
            remainingAvailability);

        if (invalidatesBooking)
            throw new ConflictException(
                "This availability change would invalidate a future appointment. Reschedule or cancel that appointment first.");
    }

    private static string? NormalizeExternalProfilePictureUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (!ProfilePictureRules.IsPermittedExternalUrl(trimmed))
            throw new BadRequestException("Profile picture URL must be an HTTPS URL.");
        return trimmed;
    }

    private static (string? Url, bool KeepStoredImage) ResolveProfilePictureUpdate(
        DoctorProfile doctor, string? requestedUrl)
    {
        if (string.IsNullOrWhiteSpace(requestedUrl)) return (null, false);
        var trimmed = requestedUrl.Trim();
        var ownAvatarPath = $"/api/doctors/{doctor.Id}/avatar";
        if (doctor.ProfilePictureStorageKey != null &&
            (trimmed == ownAvatarPath ||
             trimmed.StartsWith(ownAvatarPath + "?", StringComparison.Ordinal)))
        {
            return (doctor.ProfilePictureUrl, true);
        }

        if (!ProfilePictureRules.IsPermittedExternalUrl(trimmed))
            throw new BadRequestException(
                "Profile picture must be an HTTPS URL or your uploaded CarePoint image.");
        return (trimmed, false);
    }

    private async Task DeleteReplacedProfileImageAsync(string storageKey)
    {
        try
        {
            await _profileImageStorage.DeleteAsync(storageKey);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception,
                "Could not delete replaced profile image {StorageKey}", storageKey);
        }
    }

    private async Task<Dictionary<string, ApplicationUser>> LoadUsersAsync(IEnumerable<string> userIds)
    {
        var ids = userIds.Distinct().ToList();
        return await _context.Users.AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id);
    }

    private static DoctorDto MapToDto(DoctorProfile doctor, ApplicationUser user) => new()
    {
        Id = doctor.Id,
        UserId = doctor.UserId,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email!,
        Bio = doctor.Bio,
        ConsultationFee = doctor.ConsultationFee,
        PhoneNumber = doctor.PhoneNumber,
        Gender = doctor.Gender,
        ProfilePictureUrl = doctor.ProfilePictureUrl,
        ApprovalStatus = doctor.ApprovalStatus,
        Specialties = doctor.DoctorSpecialties.Select(ds => new SpecialtyDto
        {
            Id = ds.Specialty.Id,
            Name = ds.Specialty.Name,
            Description = ds.Specialty.Description,
            IsActive = ds.Specialty.IsActive
        }).ToList(),
        Clinics = doctor.ClinicDoctors.Select(cd => new ClinicDto
        {
            Id = cd.Clinic.Id,
            Name = cd.Clinic.Name,
            Address = cd.Clinic.Address,
            PhoneNumber = cd.Clinic.PhoneNumber,
            City = cd.Clinic.City,
            IsActive = cd.Clinic.IsActive
        }).ToList()
    };

    private static PublicDoctorDto MapToPublicDto(DoctorProfile doctor, ApplicationUser user) => new()
    {
        Id = doctor.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Bio = doctor.Bio,
        ConsultationFee = doctor.ConsultationFee,
        ProfilePictureUrl = doctor.ProfilePictureUrl,
        Specialties = doctor.DoctorSpecialties.Select(ds => new SpecialtyDto
        {
            Id = ds.Specialty.Id,
            Name = ds.Specialty.Name,
            Description = ds.Specialty.Description,
            IsActive = ds.Specialty.IsActive
        }).ToList(),
        Clinics = doctor.ClinicDoctors.Select(cd => new ClinicDto
        {
            Id = cd.Clinic.Id,
            Name = cd.Clinic.Name,
            Address = cd.Clinic.Address,
            PhoneNumber = cd.Clinic.PhoneNumber,
            City = cd.Clinic.City,
            IsActive = cd.Clinic.IsActive
        }).ToList()
    };
}
