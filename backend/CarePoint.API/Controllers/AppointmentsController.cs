using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CarePoint.Application.DTOs.Appointments;
using CarePoint.Application.DTOs.Common;
using CarePoint.Application.Interfaces;

namespace CarePoint.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpGet]
    [HttpGet("my-appointments")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AppointmentDto>>>> GetAll(
        [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        var result = await _appointmentService.GetAllAsync(userId, role, skip, take);
        return Ok(ApiResponse<IReadOnlyList<AppointmentDto>>.PagedSuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> GetById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        var result = await _appointmentService.GetByIdAsync(id, userId, role);
        return Ok(ApiResponse<AppointmentDto>.SuccessResponse(result));
    }

    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> Create([FromBody] CreateAppointmentDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _appointmentService.CreateAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<AppointmentDto>.SuccessResponse(result));
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> UpdateStatus(Guid id, [FromBody] UpdateAppointmentStatusDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        var result = await _appointmentService.UpdateStatusAsync(id, userId, role, dto);
        return Ok(ApiResponse<AppointmentDto>.SuccessResponse(result));
    }

    [HttpPut("{id:guid}/reschedule")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> Reschedule(Guid id, [FromBody] RescheduleAppointmentDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _appointmentService.RescheduleAsync(id, userId, dto);
        return Ok(ApiResponse<AppointmentDto>.SuccessResponse(result));
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> Cancel(Guid id, [FromBody] CancelAppointmentDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        var result = await _appointmentService.CancelAsync(id, userId, role, dto);
        return Ok(ApiResponse<AppointmentDto>.SuccessResponse(result));
    }
}
