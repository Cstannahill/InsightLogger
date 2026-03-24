using InsightLogger.Domain.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace InsightLogger.Application.Abstractions.Parsing;

public interface IToolDetector
{
    Task<ToolDetectionResult> DetectAsync(string content, ToolKind? explicitHint = null, CancellationToken cancellationToken = default);
}
