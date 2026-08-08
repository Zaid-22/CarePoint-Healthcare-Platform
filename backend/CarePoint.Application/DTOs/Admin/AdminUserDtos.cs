namespace CarePoint.Application.DTOs.Admin;

public sealed class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public bool IsDisabled { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class SetUserDisabledDto
{
    public bool Disabled { get; set; }
}
