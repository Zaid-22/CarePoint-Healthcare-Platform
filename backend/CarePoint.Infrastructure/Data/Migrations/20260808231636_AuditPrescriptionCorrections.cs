using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePoint.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuditPrescriptionCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Prescriptions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "PrescriptionRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PreviousRowVersion = table.Column<byte[]>(type: "binary(8)", fixedLength: true, maxLength: 8, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionRevisions_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionRevisionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Dosage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Duration = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionRevisionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionRevisionItems_PrescriptionRevisions_PrescriptionRevisionId",
                        column: x => x.PrescriptionRevisionId,
                        principalTable: "PrescriptionRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionRevisionItems_PrescriptionRevisionId",
                table: "PrescriptionRevisionItems",
                column: "PrescriptionRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionRevisions_EditedByUserId",
                table: "PrescriptionRevisions",
                column: "EditedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionRevisions_PrescriptionId_CreatedAt",
                table: "PrescriptionRevisions",
                columns: new[] { "PrescriptionId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrescriptionRevisionItems");

            migrationBuilder.DropTable(
                name: "PrescriptionRevisions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Prescriptions");
        }
    }
}
