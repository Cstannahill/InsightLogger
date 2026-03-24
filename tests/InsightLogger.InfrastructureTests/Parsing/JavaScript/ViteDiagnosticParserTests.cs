using System.Threading.Tasks;
using FluentAssertions;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Infrastructure.Parsing.JavaScript;
using Xunit;

namespace InsightLogger.InfrastructureTests.Parsing.JavaScript;

public sealed class ViteDiagnosticParserTests
{
    private readonly ViteDiagnosticParser _parser = new();

    [Fact]
    public async Task ParseAsync_resolve_import_build_log_extracts_dependency_diagnostic()
    {
        const string content = """
vite v5.4.8 building for production...
transforming...
✓ 3 modules transformed.
x Build failed in 112ms
error during build:
[vite]: Rollup failed to resolve import "axiosx" from "/src/main.ts".
This is most likely unintended because it can break your application at runtime.
""";

        var result = await _parser.ParseAsync(new ParseDiagnosticsRequest(content, InputType.BuildLog, ToolKind.Vite, true));

        result.ToolKind.Should().Be(ToolKind.Vite);
        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Code.Should().Be("VITE_RESOLVE_IMPORT");
        result.Diagnostics[0].Category.Should().Be(DiagnosticCategory.Dependency);
        result.Diagnostics[0].Location.Should().NotBeNull();
        result.Diagnostics[0].Location!.FilePath.Should().Be("/src/main.ts");
        result.Diagnostics[0].NormalizedMessage.Should().Be("Rollup failed to resolve import '{import}' from '{file}'.");
    }

    [Fact]
    public async Task ParseAsync_missing_export_build_log_extracts_missing_symbol_diagnostic()
    {
        const string content = """
vite v5.4.8 building for production...
x Build failed in 43ms
error during build:
src/routes/index.ts (7:10): "missingThing" is not exported by "src/lib/api.ts", imported by "src/routes/index.ts".
file: /workspace/app/src/routes/index.ts:7:10
""";

        var result = await _parser.ParseAsync(new ParseDiagnosticsRequest(content, InputType.BuildLog, ToolKind.Vite, true));

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Code.Should().Be("VITE_MISSING_EXPORT");
        result.Diagnostics[0].Category.Should().Be(DiagnosticCategory.MissingSymbol);
        result.Diagnostics[0].Location!.FilePath.Should().Be("src/routes/index.ts");
        result.Diagnostics[0].Location.Line.Should().Be(7);
        result.Diagnostics[0].Location.Column.Should().Be(10);
    }
}
