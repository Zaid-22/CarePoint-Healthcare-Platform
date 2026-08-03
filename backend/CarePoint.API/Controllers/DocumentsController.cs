using System.Security.Claims;
using CarePoint.Application.DTOs.Common;
using CarePoint.Application.DTOs.Medical;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePoint.API.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<MedicalDocumentDto>>> GetById(Guid id)
    {
        var result = await _documentService.GetByIdAsync(id, UserId, Role);
        return Ok(ApiResponse<MedicalDocumentDto>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> GetContent(Guid id)
    {
        var result = await _documentService.GetContentAsync(id, UserId, Role);
        return File(result.Content, result.ContentType, result.FileName, enableRangeProcessing: true);
    }

    [HttpGet("patient/{patientId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MedicalDocumentDto>>>> GetByPatient(
        Guid patientId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var result = await _documentService.GetByPatientIdAsync(patientId, UserId, Role, skip, take);
        return Ok(ApiResponse<IReadOnlyList<MedicalDocumentDto>>.PagedSuccessResponse(result));
    }

    [HttpPost]
    [RequestSizeLimit(MaxFileSizeBytes + 64 * 1024)]
    public async Task<ActionResult<ApiResponse<MedicalDocumentDto>>> Upload(
        [FromForm] UploadMedicalDocumentRequest request)
    {
        if (request.File is null || request.File.Length == 0)
            throw new BadRequestException("Select a non-empty medical document.");
        if (request.File.Length > MaxFileSizeBytes)
            throw new BadRequestException("Medical documents must be 10 MB or smaller.");
        if (request.DocumentType?.Length > 100)
            throw new BadRequestException("Document type must be 100 characters or fewer.");

        var fileName = Path.GetFileName(request.File.FileName);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => throw new BadRequestException("Only PDF, JPG, JPEG, and PNG documents are supported.")
        };

        await using var stream = request.File.OpenReadStream();
        await EnsureFileSignatureAsync(stream, extension);
        var result = await _documentService.UploadAsync(
            request.PatientProfileId, UserId, fileName, stream, contentType,
            request.DocumentType?.Trim(), request.File.Length, request.AppointmentId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<MedicalDocumentDto>.SuccessResponse(result, "Document uploaded securely."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id)
    {
        await _documentService.DeleteAsync(id, UserId);
        return Ok(ApiResponse<string>.SuccessResponse("Document deleted."));
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new ForbiddenException();

    private string Role => User.IsInRole("Admin") ? "Admin" :
        User.IsInRole("Doctor") ? "Doctor" :
        User.IsInRole("Patient") ? "Patient" : throw new ForbiddenException();

    private static async Task EnsureFileSignatureAsync(Stream stream, string extension)
    {
        var header = new byte[8];
        var bytesRead = await stream.ReadAsync(header);
        stream.Position = 0;

        var valid = extension switch
        {
            ".pdf" => bytesRead >= 4 && header[0] == 0x25 && header[1] == 0x50 &&
                      header[2] == 0x44 && header[3] == 0x46,
            ".jpg" or ".jpeg" => bytesRead >= 3 && header[0] == 0xFF &&
                                  header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => bytesRead >= 8 && header.SequenceEqual(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            _ => false
        };

        if (!valid)
            throw new BadRequestException("The file content does not match its extension.");
    }
}

public sealed class UploadMedicalDocumentRequest
{
    public Guid PatientProfileId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string? DocumentType { get; set; }
    public IFormFile File { get; set; } = null!;
}
