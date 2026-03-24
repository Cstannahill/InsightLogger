using System;
using InsightLogger.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightLogger.Infrastructure.Persistence.Migrations;

[DbContext(typeof(InsightLoggerDbContext))]
[Migration("20260324011000_AddRuleMatchStats")]
public partial class AddRuleMatchStats : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "LastMatchedAtUtc",
            table: "Rules",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "MatchCount",
            table: "Rules",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "IX_Rules_LastMatchedAtUtc",
            table: "Rules",
            column: "LastMatchedAtUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Rules_LastMatchedAtUtc",
            table: "Rules");

        migrationBuilder.DropColumn(
            name: "LastMatchedAtUtc",
            table: "Rules");

        migrationBuilder.DropColumn(
            name: "MatchCount",
            table: "Rules");
    }
}
