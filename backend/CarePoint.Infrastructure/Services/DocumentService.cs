using Microsoft.EntityFrameworkCore;
using CarePoint.Application.DTOs.Medical;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Entities;
using CarePoint.Domain.Exceptions;
using CarePoint.Infrastructure.Data;

namespace CarePoint.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;

    public DocumentService(ApplicationDbContext context) => _context = context;

    public async Task<MedicalDocumentDto> GetByIdAsync(Guid id, string userId, string role)
    {
        var doc = await _context.MedicalDocuments.FindAsync(id)
            ?? throw new NotFoundException("Document", id);
        return MapToDto(doc);
    }

    public async Task<IReadOnlyList<MedicalDocumentDto>> GetByPatientIdAsync(Guid patientId, string userId, string role)
    {
        var docs = await _context.MedicalDocuments
            .Where(d => d.PatientProfileId == patientId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
        return docs.Select(MapToDto).ToList();
    }

    public async Task<MedicalDocumentDto> UploadAsync(Guid patientProfileId, string userId, string fileName,
        string fileUrl, string? documentType, long fileSizeBytes, Guid? appointmentId = null)
    {
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
        var doc = await _context.MedicalDocuments.FindAsync(id)
            ?? throw new NotFoundException("Document", id);
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
}
