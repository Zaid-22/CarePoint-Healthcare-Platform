using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CarePoint.Application.DTOs.Common;
using CarePoint.Application.DTOs.Doctors;
using CarePoint.Application.Interfaces;

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
        [FromQuery] string? specialty, [FromQuery] string? name)
    {
        var result = await _doctorService.GetAllAsync(specialty, name);
        return Ok(ApiResponse<IReadOnlyList<DoctorDto>>.SuccessResponse(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/all")]
    [HttpGet("all")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DoctorDto>>>> GetAllForAdmin()
    {
        var result = await _doctorService.GetAllForAdminAsync();
        return Ok(ApiResponse<IReadOnlyList<DoctorDto>>.SuccessResponse(result));
    }

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
        var result = await _doctorService.GetAvailabilityAsync(doctorId);
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
