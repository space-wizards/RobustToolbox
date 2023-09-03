using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using static Robust.Analyzers.Diagnostics;

namespace Robust.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class DependencyFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IdDependencyNotPartial);

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            switch (diagnostic.Id)
            {
                case IdDependencyNotPartial:
                    return DefinitionFixer.RegisterPartialTypeFix(context, diagnostic);
            }
        }

        return Task.CompletedTask;
    }
}
