using System;
using System.IO;

namespace InsightLogger.IntegrationTests.Infrastructure;

internal static class RepositoryPathResolver
{
    public static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "InsightLogger.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the InsightLogger repository root from the test output directory.");
    }
}
