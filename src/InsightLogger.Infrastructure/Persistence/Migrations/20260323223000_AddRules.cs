using System;
using InsightLogger.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightLogger.Infrastructure.Persistence.Migrations;

[DbContext(typeof(InsightLoggerDbContext))]
[Migration("20260323223000_AddRules")]
public partial class AddRules : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Rules",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: true),
                IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                Priority = table.Column<int>(type: "INTEGER", nullable: false),
                ToolKindCondition = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                CodeCondition = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                SeverityCondition = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                CategoryCondition = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                MessageRegexCondition = table.Column<string>(type: "TEXT", nullable: true),
                FilePathRegexCondition = table.Column<string>(type: "TEXT", nullable: true),
                FingerprintCondition = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                TitleAction = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                ExplanationAction = table.Column<string>(type: "TEXT", nullable: true),
                SuggestedFixesJson = table.Column<string>(type: "TEXT", nullable: true),
                ConfidenceAdjustmentAction = table.Column<double>(type: "REAL", nullable: false),
                MarkAsPrimaryCauseAction = table.Column<bool>(type: "INTEGER", nullable: false),
                TagsJson = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Rules", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Rules_Name",
            table: "Rules",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Rules_IsEnabled_ToolKindCondition",
            table: "Rules",
            columns: new[] { "IsEnabled", "ToolKindCondition" });

        migrationBuilder.CreateIndex(
            name: "IX_Rules_FingerprintCondition",
            table: "Rules",
            column: "FingerprintCondition");

        migrationBuilder.CreateIndex(
            name: "IX_Rules_Priority",
            table: "Rules",
            column: "Priority");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Rules");
    }
}
