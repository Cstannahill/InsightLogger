using System.Threading.Tasks;
using FluentAssertions;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Infrastructure.Parsing.JavaScript;
using Xunit;

namespace InsightLogger.InfrastructureTests.Parsing.JavaScript;

public sealed class NpmDiagnosticParserTests
{
    private readonly NpmDiagnosticParser _parser = new();

    [Fact]
    public async Task ParseAsync_missing_script_build_log_extracts_configuration_diagnostic()
    {
        const string content = """
npm ERR! Missing script: "build"
npm ERR!
npm ERR! To see a list of scripts, run:
npm ERR!   npm run
""";

        var result = await _parser.ParseAsync(new ParseDiagnosticsRequest(content, InputType.BuildLog, ToolKind.Npm, true));

        result.ToolKind.Should().Be(ToolKind.Npm);
        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Code.Should().Be("NPM_MISSING_SCRIPT");
        result.Diagnostics[0].Category.Should().Be(DiagnosticCategory.Configuration);
        result.Diagnostics[0].NormalizedMessage.Should().Be("Missing script '{script}'.");
    }

    [Fact]
    public async Task ParseAsync_dependency_resolution_log_extracts_dependency_diagnostic()
    {
        const string content = """
npm ERR! code ERESOLVE
npm ERR! ERESOLVE unable to resolve dependency tree
npm ERR! While resolving: sample-app@1.0.0
npm ERR! Found: react@19.0.0
npm ERR! Could not resolve dependency:
npm ERR! peer react@"^18.0.0" from legacy-widget@2.1.0
""";

        var result = await _parser.ParseAsync(new ParseDiagnosticsRequest(content, InputType.BuildLog, ToolKind.Npm, true));

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Code.Should().Be("ERESOLVE");
        result.Diagnostics[0].Category.Should().Be(DiagnosticCategory.Dependency);
        result.Diagnostics[0].NormalizedMessage.Should().Be("unable to resolve dependency tree");
    }
}
