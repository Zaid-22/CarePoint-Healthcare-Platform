using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CarePoint.Application.DTOs.Common;
using CarePoint.Application.DTOs.Doctors;
using CarePoint.Application.DTOs.Medical;
using CarePoint.Application.Interfaces;

namespace CarePoint.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicalRecordsController : ControllerBase
{
    private readonly IMedicalRecordService _service;

    public MedicalRecordsController(IMedicalRecordService service) => _service = service;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<MedicalRecordDto>>> GetById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        return Ok(ApiResponse<MedicalRecordDto>.SuccessResponse(await _service.GetByIdAsync(id, userId, role)));
    }

    [HttpGet("patient/{patientId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MedicalRecordDto>>>> GetByPatient(
        Guid patientId, [FromQuery] string? search = null,
        [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        var page = await _service.GetByPatientIdAsync(patientId, userId, role, search, skip, take);
        return Ok(ApiResponse<IReadOnlyList<MedicalRecordDto>>.PagedSuccessResponse(page));
    }

    [Authorize(Roles = "Patient")]
    [HttpGet("my-history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MedicalRecordDto>>>> GetMyHistory(
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var page = await _service.GetMyHistoryAsync(userId, search, skip, take);
        return Ok(ApiResponse<IReadOnlyList<MedicalRecordDto>>.PagedSuccessResponse(page));
    }

    [HttpGet("{id:guid}/revisions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MedicalRecordRevisionDto>>>> GetRevisions(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        var revisions = await _service.GetRevisionsAsync(id, userId, role);
        return Ok(ApiResponse<IReadOnlyList<MedicalRecordRevisionDto>>.SuccessResponse(revisions));
    }

    [Authorize(Roles = "Doctor")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<MedicalRecordDto>>> Create([FromBody] CreateMedicalRecordDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _service.CreateAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<MedicalRecordDto>.SuccessResponse(result));
    }

    [Authorize(Roles = "Doctor")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<MedicalRecordDto>>> Update(Guid id, [FromBody] UpdateMedicalRecordDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(ApiResponse<MedicalRecordDto>.SuccessResponse(await _service.UpdateAsync(id, userId, dto)));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PrescriptionsController : ControllerBase
{
    private readonly IPrescriptionService _service;

    public PrescriptionsController(IPrescriptionService service) => _service = service;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> GetById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        return Ok(ApiResponse<PrescriptionDto>.SuccessResponse(await _service.GetByIdAsync(id, userId, role)));
    }

    [HttpGet("appointment/{appointmentId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PrescriptionDto>>>> GetByAppointment(
        Guid appointmentId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        var page = await _service.GetByAppointmentIdAsync(appointmentId, userId, role, skip, take);
        return Ok(ApiResponse<IReadOnlyList<PrescriptionDto>>.PagedSuccessResponse(page));
    }

    [Authorize(Roles = "Patient")]
    [HttpGet("my-prescriptions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PrescriptionDto>>>> GetMyPrescriptions(
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var page = await _service.GetMyPrescriptionsAsync(userId, search, skip, take);
        return Ok(ApiResponse<IReadOnlyList<PrescriptionDto>>.PagedSuccessResponse(page));
    }

    [Authorize(Roles = "Doctor")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Create([FromBody] CreatePrescriptionDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _service.CreateAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<PrescriptionDto>.SuccessResponse(result));
    }

    [Authorize(Roles = "Doctor")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Update(
        Guid id, [FromBody] CreatePrescriptionDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(ApiResponse<PrescriptionDto>.SuccessResponse(
            await _service.UpdateAsync(id, userId, dto)));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationDto>>>> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(ApiResponse<IReadOnlyList<NotificationDto>>.SuccessResponse(await _service.GetByUserIdAsync(userId)));
    }

    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<string>>> MarkAsRead(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _service.MarkAsReadAsync(id, userId);
        return Ok(ApiResponse<string>.SuccessResponse("Notification marked as read."));
    }

    [HttpPut("read-all")]
    public async Task<ActionResult<ApiResponse<string>>> MarkAllAsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _service.MarkAllAsReadAsync(userId);
        return Ok(ApiResponse<string>.SuccessResponse("All notifications marked as read."));
    }
}

[ApiController]
[Route("api/[controller]")]
public class SpecialtiesController : ControllerBase
{
    private readonly ISpecialtyService _service;

    public SpecialtiesController(ISpecialtyService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SpecialtyDto>>>> GetAll()
    {
        return Ok(ApiResponse<IReadOnlyList<SpecialtyDto>>.SuccessResponse(await _service.GetAllAsync()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SpecialtyDto>>> GetById(Guid id)
    {
        return Ok(ApiResponse<SpecialtyDto>.SuccessResponse(await _service.GetByIdAsync(id)));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SpecialtyDto>>> Create([FromBody] CreateSpecialtyDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<SpecialtyDto>.SuccessResponse(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SpecialtyDto>>> Update(Guid id, [FromBody] CreateSpecialtyDto dto)
    {
        return Ok(ApiResponse<SpecialtyDto>.SuccessResponse(await _service.UpdateAsync(id, dto)));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return Ok(ApiResponse<string>.SuccessResponse("Specialty deactivated successfully."));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("seed")]
    public async Task<ActionResult<ApiResponse<string>>> SeedSpecialties()
    {
        var count = await _service.SeedSpecialtiesAsync();
        return Ok(ApiResponse<string>.SuccessResponse($"Successfully seeded {count} specialties into the database."));
    }
}

[ApiController]
[Route("api/[controller]")]
public class ClinicsController : ControllerBase
{
    private readonly IClinicService _service;

    public ClinicsController(IClinicService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ClinicDto>>>> GetAll()
    {
        return Ok(ApiResponse<IReadOnlyList<ClinicDto>>.SuccessResponse(await _service.GetAllAsync()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClinicDto>>> GetById(Guid id)
    {
        return Ok(ApiResponse<ClinicDto>.SuccessResponse(await _service.GetByIdAsync(id)));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ClinicDto>>> Create([FromBody] CreateClinicDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<ClinicDto>.SuccessResponse(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClinicDto>>> Update(
        Guid id, [FromBody] CreateClinicDto dto)
    {
        return Ok(ApiResponse<ClinicDto>.SuccessResponse(await _service.UpdateAsync(id, dto)));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return Ok(ApiResponse<string>.SuccessResponse("Clinic deactivated successfully."));
    }
}
