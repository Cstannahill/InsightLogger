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
                SuggestedFixes: new[]
                {
                    "Verify the import path points to a real file or package.",
                    "Check tsconfig path aliases and module resolution settings.",
                    "Install or add the missing package or type declarations if needed."
                });
        }


        if (diagnostic.Code == "NameError")
        {
            return new DiagnosticInsight(
                Title: "Unknown Python name in current scope",
                Explanation: "Python reached runtime with a referenced name that was never defined in the active scope.",
                SuggestedFixes: new[]
                {
                    "Check the variable or function name for a typo.",
                    "Verify the symbol is defined before this line runs.",
                    "Confirm the value is imported into this module or passed into the current scope."
                });
        }

        if (diagnostic.Code == "ModuleNotFoundError" || diagnostic.Code == "ImportError")
        {
            return new DiagnosticInsight(
                Title: "Missing Python dependency or import target",
                Explanation: "Python could not resolve the requested module or imported symbol at runtime.",
                SuggestedFixes: new[]
                {
                    "Verify the package is installed in the active environment.",
                    "Check the module path or symbol name in the import statement.",
                    "Confirm the app is running with the expected interpreter and virtual environment."
                });
        }

        if (diagnostic.Code == "SyntaxError" || diagnostic.Code == "IndentationError" || diagnostic.Code == "TabError")
        {
            return new DiagnosticInsight(
                Title: "Invalid Python syntax",
                Explanation: "Python could not parse the source file because the syntax or indentation is invalid.",
                SuggestedFixes: new[]
                {
                    "Inspect the reported line and the line just before it for a missing token.",
                    "Fix indentation to use a consistent block structure.",
                    "Re-run after correcting the first syntax error before chasing downstream failures."
                });
        }

        if (diagnostic.Code == "VITE_RESOLVE_IMPORT")
        {
            return new DiagnosticInsight(
                Title: "Unresolved import in Vite build",
                Explanation: "Vite and Rollup could not resolve an imported module path during bundling.",
                SuggestedFixes: new[]
                {
                    "Verify the import path or package name is spelled correctly.",
                    "Confirm the referenced file or dependency actually exists.",
                    "Check alias and resolve configuration if the path depends on Vite config."
                });
        }

        if (diagnostic.Code == "VITE_MISSING_EXPORT")
        {
            return new DiagnosticInsight(
                Title: "Imported symbol is not exported",
                Explanation: "The bundle references a named export that the target module does not provide.",
                SuggestedFixes: new[]
                {
                    "Verify the imported symbol name matches a real export.",
                    "Switch between default and named import syntax if needed.",
                    "Confirm the import points at the intended module file."
                });
        }

        if (diagnostic.Code == "NPM_MISSING_SCRIPT")
        {
            return new DiagnosticInsight(
                Title: "Missing npm script",
                Explanation: "npm was asked to run a script that is not defined in package.json.",
                SuggestedFixes: new[]
                {
                    "Add the missing script under package.json scripts.",
                    "Verify you ran the intended script name.",
                    "Check that you are in the correct package or workspace before running the command."
                });
        }

        if (diagnostic.Code == "ERESOLVE")
        {
            return new DiagnosticInsight(
                Title: "npm dependency resolution conflict",
                Explanation: "npm could not build a compatible dependency tree from the current package constraints.",
                SuggestedFixes: new[]
                {
                    "Inspect peer dependency requirements in the failing packages.",
                    "Align package versions so they satisfy the same dependency range.",
                    "Retry with a clean lockfile only after understanding which versions actually conflict."
                });
        }

        return diagnostic.Category switch
        {
            DiagnosticCategory.MissingSymbol => new DiagnosticInsight(
                Title: "Missing or unresolved symbol",
                Explanation: "A referenced symbol could not be resolved by the tool in the current context.",
                SuggestedFixes: new[]
                {
                    "Check the spelling of the referenced symbol.",
                    "Verify the symbol is declared or imported.",
                    "Confirm the symbol is available in the current scope or project."
                }),
            DiagnosticCategory.NullableSafety => new DiagnosticInsight(
                Title: "Nullable safety issue",
                Explanation: "The compiler found a nullable-reference-safety issue that may lead to invalid null handling.",
                SuggestedFixes: new[]
                {
                    "Initialize the member before constructor exit.",
                    "Mark the member nullable if null is expected.",
                    "Use required/init patterns if they fit the design."
                }),
            DiagnosticCategory.Dependency => new DiagnosticInsight(
                Title: "Dependency or reference problem",
                Explanation: "The build depends on a missing or unresolved package, assembly, or referenced type.",
                SuggestedFixes: new[]
                {
                    "Verify package and project references.",
                    "Check namespaces and imports/usings.",
                    "Confirm restore/build ran against the expected solution state."
                }),
            DiagnosticCategory.BuildSystem => new DiagnosticInsight(
                Title: "Build system failure",
                Explanation: "The toolchain failed at the build-system layer rather than in user code semantics.",
                SuggestedFixes: new[]
                {
                    "Read the build-system message for locked files or invalid targets.",
                    "Close any process using the locked output if file access is blocked.",
                    "Rebuild after clearing stale build artifacts if necessary."
                }),
            _ => new DiagnosticInsight(
                Title: "Build diagnostic detected",
                Explanation: "The system extracted a structured diagnostic, but no specialized explanation has been added yet.",
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
        IReadOnlyList<string> SuggestedFixes);
}
