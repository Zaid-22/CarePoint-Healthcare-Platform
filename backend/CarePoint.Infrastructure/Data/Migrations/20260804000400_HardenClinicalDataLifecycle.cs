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
        // Repair token chains for databases that already applied the original family migration.
        migrationBuilder.Sql("""
            ;WITH TokenFamilies AS
            (
                SELECT token.[Id], token.[Id] AS [FamilyId]
                FROM [RefreshTokens] AS token
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM [RefreshTokens] AS parent
                    WHERE parent.[ReplacedByToken] = token.[Token]
                )

                UNION ALL

                SELECT child.[Id], family.[FamilyId]
                FROM TokenFamilies AS family
                INNER JOIN [RefreshTokens] AS parent ON parent.[Id] = family.[Id]
                INNER JOIN [RefreshTokens] AS child ON child.[Token] = parent.[ReplacedByToken]
            )
            UPDATE token
            SET token.[FamilyId] = family.[FamilyId]
            FROM [RefreshTokens] AS token
            INNER JOIN TokenFamilies AS family ON family.[Id] = token.[Id]
            OPTION (MAXRECURSION 0);
            """);

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
