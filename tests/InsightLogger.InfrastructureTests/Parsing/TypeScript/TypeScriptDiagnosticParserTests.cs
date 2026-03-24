using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Infrastructure.Parsing.TypeScript;
using Xunit;

namespace InsightLogger.InfrastructureTests.Parsing.TypeScript;

public sealed class TypeScriptDiagnosticParserTests
{
    private readonly TypeScriptDiagnosticParser _parser = new();

    [Fact]
    public async Task Parses_single_tsc_error_with_file_and_coordinates()
    {
        var request = new ParseDiagnosticsRequest(
            Content: "src/app.ts:5:13 - error TS2304: Cannot find name 'usre'.",
            InputType: InputType.SingleDiagnostic,
            ToolHint: ToolKind.TypeScript);

        var result = await _parser.ParseAsync(request);

        result.Diagnostics.Should().ContainSingle();
        var diagnostic = result.Diagnostics.Single();
        diagnostic.ToolKind.Should().Be(ToolKind.TypeScript);
        diagnostic.Code.Should().Be("TS2304");
        diagnostic.Severity.Should().Be(Severity.Error);
        diagnostic.Location!.FilePath.Should().Be("src/app.ts");
        diagnostic.Location.Line.Should().Be(5);
        diagnostic.Location.Column.Should().Be(13);
        diagnostic.Category.Should().Be(DiagnosticCategory.MissingSymbol);
        diagnostic.NormalizedMessage.Should().Be("Cannot find name '{identifier}'.");
        diagnostic.Fingerprint.Should().NotBeNull();
    }

    [Fact]
    public async Task Keeps_codeframe_lines_inside_raw_snippet_but_not_message()
    {
        const string content = """
src/app.ts:5:13 - error TS2304: Cannot find name 'usre'.

5 const value = usre;
              ~~~~
""";

        var result = await _parser.ParseAsync(new ParseDiagnosticsRequest(content, ToolHint: ToolKind.TypeScript));
        var diagnostic = result.Diagnostics.Single();

        diagnostic.Message.Should().Be("Cannot find name 'usre'.");
        diagnostic.RawSnippet.Should().Contain("const value = usre;");
        diagnostic.RawSnippet.Should().Contain("~~~~");
        result.TotalSegments.Should().Be(1);
        result.ParsedSegments.Should().Be(1);
        result.ParseConfidence.Should().Be(1.0);
    }

    [Fact]
    public async Task Classifies_missing_module_as_dependency_issue()
    {
        const string content = "src/main.ts:1:21 - error TS2307: Cannot find module '@/lib/api' or its corresponding type declarations.";

        var result = await _parser.ParseAsync(new ParseDiagnosticsRequest(content, ToolHint: ToolKind.TypeScript));
        var diagnostic = result.Diagnostics.Single();

        diagnostic.Category.Should().Be(DiagnosticCategory.Dependency);
        diagnostic.NormalizedMessage.Should().Be("Cannot find module '{module}' or its corresponding type declarations.");
    }

    [Fact]
    public async Task Normalizes_equivalent_cannot_find_name_errors_to_same_fingerprint()
    {
        const string first = "src/app.ts:5:13 - error TS2304: Cannot find name 'usre'.";
        const string second = "src/app.ts:8:9 - error TS2304: Cannot find name 'uesr'.";

        var firstDiagnostic = (await _parser.ParseAsync(new ParseDiagnosticsRequest(first, ToolHint: ToolKind.TypeScript))).Diagnostics.Single();
        var secondDiagnostic = (await _parser.ParseAsync(new ParseDiagnosticsRequest(second, ToolHint: ToolKind.TypeScript))).Diagnostics.Single();

        firstDiagnostic.NormalizedMessage.Should().Be(secondDiagnostic.NormalizedMessage);
        firstDiagnostic.Fingerprint.Should().Be(secondDiagnostic.Fingerprint);
    }
}
