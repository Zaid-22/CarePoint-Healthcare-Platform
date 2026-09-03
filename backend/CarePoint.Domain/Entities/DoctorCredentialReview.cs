using CarePoint.Domain.Common;
using CarePoint.Domain.Enums;

namespace CarePoint.Domain.Entities;

/// <summary>
/// Immutable administrative decision with a snapshot of the credentials reviewed.
/// </summary>
public class DoctorCredentialReview : BaseEntity
{
    public Guid DoctorProfileId { get; set; }
    public DoctorProfile DoctorProfile { get; set; } = null!;
    public string ReviewedByUserId { get; set; } = string.Empty;
    public DoctorApprovalStatus Decision { get; set; }
    public string? LicenseNumber { get; set; }
    public string? LicensingAuthority { get; set; }
    public DateTime? LicenseExpiryDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}
