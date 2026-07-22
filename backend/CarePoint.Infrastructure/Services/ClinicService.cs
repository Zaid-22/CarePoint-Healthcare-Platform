using Microsoft.EntityFrameworkCore;
using CarePoint.Application.DTOs.Doctors;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;

namespace CarePoint.Infrastructure.Services;

public class ClinicService : IClinicService
{
    private readonly ApplicationDbContext _context;

    public ClinicService(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<ClinicDto>> GetAllAsync()
    {
        var clinics = await _context.Clinics.Where(c => c.IsActive).ToListAsync();
        return clinics.Select(c => new ClinicDto
        {
            Id = c.Id,
            Name = c.Name,
            Address = c.Address,
            PhoneNumber = c.PhoneNumber,
            City = c.City,
            IsActive = c.IsActive
        }).ToList();
    }

    public async Task<ClinicDto> GetByIdAsync(Guid id)
    {
        var c = await _context.Clinics.FindAsync(id)
            ?? throw new NotFoundException("Clinic", id);
        return new ClinicDto
        {
            Id = c.Id,
            Name = c.Name,
            Address = c.Address,
            PhoneNumber = c.PhoneNumber,
            City = c.City,
            IsActive = c.IsActive
        };
    }

    public async Task<ClinicDto> CreateAsync(CreateClinicDto dto)
    {
        var clinic = new Clinic
        {
            Name = dto.Name,
            Address = dto.Address,
            PhoneNumber = dto.PhoneNumber,
            City = dto.City
        };
        _context.Clinics.Add(clinic);
        await _context.SaveChangesAsync();
        return new ClinicDto
        {
            Id = clinic.Id,
            Name = clinic.Name,
            Address = clinic.Address,
            PhoneNumber = clinic.PhoneNumber,
            City = clinic.City,
            IsActive = clinic.IsActive
        };
    }

    public async Task<ClinicDto> UpdateAsync(Guid id, CreateClinicDto dto)
    {
        var clinic = await _context.Clinics.FindAsync(id)
            ?? throw new NotFoundException("Clinic", id);
        clinic.Name = dto.Name;
        clinic.Address = dto.Address;
        clinic.PhoneNumber = dto.PhoneNumber;
        clinic.City = dto.City;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var clinic = await _context.Clinics.FindAsync(id)
            ?? throw new NotFoundException("Clinic", id);
        clinic.IsActive = false;
        await _context.SaveChangesAsync();
    }
}
