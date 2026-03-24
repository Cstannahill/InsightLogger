using InsightLogger.Domain.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace InsightLogger.Application.Abstractions.Parsing;

public interface IDiagnosticParser
{
    string Name { get; }
    ToolKind ToolKind { get; }
    bool CanHandle(ParseDiagnosticsRequest request);
    Task<ParseDiagnosticsResult> ParseAsync(ParseDiagnosticsRequest request, CancellationToken cancellationToken = default);
}
