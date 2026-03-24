using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace InsightLogger.Application.Abstractions.Parsing;

public interface IDiagnosticParserCoordinator
{
    Task<DiagnosticParserCoordinatorResult> ParseAsync(string content, InputType inputType, ToolKind detectedTool, string? correlationId = null, CancellationToken cancellationToken = default);
}
