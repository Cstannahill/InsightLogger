using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Knowledge;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Infrastructure.Knowledge;

public sealed class OfficialDocumentationKnowledgeReferenceSource : IKnowledgeReferenceSource
{
    public Task<IReadOnlyList<KnowledgeReference>> GetReferencesAsync(
        KnowledgeReferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var references = new List<KnowledgeReference>();
        var codes = request.DiagnosticCodes;

        foreach (var code in codes)
        {
            var item = BuildCodeReference(request.ToolKind, code);
            if (item is not null)
            {
                references.Add(item);
            }
        }

        if (references.Count == 0)
        {
            foreach (var category in request.Categories)
            {
                var item = BuildCategoryFallback(request.ToolKind, category);
                if (item is not null)
                {
                    references.Add(item);
                }
            }
        }

        return Task.FromResult<IReadOnlyList<KnowledgeReference>>(
            references
                .GroupBy(static item => item.Id, StringComparer.OrdinalIgnoreCase)
                .Select(static group => group.First())
                .Take(4)
                .ToArray());
    }

    private static KnowledgeReference? BuildCodeReference(ToolKind toolKind, string code)
    {
        var normalized = code.Trim().ToUpperInvariant();

        return toolKind switch
        {
            ToolKind.DotNet when normalized == "CS0103" => Create(
                id: "official:dotnet:CS0103",
                title: "Compiler Error CS0103",
                summary: "Microsoft Learn reference for unresolved names in the current C# scope.",
                url: "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0103",
                tags: ["dotnet", "csharp", "CS0103"]),
            ToolKind.DotNet when normalized == "CS0246" => Create(
                id: "official:dotnet:CS0246",
                title: "Resolve missing assembly and namespace references",
                summary: "Microsoft Learn guidance covering missing type or namespace reference errors including CS0246.",
                url: "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/assembly-references",
                tags: ["dotnet", "csharp", "CS0246"]),
            ToolKind.DotNet when normalized is "CS8602" or "CS8618" => Create(
                id: $"official:dotnet:{normalized}",
                title: "Resolve nullable warnings",
                summary: "Microsoft Learn guidance for nullable-reference warnings including CS8602 and CS8618.",
                url: "https://learn.microsoft.com/en-us/dotnet/csharp/nullable-warnings",
                tags: ["dotnet", "csharp", normalized, "nullable"]),
            ToolKind.TypeScript when normalized == "TS2307" => Create(
                id: "official:typescript:TS2307",
                title: "TypeScript module resolution",
                summary: "Official TypeScript module resolution documentation for unresolved imports and module path issues.",
                url: "https://www.typescriptlang.org/docs/handbook/module-resolution.html",
                tags: ["typescript", "TS2307", "module-resolution"]),
            ToolKind.TypeScript when normalized == "TS2322" => Create(
                id: "official:typescript:TS2322",
                title: "TypeScript type compatibility",
                summary: "Official TypeScript handbook guidance for incompatible assignment and structural type issues.",
                url: "https://www.typescriptlang.org/docs/handbook/type-compatibility.html",
                tags: ["typescript", "TS2322", "type-compatibility"]),
            ToolKind.TypeScript when normalized == "TS2304" => Create(
                id: "official:typescript:TS2304",
                title: "TypeScript modules and symbol resolution",
                summary: "Official TypeScript handbook guidance for finding unresolved names and import resolution problems.",
                url: "https://www.typescriptlang.org/docs/handbook/2/modules.html",
                tags: ["typescript", "TS2304", "modules"]),
            ToolKind.Npm when normalized == "NPM_MISSING_SCRIPT" => Create(
                id: "official:npm:missing-script",
                title: "npm package.json scripts",
                summary: "Official npm documentation for defining and running package.json scripts.",
                url: "https://docs.npmjs.com/cli/v11/configuring-npm/package-json",
                tags: ["npm", "scripts", "package.json"]),
            ToolKind.Vite when normalized == "VITE_RESOLVE_IMPORT" => Create(
                id: "official:vite:resolve-import",
                title: "Vite resolve.alias shared options",
                summary: "Official Vite configuration reference for alias and import-resolution behavior.",
                url: "https://vite.dev/config/shared-options",
                tags: ["vite", "imports", "resolve.alias"]),
            ToolKind.Python when normalized is "NAMEERROR" or "UNBOUNDLOCALERROR" or "MODULENOTFOUNDERROR" or "IMPORTERROR" => Create(
                id: $"official:python:{normalized}",
                title: "Python built-in exceptions",
                summary: "Official Python exception reference covering common import and name-resolution errors.",
                url: "https://docs.python.org/3.12/library/exceptions.html",
                tags: ["python", normalized]),
            _ => null
        };
    }

    private static KnowledgeReference? BuildCategoryFallback(ToolKind toolKind, DiagnosticCategory category)
        => (toolKind, category) switch
        {
            (ToolKind.DotNet, DiagnosticCategory.Dependency) => Create(
                id: "official:dotnet:dependency",
                title: "Resolve missing assembly and namespace references",
                summary: "Microsoft Learn guidance for missing assembly, package, and namespace reference issues.",
                url: "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/assembly-references",
                tags: ["dotnet", "dependency"]),
            (ToolKind.DotNet, DiagnosticCategory.NullableSafety) => Create(
                id: "official:dotnet:nullable",
                title: "Resolve nullable warnings",
                summary: "Microsoft Learn guidance for C# nullable-reference analysis warnings.",
                url: "https://learn.microsoft.com/en-us/dotnet/csharp/nullable-warnings",
                tags: ["dotnet", "nullable"]),
            (ToolKind.TypeScript, DiagnosticCategory.Dependency) => Create(
                id: "official:typescript:dependency",
                title: "TypeScript module resolution",
                summary: "Official TypeScript documentation for resolving module lookup and import path issues.",
                url: "https://www.typescriptlang.org/docs/handbook/module-resolution.html",
                tags: ["typescript", "dependency"]),
            (ToolKind.TypeScript, DiagnosticCategory.TypeMismatch) => Create(
                id: "official:typescript:type-mismatch",
                title: "TypeScript type compatibility",
                summary: "Official TypeScript documentation for structural compatibility and assignment issues.",
                url: "https://www.typescriptlang.org/docs/handbook/type-compatibility.html",
                tags: ["typescript", "type-mismatch"]),
            (ToolKind.Npm, DiagnosticCategory.Configuration) => Create(
                id: "official:npm:configuration",
                title: "npm package.json scripts",
                summary: "Official npm guidance for script configuration and package.json behavior.",
                url: "https://docs.npmjs.com/cli/v11/configuring-npm/package-json",
                tags: ["npm", "configuration"]),
            (ToolKind.Vite, DiagnosticCategory.Dependency) => Create(
                id: "official:vite:dependency",
                title: "Vite shared resolve options",
                summary: "Official Vite configuration reference for aliasing and dependency resolution behavior.",
                url: "https://vite.dev/config/shared-options",
                tags: ["vite", "dependency"]),
            (ToolKind.Python, DiagnosticCategory.MissingSymbol) => Create(
                id: "official:python:missing-symbol",
                title: "Python built-in exceptions",
                summary: "Official Python exception reference for name and import lookup failures.",
                url: "https://docs.python.org/3.12/library/exceptions.html",
                tags: ["python", "missing-symbol"]),
            _ => null
        };

    private static KnowledgeReference Create(string id, string title, string summary, string url, IReadOnlyList<string> tags)
        => new(
            id: id,
            kind: "official-doc",
            source: "official",
            title: title,
            summary: summary,
            url: url,
            tags: tags);
}
