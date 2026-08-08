using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CarePoint.Application.DTOs.Common;
using CarePoint.Application.DTOs.Doctors;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Enums;
using CarePoint.Domain.Exceptions;
using Microsoft.AspNetCore.RateLimiting;

namespace CarePoint.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private const long MaxProfileImageBytes = 1024 * 1024;
    private readonly IDoctorService _doctorService;

    public DoctorsController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    [HttpGet]
    [HttpGet("approved")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PublicDoctorDto>>>> GetAll(
        [FromQuery] string? specialty, [FromQuery] string? name,
        [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var result = await _doctorService.GetAllAsync(specialty, name, skip, take);
        return Ok(ApiResponse<IReadOnlyList<PublicDoctorDto>>.PagedSuccessResponse(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/all")]
    [HttpGet("all")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DoctorDto>>>> GetAllForAdmin(
        [FromQuery] DoctorApprovalStatus? status = null,
        [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var result = await _doctorService.GetAllForAdminAsync(status, skip, take);
        return Ok(ApiResponse<IReadOnlyList<DoctorDto>>.PagedSuccessResponse(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/summary")]
    public async Task<ActionResult<ApiResponse<DoctorAdminSummaryDto>>> GetAdminSummary() =>
        Ok(ApiResponse<DoctorAdminSummaryDto>.SuccessResponse(await _doctorService.GetAdminSummaryAsync()));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PublicDoctorDto>>> GetById(Guid id)
    {
        var result = await _doctorService.GetByIdAsync(id);
        return Ok(ApiResponse<PublicDoctorDto>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}/avatar")]
    public async Task<IActionResult> GetProfileImage(Guid id)
    {
        var result = await _doctorService.GetProfileImageAsync(id);
        Response.Headers.CacheControl = "public, max-age=3600";
        return File(result.Content, result.ContentType);
    }

    [Authorize(Roles = "Doctor")]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> GetMyProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _doctorService.GetProfileByUserIdAsync(userId);
        return Ok(ApiResponse<DoctorDto>.SuccessResponse(result));
    }

    [Authorize(Roles = "Doctor")]
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> UpdateMyProfile([FromBody] UpdateDoctorDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _doctorService.UpdateProfileByUserIdAsync(userId, dto);
        return Ok(ApiResponse<DoctorDto>.SuccessResponse(result));
    }

    [Authorize(Roles = "Doctor")]
    [EnableRateLimiting("document-upload")]
    [RequestSizeLimit(MaxProfileImageBytes + 64 * 1024)]
    [HttpPost("me/avatar")]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> UploadProfileImage(
        [FromForm] UploadProfileImageRequest request)
    {
        if (request.File is null || request.File.Length == 0)
            throw new BadRequestException("Select a non-empty profile image.");
        if (request.File.Length > MaxProfileImageBytes)
            throw new BadRequestException("Profile images must be 1 MB or smaller.");

        var extension = Path.GetExtension(Path.GetFileName(request.File.FileName)).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png"))
            throw new BadRequestException("Only JPG, JPEG, and PNG profile images are supported.");

        await using var stream = request.File.OpenReadStream();
        await EnsureImageSignatureAsync(stream, extension);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _doctorService.UploadProfileImageAsync(userId, stream, extension);
        return Ok(ApiResponse<DoctorDto>.SuccessResponse(result, "Profile image uploaded."));
    }

    [Authorize(Roles = "Doctor")]
    [HttpPost("profile")]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> CreateProfile([FromBody] CreateDoctorDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _doctorService.CreateProfileAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<DoctorDto>.SuccessResponse(result));
    }

    [Authorize(Roles = "Doctor")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> Update(Guid id, [FromBody] UpdateDoctorDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _doctorService.UpdateProfileAsync(id, userId, dto);
        return Ok(ApiResponse<DoctorDto>.SuccessResponse(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> Approve(Guid id)
    {
        var result = await _doctorService.ApproveAsync(id);
        return Ok(ApiResponse<DoctorDto>.SuccessResponse(result, "Doctor approved."));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> Reject(Guid id)
    {
        var result = await _doctorService.RejectAsync(id);
        return Ok(ApiResponse<DoctorDto>.SuccessResponse(result, "Doctor rejected."));
    }

    // --- Availability ---

    [HttpGet("{doctorId:guid}/availability")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DoctorAvailabilityDto>>>> GetAvailability(Guid doctorId)
    {
        var userId = User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
        var role = User.IsInRole("Admin") ? "Admin" : User.IsInRole("Doctor") ? "Doctor" : null;
        var result = await _doctorService.GetAvailabilityAsync(doctorId, userId, role);
        return Ok(ApiResponse<IReadOnlyList<DoctorAvailabilityDto>>.SuccessResponse(result));
    }

    [Authorize(Roles = "Doctor")]
    [HttpPost("{doctorId:guid}/availability")]
    public async Task<ActionResult<ApiResponse<DoctorAvailabilityDto>>> AddAvailability(
        Guid doctorId, [FromBody] CreateAvailabilityDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _doctorService.AddAvailabilityAsync(doctorId, userId, dto);
        return Ok(ApiResponse<DoctorAvailabilityDto>.SuccessResponse(result));
    }

    [Authorize(Roles = "Doctor")]
    [HttpPut("{doctorId:guid}/availability/{slotId:guid}")]
    public async Task<ActionResult<ApiResponse<DoctorAvailabilityDto>>> UpdateAvailability(
        Guid doctorId, Guid slotId, [FromBody] CreateAvailabilityDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _doctorService.UpdateAvailabilityAsync(doctorId, slotId, userId, dto);
        return Ok(ApiResponse<DoctorAvailabilityDto>.SuccessResponse(result));
    }

    [Authorize(Roles = "Doctor")]
    [HttpDelete("{doctorId:guid}/availability/{slotId:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteAvailability(Guid doctorId, Guid slotId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _doctorService.DeleteAvailabilityAsync(doctorId, slotId, userId);
        return Ok(ApiResponse<string>.SuccessResponse("Availability removed."));
    }

    [HttpGet("{doctorId:guid}/slots")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AvailableSlotDto>>>> GetSlots(
        Guid doctorId, [FromQuery] DateTime date)
    {
        var result = await _doctorService.GetAvailableSlotsAsync(doctorId, date);
        return Ok(ApiResponse<IReadOnlyList<AvailableSlotDto>>.SuccessResponse(result));
    }

    private static async Task EnsureImageSignatureAsync(Stream stream, string extension)
    {
        var header = new byte[8];
        var bytesRead = await stream.ReadAsync(header);
        stream.Position = 0;
        var valid = extension switch
        {
            ".jpg" or ".jpeg" => bytesRead >= 3 && header[0] == 0xFF &&
                                  header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => bytesRead >= 8 && header.SequenceEqual(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            _ => false
        };
        if (!valid)
            throw new BadRequestException("The image content does not match its extension.");
    }
}

public sealed class UploadProfileImageRequest
{
    public IFormFile File { get; set; } = null!;
}
