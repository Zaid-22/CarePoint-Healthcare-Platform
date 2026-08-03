using Microsoft.EntityFrameworkCore;
using CarePoint.Application.DTOs.Doctors;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Enums;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CarePoint.Infrastructure.Services;

public class DoctorService : IDoctorService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;

    public DoctorService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, INotificationService notificationService)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    public async Task<DoctorDto> GetByIdAsync(Guid id)
    {
        var doctor = await _context.DoctorProfiles
            .Include(d => d.DoctorSpecialties).ThenInclude(ds => ds.Specialty)
            .Include(d => d.ClinicDoctors).ThenInclude(cd => cd.Clinic)
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new NotFoundException("Doctor", id);

        var user = await _userManager.FindByIdAsync(doctor.UserId)
            ?? throw new NotFoundException("User", doctor.UserId);

        return MapToDto(doctor, user);
    }

    public async Task<IReadOnlyList<DoctorDto>> GetAllAsync(
        string? specialtyFilter = null, string? nameFilter = null, int skip = 0, int take = 50)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 100);
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

        return result;
    }

    public async Task<IReadOnlyList<DoctorDto>> GetAllForAdminAsync(int skip = 0, int take = 50)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 100);
        var doctors = await _context.DoctorProfiles
            .Include(d => d.DoctorSpecialties).ThenInclude(ds => ds.Specialty)
            .Include(d => d.ClinicDoctors).ThenInclude(cd => cd.Clinic)
            .AsNoTracking()
            .AsSplitQuery()
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
        return result;
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
            ProfilePictureUrl = dto.ProfilePictureUrl,
            ApprovalStatus = DoctorApprovalStatus.Pending
        };

        _context.DoctorProfiles.Add(doctor);

        if (dto.SpecialtyIds != null && dto.SpecialtyIds.Count > 0)
        {
            var validSpecialties = await _context.Specialties
                .Where(s => dto.SpecialtyIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();

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
        return await GetByIdAsync(doctor.Id);
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
        doctor.ProfilePictureUrl = dto.ProfilePictureUrl;

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
        return await GetByIdAsync(doctor.Id);
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

        return await GetByIdAsync(id);
    }

    public async Task<DoctorDto> RejectAsync(Guid id)
    {
        var doctor = await _context.DoctorProfiles.FindAsync(id)
            ?? throw new NotFoundException("Doctor", id);
        doctor.ApprovalStatus = DoctorApprovalStatus.Rejected;
        await _context.SaveChangesAsync();

        await _notificationService.CreateNotificationAsync(
            doctor.UserId,
            "Application Status Update",
            "Your practitioner registration requires additional credential verification.",
            NotificationType.SystemAlert,
            id
        );

        return await GetByIdAsync(id);
    }

    public async Task<IReadOnlyList<DoctorAvailabilityDto>> GetAvailabilityAsync(Guid doctorId)
    {
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

        slot.DayOfWeek = dto.DayOfWeek;
        slot.StartTime = dto.StartTime;
        slot.EndTime = dto.EndTime;
        slot.SlotDurationMinutes = dto.SlotDurationMinutes;
        await _context.SaveChangesAsync();

        return new DoctorAvailabilityDto
        {
            Id = slot.Id, DayOfWeek = slot.DayOfWeek,
            StartTime = slot.StartTime, EndTime = slot.EndTime,
            SlotDurationMinutes = slot.SlotDurationMinutes
        };
    }

    public async Task DeleteAvailabilityAsync(Guid doctorId, Guid slotId, string userId)
    {
        var doctor = await _context.DoctorProfiles.FindAsync(doctorId)
            ?? throw new NotFoundException("Doctor", doctorId);
        if (doctor.UserId != userId)
            throw new ForbiddenException();

        var slot = await _context.DoctorAvailabilities.FindAsync(slotId)
            ?? throw new NotFoundException("Availability", slotId);
        if (slot.DoctorProfileId != doctorId)
            throw new ForbiddenException();

        _context.DoctorAvailabilities.Remove(slot);
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AvailableSlotDto>> GetAvailableSlotsAsync(Guid doctorId, DateTime date)
    {
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

                slots.Add(new AvailableSlotDto
                {
                    Date = date.Date,
                    StartTime = current,
                    EndTime = slotEnd,
                    IsAvailable = !isBooked
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
}
