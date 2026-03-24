using System;
using System.IO;

namespace InsightLogger.ApiTests.Infrastructure;

public static class RepositoryPathResolver
{
    public static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var solutionPath = Path.Combine(current.FullName, "InsightLogger.sln");
            var propsPath = Path.Combine(current.FullName, "Directory.Build.props");

            if (File.Exists(solutionPath) || File.Exists(propsPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the current test base directory.");
    }
}
