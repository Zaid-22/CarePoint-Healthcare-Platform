using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace CarePoint.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260804000200_AddRefreshTokenFamilies")]
public partial class AddRefreshTokenFamilies : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "FamilyId",
            table: "RefreshTokens",
            type: "uniqueidentifier",
            nullable: true);

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

            UPDATE [RefreshTokens]
            SET [FamilyId] = NEWID()
            WHERE [FamilyId] IS NULL;
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "FamilyId",
            table: "RefreshTokens",
            type: "uniqueidentifier",
            nullable: false,
            defaultValueSql: "NEWID()",
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_FamilyId",
            table: "RefreshTokens",
            column: "FamilyId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_RefreshTokens_FamilyId",
            table: "RefreshTokens");

        migrationBuilder.DropColumn(
            name: "FamilyId",
            table: "RefreshTokens");
    }
}
