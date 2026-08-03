using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CarePoint.Application.DTOs.Common;
using CarePoint.Application.DTOs.Patients;
using CarePoint.Application.Interfaces;

namespace CarePoint.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PatientDto>>>> GetAll(
        [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var result = await _patientService.GetAllAsync(skip, take);
        return Ok(ApiResponse<IReadOnlyList<PatientDto>>.PagedSuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PatientDto>>> GetById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        var result = await _patientService.GetByIdAsync(id, userId, role);
        return Ok(ApiResponse<PatientDto>.SuccessResponse(result));
    }

    [HttpGet("me")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<ApiResponse<PatientDto>>> GetMyProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _patientService.GetByUserIdAsync(userId);
        return Ok(ApiResponse<PatientDto>.SuccessResponse(result));
    }

    [HttpPut("me")]
    [HttpPut]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<ApiResponse<PatientDto>>> UpdateMyProfile([FromBody] UpdatePatientDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _patientService.UpdateMyProfileAsync(userId, dto);
        return Ok(ApiResponse<PatientDto>.SuccessResponse(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<ApiResponse<PatientDto>>> Update(Guid id, [FromBody] UpdatePatientDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _patientService.UpdateProfileAsync(id, userId, dto);
        return Ok(ApiResponse<PatientDto>.SuccessResponse(result));
    }
}
