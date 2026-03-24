namespace InsightLogger.Contracts.Common;

public sealed record ValidationErrorDetail(
    string Field,
    string Message);
