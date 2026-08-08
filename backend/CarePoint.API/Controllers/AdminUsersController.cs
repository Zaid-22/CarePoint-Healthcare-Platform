using System.Security.Claims;
using CarePoint.Application.DTOs.Admin;
using CarePoint.Application.DTOs.Common;
using CarePoint.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePoint.API.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _service;

    public AdminUsersController(IAdminUserService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminUserDto>>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var result = await _service.GetAllAsync(search, role, skip, take);
        return Ok(ApiResponse<IReadOnlyList<AdminUserDto>>.PagedSuccessResponse(result));
    }

    [HttpPut("{id}/disabled")]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> SetDisabled(
        string id, [FromBody] SetUserDisabledDto dto)
    {
        var actorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _service.SetDisabledAsync(id, actorUserId, dto.Disabled);
        return Ok(ApiResponse<AdminUserDto>.SuccessResponse(result));
    }
}
