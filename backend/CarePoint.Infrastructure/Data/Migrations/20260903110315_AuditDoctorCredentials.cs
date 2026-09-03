using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePoint.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuditDoctorCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CredentialReviewNotes",
                table: "DoctorProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CredentialsReviewedAt",
                table: "DoctorProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CredentialsReviewedByUserId",
                table: "DoctorProfiles",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LicenseExpiryDate",
                table: "DoctorProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseNumber",
                table: "DoctorProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicensingAuthority",
                table: "DoctorProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorProfiles_LicensingAuthority_LicenseNumber",
                table: "DoctorProfiles",
                columns: new[] { "LicensingAuthority", "LicenseNumber" },
                unique: true,
                filter: "[LicensingAuthority] IS NOT NULL AND [LicenseNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DoctorProfiles_LicensingAuthority_LicenseNumber",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "CredentialReviewNotes",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "CredentialsReviewedAt",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "CredentialsReviewedByUserId",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "LicenseExpiryDate",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "LicenseNumber",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "LicensingAuthority",
                table: "DoctorProfiles");
        }
    }
}
