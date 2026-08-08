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
            migrationBuilder.Sql("""
                UPDATE [DoctorProfiles]
                SET [ProfilePictureUrl] = NULL
                WHERE [ProfilePictureUrl] LIKE N'data:%'
                   OR LEN([ProfilePictureUrl]) > 1000;
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
        }
    }
}
