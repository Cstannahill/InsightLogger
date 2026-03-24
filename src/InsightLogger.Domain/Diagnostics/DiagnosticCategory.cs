namespace InsightLogger.Domain.Diagnostics;

public enum DiagnosticCategory
{
    Unknown = 0,
    Syntax = 1,
    MissingSymbol = 2,
    TypeMismatch = 3,
    NullableSafety = 4,
    Dependency = 5,
    Configuration = 6,
    BuildSystem = 7,
    RuntimeEnvironment = 8,
    Serialization = 9,
    TestFailure = 10
}
