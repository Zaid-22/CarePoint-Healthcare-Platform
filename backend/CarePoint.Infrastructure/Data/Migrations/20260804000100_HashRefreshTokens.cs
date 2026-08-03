using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePoint.Infrastructure.Data.Migrations;

[Migration("20260804000100_HashRefreshTokens")]
public partial class HashRefreshTokens : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE [RefreshTokens]
            SET [Token] = CONVERT(varchar(64), HASHBYTES('SHA2_256', [Token]), 2)
            WHERE LEN([Token]) <> 64 OR [Token] LIKE '%[^0-9A-Fa-f]%';

            UPDATE [RefreshTokens]
            SET [ReplacedByToken] = CONVERT(varchar(64), HASHBYTES('SHA2_256', [ReplacedByToken]), 2)
            WHERE [ReplacedByToken] IS NOT NULL
              AND (LEN([ReplacedByToken]) <> 64 OR [ReplacedByToken] LIKE '%[^0-9A-Fa-f]%');
            """);

        migrationBuilder.AlterColumn<string>(
            name: "Token",
            table: "RefreshTokens",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(500)",
            oldMaxLength: 500);

        migrationBuilder.AlterColumn<string>(
            name: "ReplacedByToken",
            table: "RefreshTokens",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(500)",
            oldMaxLength: 500,
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Token",
            table: "RefreshTokens",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(64)",
            oldMaxLength: 64);

        migrationBuilder.AlterColumn<string>(
            name: "ReplacedByToken",
            table: "RefreshTokens",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(64)",
            oldMaxLength: 64,
            oldNullable: true);
    }
}
