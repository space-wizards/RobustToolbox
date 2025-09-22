using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Roslyn.Shared.Helpers;

namespace Robust.Roslyn.Shared;

#nullable enable

/// <summary>
/// All the information to make a partial type alternative for a type.
/// </summary>
public sealed record PartialTypeInfo(
    string? Namespace,
    string Name,
    string DisplayName,
    EquatableArray<string> TypeParameterNames,
    bool IsValid,
    Location SyntaxLocation,
    Accessibility Accessibility,
    TypeKind Kind,
    bool IsRecord,
    bool IsAbstract)
{
    public static PartialTypeInfo FromSymbol(INamedTypeSymbol symbol, TypeDeclarationSyntax syntax)
    {
        var typeParameters = ImmutableArray<string>.Empty;
        if (symbol.TypeParameters.Length > 0)
        {
            var builder = ImmutableArray.CreateBuilder<string>(symbol.TypeParameters.Length);
            foreach (var typeParameter in symbol.TypeParameters)
            {
                builder.Add(typeParameter.Name);
            }

            typeParameters = builder.MoveToImmutable();
        }

        return new PartialTypeInfo(
            symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString(),
            symbol.Name,
            symbol.ToDisplayString(),
            typeParameters,
            syntax.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)),
            syntax.Keyword.GetLocation(),
            symbol.DeclaredAccessibility,
            symbol.TypeKind,
            symbol.IsRecord,
            symbol.IsAbstract);
    }

    public bool CheckPartialDiagnostic(SourceProductionContext context, DiagnosticDescriptor diagnostic)
    {
        if (!IsValid)
        {
            context.ReportDiagnostic(Diagnostic.Create(diagnostic, SyntaxLocation, DisplayName));
            return true;
        }

        return false;
    }

    public string GetGeneratedFileName()
    {
        var name = Namespace == null ? Name : $"{Namespace}.{Name}";
        if (TypeParameterNames.AsImmutableArray().Length > 0)
            name += $"`{TypeParameterNames.AsImmutableArray().Length}";

        name += ".g.cs";

        return name;
    }

    public void WriteHeader(StringBuilder builder)
    {
        if (Namespace != null)
            builder.AppendLine($"namespace {Namespace};\n");

        // TODO: Nested classes

        var access = Accessibility switch
        {
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            _ => "public"
        };

        string keyword;
        if (Kind == TypeKind.Interface)
        {
            keyword = "interface";
        }
        else
        {
            if (IsRecord)
            {
                keyword = Kind == TypeKind.Struct ? "record struct" : "record";
            }
            else
            {
                keyword = Kind == TypeKind.Struct ? "struct" : "class";
            }
        }

        builder.Append($"{access} {(IsAbstract ? "abstract " : "")}partial {keyword} {Name}");
        if (TypeParameterNames.AsSpan().Length > 0)
        {
            builder.Append($"<{string.Join(", ", TypeParameterNames.AsImmutableArray())}>");
        }
    }

    public void WriteFooter(StringBuilder builder)
    {
        // TODO: Nested classes
    }
}
