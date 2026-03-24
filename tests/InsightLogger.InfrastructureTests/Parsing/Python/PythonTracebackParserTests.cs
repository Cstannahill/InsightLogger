using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Infrastructure.Parsing.Python;
using Xunit;

namespace InsightLogger.InfrastructureTests.Parsing.Python;

public sealed class PythonTracebackParserTests
{
    private readonly PythonTracebackParser _parser = new();

    [Fact]
    public async Task Parses_name_error_traceback_and_uses_terminal_frame_location()
    {
        const string content = """
Traceback (most recent call last):
  File "src/main.py", line 8, in <module>
    run()
  File "src/main.py", line 5, in run
    print(usre_name)
NameError: name 'usre_name' is not defined
""";

        var result = await _parser.ParseAsync(new ParseDiagnosticsRequest(content, InputType.BuildLog, ToolKind.Python));
        var diagnostic = result.Diagnostics.Single();

        diagnostic.ToolKind.Should().Be(ToolKind.Python);
        diagnostic.Code.Should().Be("NameError");
        diagnostic.Category.Should().Be(DiagnosticCategory.MissingSymbol);
        diagnostic.Location!.FilePath.Should().Be("src/main.py");
        diagnostic.Location.Line.Should().Be(5);
        diagnostic.Message.Should().Be("name 'usre_name' is not defined");
        diagnostic.NormalizedMessage.Should().Be("name '{identifier}' is not defined");
        diagnostic.RawSnippet.Should().Contain("print(usre_name)");
        diagnostic.Metadata["frameCount"].Should().Be("2");
    }

    [Fact]
    public async Task Parses_module_not_found_as_dependency_issue()
    {
        const string content = """
Traceback (most recent call last):
  File "app.py", line 1, in <module>
    import requestsx
ModuleNotFoundError: No module named 'requestsx'
""";

        var result = await _parser.ParseAsync(new ParseDiagnosticsRequest(content, InputType.BuildLog, ToolKind.Python));
        var diagnostic = result.Diagnostics.Single();

        diagnostic.Code.Should().Be("ModuleNotFoundError");
        diagnostic.Category.Should().Be(DiagnosticCategory.Dependency);
        diagnostic.NormalizedMessage.Should().Be("No module named '{module}'");
    }

    [Fact]
    public async Task Extracts_syntax_error_column_from_caret_line()
    {
        const string content = """
Traceback (most recent call last):
  File "script.py", line 1
    if True print("hello")
            ^^^^^
SyntaxError: invalid syntax
""";

        var result = await _parser.ParseAsync(new ParseDiagnosticsRequest(content, InputType.SingleDiagnostic, ToolKind.Python));
        var diagnostic = result.Diagnostics.Single();

        diagnostic.Code.Should().Be("SyntaxError");
        diagnostic.Category.Should().Be(DiagnosticCategory.Syntax);
        diagnostic.Location!.FilePath.Should().Be("script.py");
        diagnostic.Location.Line.Should().Be(1);
        diagnostic.Location.Column.Should().Be(13);
    }

    [Fact]
    public async Task Normalizes_equivalent_name_errors_to_same_fingerprint()
    {
        const string first = """
Traceback (most recent call last):
  File "src/main.py", line 3, in <module>
    print(usre_name)
NameError: name 'usre_name' is not defined
""";

        const string second = """
Traceback (most recent call last):
  File "src/main.py", line 9, in <module>
    print(usern_ame)
NameError: name 'usern_ame' is not defined
""";

        var firstDiagnostic = (await _parser.ParseAsync(new ParseDiagnosticsRequest(first, InputType.BuildLog, ToolKind.Python))).Diagnostics.Single();
        var secondDiagnostic = (await _parser.ParseAsync(new ParseDiagnosticsRequest(second, InputType.BuildLog, ToolKind.Python))).Diagnostics.Single();

        firstDiagnostic.NormalizedMessage.Should().Be(secondDiagnostic.NormalizedMessage);
        firstDiagnostic.Fingerprint.Should().Be(secondDiagnostic.Fingerprint);
    }
}
