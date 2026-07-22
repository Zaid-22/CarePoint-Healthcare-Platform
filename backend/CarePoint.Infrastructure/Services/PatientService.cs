using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CarePoint.Application.DTOs.Patients;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;

namespace CarePoint.Infrastructure.Services;

public class PatientService : IPatientService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PatientService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<PatientDto> GetByIdAsync(Guid id, string userId, string role)
    {
        var patient = await _context.PatientProfiles.FindAsync(id)
            ?? throw new NotFoundException("Patient", id);

        if (role != "Admin" && patient.UserId != userId)
        {
            var doctor = role == "Doctor"
                ? await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId)
                : null;
            if (doctor == null || !await _context.Appointments.AnyAsync(a =>
                    a.DoctorProfileId == doctor.Id && a.PatientProfileId == patient.Id))
                throw new ForbiddenException();
        }

        var user = await _userManager.FindByIdAsync(patient.UserId)
            ?? throw new NotFoundException("User", patient.UserId);
        return MapToDto(patient, user);
    }

    public async Task<PatientDto> GetByUserIdAsync(string userId)
    {
        var patient = await _context.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null)
        {
            patient = new PatientProfile { UserId = userId };
            _context.PatientProfiles.Add(patient);
            await _context.SaveChangesAsync();
        }

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);
        return MapToDto(patient, user);
    }

    public async Task<IReadOnlyList<PatientDto>> GetAllAsync()
    {
        var patients = await _context.PatientProfiles.ToListAsync();
        var result = new List<PatientDto>();
        foreach (var p in patients)
        {
            var user = await _userManager.FindByIdAsync(p.UserId);
            if (user != null) result.Add(MapToDto(p, user));
        }
        return result;
    }

    public async Task<PatientDto> UpdateProfileAsync(Guid id, string userId, UpdatePatientDto dto)
    {
        var patient = await _context.PatientProfiles.FindAsync(id)
            ?? throw new NotFoundException("Patient", id);
        if (patient.UserId != userId)
            throw new ForbiddenException("You can only update your own profile.");

        patient.PhoneNumber = dto.PhoneNumber;
        patient.DateOfBirth = dto.DateOfBirth;
        patient.Gender = dto.Gender;
        patient.BloodType = dto.BloodType;
        patient.Address = dto.Address;
        patient.EmergencyContact = dto.EmergencyContact;

        await _context.SaveChangesAsync();
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);
        return MapToDto(patient, user);
    }

    public async Task<PatientDto> UpdateMyProfileAsync(string userId, UpdatePatientDto dto)
    {
        var patient = await _context.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null)
        {
            patient = new PatientProfile { UserId = userId };
            _context.PatientProfiles.Add(patient);
        }

        patient.PhoneNumber = dto.PhoneNumber;
        patient.DateOfBirth = dto.DateOfBirth;
        patient.Gender = dto.Gender;
        patient.BloodType = dto.BloodType;
        patient.Address = dto.Address;
        patient.EmergencyContact = dto.EmergencyContact;

        await _context.SaveChangesAsync();
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);
        return MapToDto(patient, user);
    }

    private static PatientDto MapToDto(PatientProfile p, ApplicationUser u) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        FirstName = u.FirstName,
        LastName = u.LastName,
        Email = u.Email!,
        PhoneNumber = p.PhoneNumber,
        DateOfBirth = p.DateOfBirth,
        Gender = p.Gender,
        BloodType = p.BloodType,
        Address = p.Address,
        EmergencyContact = p.EmergencyContact
    };
}
