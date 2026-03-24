using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace InsightLogger.GoldenLogs.Tests.Infrastructure;

public static class GoldenLogCaseLoader
{
    public static IReadOnlyList<GoldenLogCase> LoadAll(string casesDirectory)
    {
        var files = Directory
            .EnumerateFiles(casesDirectory, "*.case.json", SearchOption.AllDirectories)
            .OrderBy(path => path)
            .ToArray();

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        return files
            .Select(path => JsonSerializer.Deserialize<GoldenLogCase>(File.ReadAllText(path), options)!)
            .ToArray();
    }
}
