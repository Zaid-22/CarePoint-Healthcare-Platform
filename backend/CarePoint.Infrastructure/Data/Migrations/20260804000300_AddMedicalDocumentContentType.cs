using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace CarePoint.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260804000300_AddMedicalDocumentContentType")]
public partial class AddMedicalDocumentContentType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ContentType",
            table: "MedicalDocuments",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "application/octet-stream");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ContentType", table: "MedicalDocuments");
    }
}
