using Microsoft.EntityFrameworkCore;
using CarePoint.Application.DTOs.Doctors;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Domain.Enums;

namespace CarePoint.Infrastructure.Services;

public class SpecialtyService : ISpecialtyService
{
    private readonly ApplicationDbContext _context;

    public SpecialtyService(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<SpecialtyDto>> GetAllAsync()
    {
        return await _context.Specialties
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SpecialtyDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                IsActive = s.IsActive,
                DoctorCount = s.DoctorSpecialties.Count(link =>
                    link.DoctorProfile.ApprovalStatus == DoctorApprovalStatus.Approved)
            })
            .ToListAsync();
    }

    public async Task<SpecialtyDto> GetByIdAsync(Guid id)
    {
        var s = await _context.Specialties
            .Include(spec => spec.DoctorSpecialties)
            .FirstOrDefaultAsync(spec => spec.Id == id)
            ?? throw new NotFoundException("Specialty", id);

        return new SpecialtyDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            IsActive = s.IsActive,
            DoctorCount = s.DoctorSpecialties.Count
        };
    }

    public async Task<SpecialtyDto> CreateAsync(CreateSpecialtyDto dto)
    {
        var specialty = new Specialty { Name = dto.Name, Description = dto.Description };
        _context.Specialties.Add(specialty);
        await _context.SaveChangesAsync();
        return new SpecialtyDto
        {
            Id = specialty.Id,
            Name = specialty.Name,
            Description = specialty.Description,
            IsActive = specialty.IsActive
        };
    }

    public async Task<SpecialtyDto> UpdateAsync(Guid id, CreateSpecialtyDto dto)
    {
        var s = await _context.Specialties.FindAsync(id)
            ?? throw new NotFoundException("Specialty", id);
        s.Name = dto.Name;
        s.Description = dto.Description;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var s = await _context.Specialties.FindAsync(id)
            ?? throw new NotFoundException("Specialty", id);
        s.IsActive = false;
        await _context.SaveChangesAsync();
    }

    public async Task<int> SeedSpecialtiesAsync()
    {
        var map = await Data.DatabaseSeeder.SeedSpecialtiesAsync(_context);
        return map.Count;
    }
}
