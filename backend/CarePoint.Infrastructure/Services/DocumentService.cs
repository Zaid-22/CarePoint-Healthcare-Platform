using Microsoft.EntityFrameworkCore;
using CarePoint.Application.DTOs.Medical;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;
using CarePoint.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using CarePoint.Application.DTOs.Common;
using CarePoint.Domain.Common;
using CarePoint.Domain.Enums;

namespace CarePoint.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMedicalDocumentStorage _storage;

    public DocumentService(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        IMedicalDocumentStorage storage)
    {
        _context = context;
        _userManager = userManager;
        _storage = storage;
    }

    public async Task<MedicalDocumentDto> GetByIdAsync(Guid id, string userId, string role)
    {
        var doc = await _context.MedicalDocuments
            .Include(d => d.PatientProfile)
            .Include(d => d.Appointment).ThenInclude(a => a!.DoctorProfile)
            .FirstOrDefaultAsync(d => d.Id == id && d.DeletionRequestedAt == null)
            ?? throw new NotFoundException("Document", id);
        EnsureCanRead(doc, userId, role);
        return MapToDto(doc);
    }

    public async Task<PagedResult<MedicalDocumentDto>> GetByPatientIdAsync(
        Guid patientId, string userId, string role, int skip = 0, int take = 50)
    {
        (skip, take) = Pagination.Normalize(skip, take);
        var patient = await _context.PatientProfiles.FindAsync(patientId)
            ?? throw new NotFoundException("Patient", patientId);

        IQueryable<MedicalDocument> query = _context.MedicalDocuments
            .AsNoTracking()
            .Where(d => d.PatientProfileId == patientId && d.DeletionRequestedAt == null);

        if (role == "Patient")
        {
            if (patient.UserId != userId) throw new ForbiddenException();
        }
        else if (role == "Doctor")
        {
            var doctor = await _context.DoctorProfiles.FirstOrDefaultAsync(d => d.UserId == userId)
                ?? throw new ForbiddenException();
            if (doctor.ApprovalStatus != DoctorApprovalStatus.Approved)
                throw new ForbiddenException("Only approved doctors can access clinical documents.");
            query = query.Where(d => d.AppointmentId != null &&
                d.Appointment!.DoctorProfileId == doctor.Id &&
                (d.Appointment.Status == AppointmentStatus.Accepted ||
                 d.Appointment.Status == AppointmentStatus.InProgress ||
                 d.Appointment.Status == AppointmentStatus.Completed));
        }
        else if (role != "Admin")
        {
            throw new ForbiddenException();
        }

        var totalCount = await query.CountAsync();
        var docs = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return PagedResult<MedicalDocumentDto>.Create(
            docs.Select(MapToDto).ToList(), totalCount, skip, take);
    }

    public async Task<MedicalDocumentContent> GetContentAsync(Guid id, string userId, string role)
    {
        var doc = await _context.MedicalDocuments
            .Include(d => d.PatientProfile)
            .Include(d => d.Appointment).ThenInclude(a => a!.DoctorProfile)
            .FirstOrDefaultAsync(d => d.Id == id && d.DeletionRequestedAt == null)
            ?? throw new NotFoundException("Document", id);
        EnsureCanRead(doc, userId, role);
        Stream stream;
        try
        {
            stream = await _storage.OpenReadAsync(doc.FileUrl);
        }
        catch (FileNotFoundException)
        {
            throw new NotFoundException("Document content", id);
        }
        return new MedicalDocumentContent(stream, doc.ContentType, doc.FileName);
    }

    public async Task<MedicalDocumentDto> UploadAsync(Guid patientProfileId, string userId, string fileName,
        Stream content, string contentType, string? documentType, long fileSizeBytes,
        Guid? appointmentId = null)
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
        var isTreatingDoctor = doctor != null && appointment != null &&
            ClinicalAccessRules.CanDoctorAccessClinicalData(
                doctor.Id, appointment.DoctorProfileId, doctor.ApprovalStatus, appointment.Status);
        if (!isPatientOwner && !isTreatingDoctor && !await IsAdminAsync(userId))
            throw new ForbiddenException("You cannot upload documents for this patient.");

        var storageKey = await _storage.SaveAsync(content, Path.GetExtension(fileName));
        var doc = new MedicalDocument
        {
            PatientProfileId = patientProfileId,
            UploadedByUserId = userId,
            FileName = fileName,
            FileUrl = storageKey,
            ContentType = contentType,
            DocumentType = documentType,
            FileSizeBytes = fileSizeBytes,
            AppointmentId = appointmentId
        };
        try
        {
            _context.MedicalDocuments.Add(doc);
            await _context.SaveChangesAsync();
        }
        catch
        {
            await _storage.DeleteAsync(storageKey);
            throw;
        }
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

        var storageKey = doc.FileUrl;
        if (doc.DeletionRequestedAt == null)
        {
            doc.DeletionRequestedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        await _storage.DeleteAsync(storageKey);
        _context.MedicalDocuments.Remove(doc);
        await _context.SaveChangesAsync();
    }

    private static MedicalDocumentDto MapToDto(MedicalDocument d) => new()
    {
        Id = d.Id,
        PatientProfileId = d.PatientProfileId,
        AppointmentId = d.AppointmentId,
        FileName = d.FileName,
        DownloadUrl = $"/api/documents/{d.Id}/content",
        ContentType = d.ContentType,
        DocumentType = d.DocumentType,
        FileSizeBytes = d.FileSizeBytes,
        CreatedAt = d.CreatedAt
    };

    private static void EnsureCanRead(MedicalDocument document, string userId, string role)
    {
        if (role == "Admin") return;
        if (role == "Patient" && document.PatientProfile.UserId == userId) return;
        if (role == "Doctor" && document.Appointment != null &&
            document.Appointment.DoctorProfile.UserId == userId &&
            ClinicalAccessRules.CanDoctorAccessClinicalData(
                document.Appointment.DoctorProfile.Id,
                document.Appointment.DoctorProfileId,
                document.Appointment.DoctorProfile.ApprovalStatus,
                document.Appointment.Status)) return;
        throw new ForbiddenException();
    }

    private async Task<bool> IsAdminAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user != null && await _userManager.IsInRoleAsync(user, "Admin");
    }
}
