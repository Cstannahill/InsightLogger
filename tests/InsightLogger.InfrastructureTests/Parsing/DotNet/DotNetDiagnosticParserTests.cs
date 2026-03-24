using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Infrastructure.Parsing.DotNet;
using Xunit;

namespace InsightLogger.InfrastructureTests.Parsing.DotNet;

public sealed class DotNetDiagnosticParserTests
{
    private readonly DotNetDiagnosticParser _parser = new();

    [Fact]
    public async Task Parses_roslyn_compiler_error_with_file_and_coordinates()
    {
        var request = new ParseDiagnosticsRequest(
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            InputType: InputType.SingleDiagnostic,
            ToolHint: ToolKind.DotNet);

        var result = await _parser.ParseAsync(request);

        result.Diagnostics.Should().ContainSingle();
        var diagnostic = result.Diagnostics.Single();
        diagnostic.ToolKind.Should().Be(ToolKind.DotNet);
        diagnostic.Code.Should().Be("CS0103");
        diagnostic.Severity.Should().Be(Severity.Error);
        diagnostic.Location!.FilePath.Should().Be("Program.cs");
        diagnostic.Location.Line.Should().Be(14);
        diagnostic.Location.Column.Should().Be(9);
        diagnostic.Category.Should().Be(DiagnosticCategory.MissingSymbol);
        diagnostic.NormalizedMessage.Should().Be("The name '{identifier}' does not exist in the current context");
        diagnostic.Fingerprint.Should().NotBeNull();
    }

    [Fact]
    public async Task Parses_warning_with_range_and_project_metadata()
    {
        const string content = @"C:\src\InsightLogger\User.cs(8,19,8,23): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [C:\src\InsightLogger\InsightLogger.csproj]";

        var result = await _parser.ParseAsync(new ParseDiagnosticsRequest(content, ToolHint: ToolKind.DotNet));
        var diagnostic = result.Diagnostics.Single();

        diagnostic.Severity.Should().Be(Severity.Warning);
        diagnostic.Category.Should().Be(DiagnosticCategory.NullableSafety);
        diagnostic.Location!.FilePath.Should().Be("C:/src/InsightLogger/User.cs");
        diagnostic.Location.EndLine.Should().Be(8);
        diagnostic.Location.EndColumn.Should().Be(23);
        diagnostic.Metadata.Should().ContainKey("project");
        diagnostic.Metadata["project"].Should().Be("C:/src/InsightLogger/InsightLogger.csproj");
    }

    [Fact]
    public async Task Parses_msbuild_error_without_source_file()
    {
        const string content = "error MSB3021: Unable to copy file \"obj\\Debug\\net9.0\\apphost.exe\" to \"bin\\Debug\\net9.0\\MyApp.exe\". The process cannot access the file because it is being used by another process.";

        var result = await _parser.ParseAsync(new ParseDiagnosticsRequest(content, ToolHint: ToolKind.DotNet));
        var diagnostic = result.Diagnostics.Single();

        diagnostic.Source.Should().Be("MSBuild");
        diagnostic.Category.Should().Be(DiagnosticCategory.BuildSystem);
        diagnostic.Location.Should().BeNull();
    }

    [Fact]
    public async Task Normalizes_equivalent_missing_name_errors_to_same_fingerprint()
    {
        const string first = "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context";
        const string second = "Program.cs(42,13): error CS0103: The name 'servicez' does not exist in the current context";

        var firstDiagnostic = (await _parser.ParseAsync(new ParseDiagnosticsRequest(first, ToolHint: ToolKind.DotNet))).Diagnostics.Single();
        var secondDiagnostic = (await _parser.ParseAsync(new ParseDiagnosticsRequest(second, ToolHint: ToolKind.DotNet))).Diagnostics.Single();

        firstDiagnostic.NormalizedMessage.Should().Be(secondDiagnostic.NormalizedMessage);
        firstDiagnostic.Fingerprint.Should().Be(secondDiagnostic.Fingerprint);
    }

    [Fact]
    public async Task Ignores_summary_noise_but_keeps_multiple_diagnostics()
    {
        const string content = """
Build started...
Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context
Models/User.cs(8,19): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Build FAILED.
    1 Warning(s)
    1 Error(s)
Time Elapsed 00:00:01.42
""";

        var result = await _parser.ParseAsync(new ParseDiagnosticsRequest(content, ToolHint: ToolKind.DotNet));

        result.Diagnostics.Should().HaveCount(2);
        result.TotalSegments.Should().Be(2);
        result.ParsedSegments.Should().Be(2);
        result.ParseConfidence.Should().Be(1.0);
    }
}
