using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePoint.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260804000400_HardenClinicalDataLifecycle")]
public partial class HardenClinicalDataLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "Appointments",
            type: "rowversion",
            rowVersion: true,
            nullable: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "DeletionRequestedAt",
            table: "MedicalDocuments",
            type: "datetime2",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "RowVersion", table: "Appointments");
        migrationBuilder.DropColumn(name: "DeletionRequestedAt", table: "MedicalDocuments");
    }
}
