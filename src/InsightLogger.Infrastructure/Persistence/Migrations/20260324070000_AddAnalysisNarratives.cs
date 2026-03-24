using InsightLogger.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightLogger.Infrastructure.Persistence.Migrations;

[DbContext(typeof(InsightLoggerDbContext))]
[Migration("20260324070000_AddAnalysisNarratives")]
public partial class AddAnalysisNarratives : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "NarrativeGroupSummariesJson",
            table: "Analyses",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "NarrativeFallbackUsed",
            table: "Analyses",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "NarrativeModel",
            table: "Analyses",
            type: "TEXT",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "NarrativeProvider",
            table: "Analyses",
            type: "TEXT",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "NarrativeReason",
            table: "Analyses",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "NarrativeRecommendedNextStepsJson",
            table: "Analyses",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "NarrativeSource",
            table: "Analyses",
            type: "TEXT",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "NarrativeStatus",
            table: "Analyses",
            type: "TEXT",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "NarrativeSummary",
            table: "Analyses",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProjectName",
            table: "Analyses",
            type: "TEXT",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Repository",
            table: "Analyses",
            type: "TEXT",
            maxLength: 200,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Analyses_NarrativeSource",
            table: "Analyses",
            column: "NarrativeSource");

        migrationBuilder.CreateIndex(
            name: "IX_Analyses_ProjectName",
            table: "Analyses",
            column: "ProjectName");

        migrationBuilder.CreateIndex(
            name: "IX_Analyses_Repository",
            table: "Analyses",
            column: "Repository");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Analyses_NarrativeSource",
            table: "Analyses");

        migrationBuilder.DropIndex(
            name: "IX_Analyses_ProjectName",
            table: "Analyses");

        migrationBuilder.DropIndex(
            name: "IX_Analyses_Repository",
            table: "Analyses");

        migrationBuilder.DropColumn(name: "NarrativeGroupSummariesJson", table: "Analyses");
        migrationBuilder.DropColumn(name: "NarrativeFallbackUsed", table: "Analyses");
        migrationBuilder.DropColumn(name: "NarrativeModel", table: "Analyses");
        migrationBuilder.DropColumn(name: "NarrativeProvider", table: "Analyses");
        migrationBuilder.DropColumn(name: "NarrativeReason", table: "Analyses");
        migrationBuilder.DropColumn(name: "NarrativeRecommendedNextStepsJson", table: "Analyses");
        migrationBuilder.DropColumn(name: "NarrativeSource", table: "Analyses");
        migrationBuilder.DropColumn(name: "NarrativeStatus", table: "Analyses");
        migrationBuilder.DropColumn(name: "NarrativeSummary", table: "Analyses");
        migrationBuilder.DropColumn(name: "ProjectName", table: "Analyses");
        migrationBuilder.DropColumn(name: "Repository", table: "Analyses");
    }
}
