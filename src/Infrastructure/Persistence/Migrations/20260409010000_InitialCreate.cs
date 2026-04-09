using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Projects",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProjectNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Projects", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "WorkInstructionJobs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProjectNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                ExternalReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                RequestPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkInstructionJobs", x => x.Id);
                table.ForeignKey(
                    name: "FK_WorkInstructionJobs_Projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "WorkInstructionExecutions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                WorkInstructionJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                Outcome = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkInstructionExecutions", x => x.Id);
                table.ForeignKey(
                    name: "FK_WorkInstructionExecutions_WorkInstructionJobs_WorkInstructionJobId",
                    column: x => x.WorkInstructionJobId,
                    principalTable: "WorkInstructionJobs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Projects_ProjectNumber",
            table: "Projects",
            column: "ProjectNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_WorkInstructionExecutions_StartedAtUtc",
            table: "WorkInstructionExecutions",
            column: "StartedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_WorkInstructionExecutions_WorkInstructionJobId",
            table: "WorkInstructionExecutions",
            column: "WorkInstructionJobId");

        migrationBuilder.CreateIndex(
            name: "IX_WorkInstructionJobs_CreatedAtUtc",
            table: "WorkInstructionJobs",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_WorkInstructionJobs_ProjectId_CreatedAtUtc",
            table: "WorkInstructionJobs",
            columns: new[] { "ProjectId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_WorkInstructionJobs_ProjectNumber",
            table: "WorkInstructionJobs",
            column: "ProjectNumber");

        migrationBuilder.CreateIndex(
            name: "IX_WorkInstructionJobs_Status",
            table: "WorkInstructionJobs",
            column: "Status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "WorkInstructionExecutions");

        migrationBuilder.DropTable(
            name: "WorkInstructionJobs");

        migrationBuilder.DropTable(
            name: "Projects");
    }
}
