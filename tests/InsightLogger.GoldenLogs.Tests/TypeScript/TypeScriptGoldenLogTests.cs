using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InsightLogger.GoldenLogs.Tests.Infrastructure;
using Xunit;

namespace InsightLogger.GoldenLogs.Tests.TypeScript;

public sealed class TypeScriptGoldenLogTests
{
    public static IEnumerable<object[]> Cases()
    {
        var repoRoot = RepositoryPathResolver.FindRepositoryRoot();
        var casesDirectory = Path.Combine(repoRoot, "tests", "InsightLogger.GoldenLogs.Tests", "Cases", "tsc");

        return GoldenLogCaseLoader
            .LoadAll(casesDirectory)
            .Select(testCase => new object[] { testCase });
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task TypeScriptGoldenCase_ShouldMatchExpectedDeterministicOutput(GoldenLogCase testCase)
    {
        var harness = new GoldenLogTestHarness();
        var result = await harness.ExecuteAsync(testCase);

        harness.AssertMatches(testCase, result);
    }
}
