using InsightLogger.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightLogger.Infrastructure.Persistence.Migrations;

[DbContext(typeof(InsightLoggerDbContext))]
[Migration("20260324084500_AddAnalysisResultSnapshot")]
public partial class AddAnalysisResultSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AnalysisSnapshotJson",
            table: "Analyses",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AnalysisSnapshotJson",
            table: "Analyses");
    }
}
