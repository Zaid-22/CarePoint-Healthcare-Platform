using Microsoft.EntityFrameworkCore;
using CarePoint.Application.DTOs.Medical;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CarePoint.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DocumentService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<MedicalDocumentDto> GetByIdAsync(Guid id, string userId, string role)
    {
        var doc = await _context.MedicalDocuments
            .Include(d => d.PatientProfile)
            .Include(d => d.Appointment).ThenInclude(a => a!.DoctorProfile)
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new NotFoundException("Document", id);
        EnsureCanRead(doc, userId, role);
        return MapToDto(doc);
    }

    public async Task<IReadOnlyList<MedicalDocumentDto>> GetByPatientIdAsync(Guid patientId, string userId, string role)
    {
        var patient = await _context.PatientProfiles.FindAsync(patientId)
            ?? throw new NotFoundException("Patient", patientId);

        IQueryable<MedicalDocument> query = _context.MedicalDocuments
            .AsNoTracking()
            .Where(d => d.PatientProfileId == patientId);

        if (role == "Patient")
        {
            if (patient.UserId != userId) throw new ForbiddenException();
        }
        else if (role == "Doctor")
        {
            var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId)
                ?? throw new ForbiddenException();
            query = query.Where(d => d.AppointmentId != null && d.Appointment!.DoctorProfileId == doctor.Id);
        }
        else if (role != "Admin")
        {
            throw new ForbiddenException();
        }

        var docs = await query
            .OrderByDescending(d => d.CreatedAt)
            .Take(100)
            .ToListAsync();
        return docs.Select(MapToDto).ToList();
    }

    public async Task<MedicalDocumentDto> UploadAsync(Guid patientProfileId, string userId, string fileName,
        string fileUrl, string? documentType, long fileSizeBytes, Guid? appointmentId = null)
    {
        var patient = await _context.PatientProfiles.FindAsync(patientProfileId)
            ?? throw new NotFoundException("Patient", patientProfileId);

        Appointment? appointment = null;
        if (appointmentId.HasValue)
        {
            appointment = await _context.Appointments.FindAsync(appointmentId.Value)
                ?? throw new NotFoundException("Appointment", appointmentId.Value);
            if (appointment.PatientProfileId != patientProfileId)
                throw new BadRequestException("The appointment does not belong to the selected patient.");
        }

        var isPatientOwner = patient.UserId == userId;
        var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
        var isTreatingDoctor = doctor != null && appointment != null && appointment.DoctorProfileId == doctor.Id;
        if (!isPatientOwner && !isTreatingDoctor && !await IsAdminAsync(userId))
            throw new ForbiddenException("You cannot upload documents for this patient.");

        var doc = new MedicalDocument
        {
            PatientProfileId = patientProfileId,
            UploadedByUserId = userId,
            FileName = fileName,
            FileUrl = fileUrl,
            DocumentType = documentType,
            FileSizeBytes = fileSizeBytes,
            AppointmentId = appointmentId
        };
        _context.MedicalDocuments.Add(doc);
        await _context.SaveChangesAsync();
        return MapToDto(doc);
    }

    public async Task DeleteAsync(Guid id, string userId)
    {
        var doc = await _context.MedicalDocuments
            .Include(d => d.PatientProfile)
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new NotFoundException("Document", id);

        if (doc.UploadedByUserId != userId && doc.PatientProfile.UserId != userId && !await IsAdminAsync(userId))
            throw new ForbiddenException("You cannot delete this document.");

        _context.MedicalDocuments.Remove(doc);
        await _context.SaveChangesAsync();
    }

    private static MedicalDocumentDto MapToDto(MedicalDocument d) => new()
    {
        Id = d.Id,
        PatientProfileId = d.PatientProfileId,
        AppointmentId = d.AppointmentId,
        FileName = d.FileName,
        FileUrl = d.FileUrl,
        DocumentType = d.DocumentType,
        FileSizeBytes = d.FileSizeBytes,
        CreatedAt = d.CreatedAt
    };

    private static void EnsureCanRead(MedicalDocument document, string userId, string role)
    {
        if (role == "Admin") return;
        if (role == "Patient" && document.PatientProfile.UserId == userId) return;
        if (role == "Doctor" && document.Appointment?.DoctorProfile.UserId == userId) return;
        throw new ForbiddenException();
    }

    private async Task<bool> IsAdminAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user != null && await _userManager.IsInRoleAsync(user, "Admin");
    }
}
