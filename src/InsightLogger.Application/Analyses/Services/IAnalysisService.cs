using InsightLogger.Application.Analyses.Commands;
using InsightLogger.Domain.Analyses;
using System.Threading;
using System.Threading.Tasks;

namespace InsightLogger.Application.Analyses.Services;

public interface IAnalysisService
{
    Task<AnalysisResult> AnalyzeAsync(AnalyzeInputCommand command, CancellationToken cancellationToken = default);
}
