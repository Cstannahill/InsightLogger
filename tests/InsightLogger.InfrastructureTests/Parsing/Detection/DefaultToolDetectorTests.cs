using System.Threading.Tasks;
using FluentAssertions;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Infrastructure.Parsing.Detection;
using Xunit;

namespace InsightLogger.InfrastructureTests.Parsing.Detection;

public sealed class DefaultToolDetectorTests
{
    private readonly DefaultToolDetector _detector = new();

    [Fact]
    public async Task Returns_explicit_hint_when_provided()
    {
        var result = await _detector.DetectAsync("anything at all", ToolKind.DotNet);

        result.ToolKind.Should().Be(ToolKind.DotNet);
        result.Confidence.Should().Be(1.0);
        result.WasExplicitHint.Should().BeTrue();
    }

    [Fact]
    public async Task Detects_dotnet_from_compiler_codes()
    {
        var result = await _detector.DetectAsync("Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context");

        result.ToolKind.Should().Be(ToolKind.DotNet);
        result.Confidence.Should().BeGreaterThan(0.9);
    }


    [Fact]
    public async Task Detects_typescript_from_tsc_error_code()
    {
        var result = await _detector.DetectAsync("src/app.ts:5:13 - error TS2304: Cannot find name 'usre'.");

        result.ToolKind.Should().Be(ToolKind.TypeScript);
        result.Confidence.Should().BeGreaterThan(0.9);
    }



    [Fact]
    public async Task Detects_python_from_traceback_header()
    {
        var result = await _detector.DetectAsync("""
Traceback (most recent call last):
  File "app.py", line 1, in <module>
NameError: name 'x' is not defined
""");

        result.ToolKind.Should().Be(ToolKind.Python);
        result.Confidence.Should().BeGreaterThan(0.9);
    }

    [Fact]
    public async Task Detects_vite_before_npm_when_wrapped_build_log_contains_both_signatures()
    {
        var result = await _detector.DetectAsync("""
vite v5.4.8 building for production...
error during build:
[vite]: Rollup failed to resolve import "axiosx" from "/src/main.ts".
npm ERR! code ELIFECYCLE
npm ERR! command failed
""");

        result.ToolKind.Should().Be(ToolKind.Vite);
        result.Confidence.Should().BeGreaterThan(0.9);
    }

    [Fact]
    public async Task Detects_npm_from_missing_script_output()
    {
        var result = await _detector.DetectAsync("npm ERR! Missing script: \"build\"");

        result.ToolKind.Should().Be(ToolKind.Npm);
        result.Confidence.Should().BeGreaterThan(0.8);
    }

    [Fact]
    public async Task Returns_unknown_when_no_pattern_matches()
    {
        var result = await _detector.DetectAsync("this is just some random text without a known tool signature");

        result.ToolKind.Should().Be(ToolKind.Unknown);
        result.Confidence.Should().BeLessThan(0.1);
    }
}
