using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePoint.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260904000100_RequireCurrentDoctorCredentials")]
public partial class RequireCurrentDoctorCredentials : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE [DoctorProfiles]
            SET [ApprovalStatus] = N'Pending',
                [CredentialsReviewedAt] = NULL,
                [CredentialsReviewedByUserId] = NULL,
                [CredentialReviewNotes] = NULL
            WHERE [ApprovalStatus] = N'Approved'
              AND
              (
                  NULLIF(LTRIM(RTRIM([LicenseNumber])), N'') IS NULL
                  OR NULLIF(LTRIM(RTRIM([LicensingAuthority])), N'') IS NULL
                  OR [LicenseExpiryDate] IS NULL
                  OR [LicenseExpiryDate] <= CONVERT(date, SYSUTCDATETIME())
                  OR [CredentialsReviewedAt] IS NULL
              );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Credential approval cannot be safely reconstructed once invalid approvals are removed.
    }
}
