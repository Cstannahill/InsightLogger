using InsightLogger.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightLogger.Infrastructure.Persistence.Migrations;

[DbContext(typeof(InsightLoggerDbContext))]
[Migration("20260324021500_AddRuleScopes")]
public partial class AddRuleScopes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ProjectNameCondition",
            table: "Rules",
            type: "TEXT",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RepositoryCondition",
            table: "Rules",
            type: "TEXT",
            maxLength: 200,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Rules_ProjectNameCondition",
            table: "Rules",
            column: "ProjectNameCondition");

        migrationBuilder.CreateIndex(
            name: "IX_Rules_RepositoryCondition",
            table: "Rules",
            column: "RepositoryCondition");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Rules_ProjectNameCondition",
            table: "Rules");

        migrationBuilder.DropIndex(
            name: "IX_Rules_RepositoryCondition",
            table: "Rules");

        migrationBuilder.DropColumn(
            name: "ProjectNameCondition",
            table: "Rules");

        migrationBuilder.DropColumn(
            name: "RepositoryCondition",
            table: "Rules");
    }
}
