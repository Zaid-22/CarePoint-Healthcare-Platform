using Microsoft.AspNetCore.Identity;
using CarePoint.Domain.Entities;

namespace CarePoint.Infrastructure.Identity;

/// <summary>
/// The actual Identity user that extends IdentityUser.
/// Domain layer's entities reference UserId (string) — this class lives in Infrastructure only.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public PatientProfile? PatientProfile { get; set; }
    public DoctorProfile? DoctorProfile { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
