using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CarePoint.Application.DTOs.Common;
using CarePoint.Application.DTOs.Doctors;
using CarePoint.Application.Interfaces;
using CarePoint.Domain.Enums;

namespace CarePoint.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;

    public DoctorsController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    [HttpGet]
    [HttpGet("approved")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DoctorDto>>>> GetAll(
        [FromQuery] string? specialty, [FromQuery] string? name,
        [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var result = await _doctorService.GetAllAsync(specialty, name, skip, take);
        return Ok(ApiResponse<IReadOnlyList<DoctorDto>>.PagedSuccessResponse(result));
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
    public async Task<ActionResult<ApiResponse<DoctorDto>>> GetById(Guid id)
    {
        var result = await _doctorService.GetByIdAsync(id);
        return Ok(ApiResponse<DoctorDto>.SuccessResponse(result));
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
}
