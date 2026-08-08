using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePoint.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class HardenDoctorProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchivedProfilePictureValue",
                table: "DoctorProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [DoctorProfiles]
                SET [ArchivedProfilePictureValue] = [ProfilePictureUrl],
                    [ProfilePictureUrl] = CASE
                        WHEN [ProfilePictureUrl] LIKE N'data:image/%;base64,%'
                            THEN N'/api/doctors/' + CONVERT(nvarchar(36), [Id]) + N'/avatar?v=legacy'
                        ELSE NULL
                    END
                WHERE [ProfilePictureUrl] IS NOT NULL
                  AND ([ProfilePictureUrl] LIKE N'data:image/%;base64,%'
                       OR DATALENGTH([ProfilePictureUrl]) / 2 > 1000
                       OR [ProfilePictureUrl] NOT LIKE N'https://%');
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ProfilePictureUrl",
                table: "DoctorProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureStorageKey",
                table: "DoctorProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePictureStorageKey",
                table: "DoctorProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "ProfilePictureUrl",
                table: "DoctorProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.Sql("""
                UPDATE [DoctorProfiles]
                SET [ProfilePictureUrl] = [ArchivedProfilePictureValue]
                WHERE [ArchivedProfilePictureValue] IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "ArchivedProfilePictureValue",
                table: "DoctorProfiles");
        }
    }
}
