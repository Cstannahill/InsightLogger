using InsightLogger.Api.Validation;
using InsightLogger.Contracts.Analyses;
using Xunit;

namespace InsightLogger.ApiTests.Validation;

public sealed class AnalysisRequestValidatorTests
{
    [Fact]
    public void Validate_ShouldRejectEmptyContent()
    {
        var request = new AnalyzeBuildLogRequest(
            Tool: "dotnet",
            Content: "  ");

        var errors = AnalysisRequestValidator.Validate(request);

        Assert.Contains(errors, e => e.Field == "content");
    }

    [Fact]
    public void Validate_ShouldRejectUnknownTool()
    {
        var request = new AnalyzeCompilerErrorRequest(
            Tool: "mystery-tool",
            Content: "something broke");

        var errors = AnalysisRequestValidator.Validate(request);

        Assert.Contains(errors, e => e.Field == "tool");
    }

    [Fact]
    public void Validate_ShouldAllowKnownToolAndContent()
    {
        var request = new AnalyzeCompilerErrorRequest(
            Tool: "dotnet",
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context");

        var errors = AnalysisRequestValidator.Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldRejectPersistRawContent_WhenPersistIsFalse()
    {
        var request = new AnalyzeBuildLogRequest(
            Tool: "dotnet",
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            Options: new AnalyzeRequestOptionsContract(
                Persist: false,
                PersistRawContent: true));

        var errors = AnalysisRequestValidator.Validate(request);

        Assert.Contains(errors, e => e.Field == "options.persistRawContent");
    }
}
