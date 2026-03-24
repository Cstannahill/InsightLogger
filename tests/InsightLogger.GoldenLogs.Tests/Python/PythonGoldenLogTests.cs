using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InsightLogger.GoldenLogs.Tests.Infrastructure;
using Xunit;

namespace InsightLogger.GoldenLogs.Tests.Python;

public sealed class PythonGoldenLogTests
{
    public static IEnumerable<object[]> Cases()
    {
        var repoRoot = RepositoryPathResolver.FindRepositoryRoot();
        var casesDirectory = Path.Combine(repoRoot, "tests", "InsightLogger.GoldenLogs.Tests", "Cases", "python");

        return GoldenLogCaseLoader
            .LoadAll(casesDirectory)
            .Select(testCase => new object[] { testCase });
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task PythonGoldenCase_ShouldMatchExpectedDeterministicOutput(GoldenLogCase testCase)
    {
        var harness = new GoldenLogTestHarness();
        var result = await harness.ExecuteAsync(testCase);

        harness.AssertMatches(testCase, result);
    }
}
