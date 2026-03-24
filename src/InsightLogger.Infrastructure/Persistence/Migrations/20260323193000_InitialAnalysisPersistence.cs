using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using InsightLogger.Infrastructure.Persistence.Db;

#nullable disable

namespace InsightLogger.Infrastructure.Persistence.Migrations;

[DbContext(typeof(InsightLoggerDbContext))]
[Migration("20260323193000_InitialAnalysisPersistence")]
public partial class InitialAnalysisPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Analyses",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                InputType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                ToolDetected = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                TotalDiagnostics = table.Column<int>(type: "INTEGER", nullable: false),
                GroupCount = table.Column<int>(type: "INTEGER", nullable: false),
                PrimaryIssueCount = table.Column<int>(type: "INTEGER", nullable: false),
                ErrorCount = table.Column<int>(type: "INTEGER", nullable: false),
                WarningCount = table.Column<int>(type: "INTEGER", nullable: false),
                UsedAi = table.Column<bool>(type: "INTEGER", nullable: false),
                DurationMs = table.Column<int>(type: "INTEGER", nullable: false),
                Parser = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                CorrelationId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                ToolDetectionConfidence = table.Column<double>(type: "REAL", nullable: false),
                ParseConfidence = table.Column<double>(type: "REAL", nullable: false),
                UnparsedSegmentCount = table.Column<int>(type: "INTEGER", nullable: false),
                Notes = table.Column<string>(type: "TEXT", nullable: true),
                RawContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                RawContent = table.Column<string>(type: "TEXT", nullable: true),
                ContextJson = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Analyses", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ErrorPatterns",
            columns: table => new
            {
                Fingerprint = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                CanonicalMessage = table.Column<string>(type: "TEXT", nullable: false),
                ToolKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                Category = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                FirstSeenAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                LastSeenAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                OccurrenceCount = table.Column<int>(type: "INTEGER", nullable: false),
                LastSuggestedFix = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ErrorPatterns", x => x.Fingerprint);
            });

        migrationBuilder.CreateTable(
            name: "Diagnostics",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                AnalysisId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                ToolKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                Source = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                Code = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                Severity = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                Message = table.Column<string>(type: "TEXT", nullable: false),
                NormalizedMessage = table.Column<string>(type: "TEXT", nullable: false),
                FilePath = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                Line = table.Column<int>(type: "INTEGER", nullable: true),
                Column = table.Column<int>(type: "INTEGER", nullable: true),
                EndLine = table.Column<int>(type: "INTEGER", nullable: true),
                EndColumn = table.Column<int>(type: "INTEGER", nullable: true),
                RawSnippet = table.Column<string>(type: "TEXT", nullable: false),
                Category = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Subcategory = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                IsPrimaryCandidate = table.Column<bool>(type: "INTEGER", nullable: false),
                Fingerprint = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                OrderIndex = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Diagnostics", x => x.Id);
                table.ForeignKey(
                    name: "FK_Diagnostics_Analyses_AnalysisId",
                    column: x => x.AnalysisId,
                    principalTable: "Analyses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DiagnosticGroups",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                AnalysisId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                Fingerprint = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                Count = table.Column<int>(type: "INTEGER", nullable: false),
                GroupReason = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                PrimaryDiagnosticId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                RelatedDiagnosticIdsJson = table.Column<string>(type: "TEXT", nullable: false),
                OrderIndex = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DiagnosticGroups", x => x.Id);
                table.ForeignKey(
                    name: "FK_DiagnosticGroups_Analyses_AnalysisId",
                    column: x => x.AnalysisId,
                    principalTable: "Analyses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PatternOccurrences",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                Fingerprint = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                AnalysisId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                DiagnosticId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                SeenAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PatternOccurrences", x => x.Id);
                table.ForeignKey(
                    name: "FK_PatternOccurrences_Analyses_AnalysisId",
                    column: x => x.AnalysisId,
                    principalTable: "Analyses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_PatternOccurrences_ErrorPatterns_Fingerprint",
                    column: x => x.Fingerprint,
                    principalTable: "ErrorPatterns",
                    principalColumn: "Fingerprint",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Analyses_CreatedAtUtc",
            table: "Analyses",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_Analyses_RawContentHash",
            table: "Analyses",
            column: "RawContentHash");

        migrationBuilder.CreateIndex(
            name: "IX_Analyses_ToolDetected",
            table: "Analyses",
            column: "ToolDetected");

        migrationBuilder.CreateIndex(
            name: "IX_DiagnosticGroups_AnalysisId",
            table: "DiagnosticGroups",
            column: "AnalysisId");

        migrationBuilder.CreateIndex(
            name: "IX_DiagnosticGroups_AnalysisId_OrderIndex",
            table: "DiagnosticGroups",
            columns: new[] { "AnalysisId", "OrderIndex" });

        migrationBuilder.CreateIndex(
            name: "IX_DiagnosticGroups_Fingerprint",
            table: "DiagnosticGroups",
            column: "Fingerprint");

        migrationBuilder.CreateIndex(
            name: "IX_Diagnostics_AnalysisId",
            table: "Diagnostics",
            column: "AnalysisId");

        migrationBuilder.CreateIndex(
            name: "IX_Diagnostics_AnalysisId_OrderIndex",
            table: "Diagnostics",
            columns: new[] { "AnalysisId", "OrderIndex" });

        migrationBuilder.CreateIndex(
            name: "IX_Diagnostics_Fingerprint",
            table: "Diagnostics",
            column: "Fingerprint");

        migrationBuilder.CreateIndex(
            name: "IX_ErrorPatterns_Category",
            table: "ErrorPatterns",
            column: "Category");

        migrationBuilder.CreateIndex(
            name: "IX_ErrorPatterns_LastSeenAtUtc",
            table: "ErrorPatterns",
            column: "LastSeenAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_ErrorPatterns_ToolKind",
            table: "ErrorPatterns",
            column: "ToolKind");

        migrationBuilder.CreateIndex(
            name: "IX_PatternOccurrences_AnalysisId",
            table: "PatternOccurrences",
            column: "AnalysisId");

        migrationBuilder.CreateIndex(
            name: "IX_PatternOccurrences_Fingerprint",
            table: "PatternOccurrences",
            column: "Fingerprint");

        migrationBuilder.CreateIndex(
            name: "IX_PatternOccurrences_SeenAtUtc",
            table: "PatternOccurrences",
            column: "SeenAtUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DiagnosticGroups");
        migrationBuilder.DropTable(name: "Diagnostics");
        migrationBuilder.DropTable(name: "PatternOccurrences");
        migrationBuilder.DropTable(name: "Analyses");
        migrationBuilder.DropTable(name: "ErrorPatterns");
    }
}
