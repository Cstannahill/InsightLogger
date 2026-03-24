using System.Collections.Generic;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Analyses.Services;

internal static class DiagnosticInsightFactory
{
    public static DiagnosticInsight Create(DiagnosticRecord diagnostic)
    {
        if (diagnostic.Code == "CS0103")
        {
            return new DiagnosticInsight(
                Title: "Unknown symbol in current context",
                Explanation: "The compiler cannot resolve a referenced name in the current scope.",
                LikelyCauses: new[]
                {
                    "Typo in variable or member name",
                    "Missing declaration",
                    "Wrong scope or missing using/reference"
                },
                SuggestedFixes: new[]
                {
                    "Check the symbol name for a typo.",
                    "Verify the symbol is declared before use.",
                    "Confirm the intended variable or member is available in this scope."
                });
        }

        if (diagnostic.Code == "TS2304")
        {
            return new DiagnosticInsight(
                Title: "Unknown identifier in TypeScript context",
                Explanation: "TypeScript cannot resolve a referenced identifier in the current file or scope.",
                LikelyCauses: new[]
                {
                    "Typo in identifier name",
                    "Variable not declared",
                    "Import missing or incorrect"
                },
                SuggestedFixes: new[]
                {
                    "Check the identifier spelling for a typo.",
                    "Verify the symbol is declared before use.",
                    "Confirm the symbol is imported or exported from the expected module."
                });
        }

        if (diagnostic.Code == "TS2307")
        {
            return new DiagnosticInsight(
                Title: "Missing TypeScript module or declarations",
                Explanation: "TypeScript could not resolve the referenced module path or its type declarations.",
                LikelyCauses: new[]
                {
                    "Import path points to the wrong file",
                    "Package or local module is missing",
                    "Type declarations are unavailable for the imported package"
                },
                SuggestedFixes: new[]
                {
                    "Verify the import path points to a real file or package.",
                    "Check tsconfig path aliases and module resolution settings.",
                    "Install or add the missing package or type declarations if needed."
                });
        }

        return diagnostic.Category switch
        {
            DiagnosticCategory.MissingSymbol => new DiagnosticInsight(
                Title: "Missing or unresolved symbol",
                Explanation: "A referenced symbol could not be resolved by the tool in the current context.",
                LikelyCauses: new[]
                {
                    "Typo in referenced symbol",
                    "Missing declaration or import",
                    "Wrong scope or unavailable project reference"
                },
                SuggestedFixes: new[]
                {
                    "Check the spelling of the referenced symbol.",
                    "Verify the symbol is declared or imported.",
                    "Confirm the symbol is available in the current scope or project."
                }),
            DiagnosticCategory.NullableSafety => new DiagnosticInsight(
                Title: "Nullable safety issue",
                Explanation: "The compiler found a nullable-reference-safety issue that may lead to invalid null handling.",
                LikelyCauses: new[]
                {
                    "Member not initialized before constructor exit",
                    "Nullability annotation does not match runtime behavior",
                    "Required/init pattern is missing where the type expects it"
                },
                SuggestedFixes: new[]
                {
                    "Initialize the member before constructor exit.",
                    "Mark the member nullable if null is expected.",
                    "Use required/init patterns if they fit the design."
                }),
            DiagnosticCategory.Dependency => new DiagnosticInsight(
                Title: "Dependency or reference problem",
                Explanation: "The build depends on a missing or unresolved package, assembly, or referenced type.",
                LikelyCauses: new[]
                {
                    "Package, assembly, or project reference is missing",
                    "Namespace or import is incorrect",
                    "Restore/build state is out of sync"
                },
                SuggestedFixes: new[]
                {
                    "Verify package and project references.",
                    "Check namespaces and imports/usings.",
                    "Confirm restore/build ran against the expected solution state."
                }),
            DiagnosticCategory.BuildSystem => new DiagnosticInsight(
                Title: "Build system failure",
                Explanation: "The toolchain failed at the build-system layer rather than in user code semantics.",
                LikelyCauses: new[]
                {
                    "Locked file or output path issue",
                    "Bad build target or configuration",
                    "Stale build artifacts are interfering with the current build"
                },
                SuggestedFixes: new[]
                {
                    "Read the build-system message for locked files or invalid targets.",
                    "Close any process using the locked output if file access is blocked.",
                    "Rebuild after clearing stale build artifacts if necessary."
                }),
            _ => new DiagnosticInsight(
                Title: "Build diagnostic detected",
                Explanation: "The system extracted a structured diagnostic, but no specialized explanation has been added yet.",
                LikelyCauses: new[]
                {
                    "The earliest high-severity diagnostic is the most likely starting point",
                    "A configuration or code issue may be causing downstream noise",
                    "This pattern needs a specialized explainer once it repeats"
                },
                SuggestedFixes: new[]
                {
                    "Inspect the diagnostic code and file location first.",
                    "Fix the earliest high-severity diagnostic before downstream noise.",
                    "Add a specialized rule or explainer once this pattern repeats."
                })
        };
    }

    internal sealed record DiagnosticInsight(
        string Title,
        string Explanation,
        IReadOnlyList<string> LikelyCauses,
        IReadOnlyList<string> SuggestedFixes);
}
