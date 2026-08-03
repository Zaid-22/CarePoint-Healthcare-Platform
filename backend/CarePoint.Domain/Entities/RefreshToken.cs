using CarePoint.Domain.Common;

namespace CarePoint.Domain.Entities;

/// <summary>
/// JWT refresh token for secure token rotation.
/// When a token is used, it's revoked and replaced by a new one.
/// </summary>
public class RefreshToken : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? CreatedByIp { get; set; }
}
