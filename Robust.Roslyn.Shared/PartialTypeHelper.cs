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
    EquatableArray<PartialTypeInfo.NestedPart> Parts,
    bool IsValid,
    Location SyntaxLocation,
    bool IsSealed)
{
    public string Name => Parts[^1].Name;
    public string DisplayName => Parts[^1].DisplayName;

    public string FullDisplayName => Namespace != null ? $"{Namespace}.{Name}" : Name;

    public static PartialTypeInfo FromSymbol(INamedTypeSymbol symbol, TypeDeclarationSyntax syntax)
    {
        var parts = ImmutableArray<NestedPart>.Empty.ToBuilder();
        var isValid = true;

        var curSymbol = symbol;
        var curSyntax = syntax;

        do
        {
            if (!IsPartial(curSyntax))
            {
                isValid = false;
                break;
            }

            parts.Insert(0, NestedPart.FromNode(curSymbol, curSyntax));

            curSymbol = curSymbol.ContainingType;
            curSyntax = curSyntax.Parent as TypeDeclarationSyntax;
        } while (curSymbol != null && curSyntax != null);

        return new PartialTypeInfo(
            symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString(),
            parts.ToImmutable().AsEquatableArray(),
            isValid,
            syntax.Keyword.GetLocation(),
            symbol.IsSealed);
    }

    private static bool IsPartial(TypeDeclarationSyntax syntax)
    {
        foreach (var modifier in syntax.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.PartialKeyword))
                return true;
        }

        return false;
    }

    [Obsolete("Diagnostics from source generators are recommended against, apparently: https://github.com/dotnet/roslyn/issues/71709")]
    public bool CheckPartialDiagnostic(SourceProductionContext context, DiagnosticDescriptor diagnostic)
    {
        if (!IsValid)
        {
            context.ReportDiagnostic(Diagnostic.Create(diagnostic, SyntaxLocation, Parts[^1].DisplayName));
            return true;
        }

        return false;
    }

    public string GetGeneratedFileName()
    {
        var name = Namespace == null ? "" : $"{Namespace}.";

        for (var index = 0; index < Parts.Length; index++)
        {
            var part = Parts[index];
            name += part.Name;

            if (part.TypeParameterNames.Length > 0)
                name += $"`{part.TypeParameterNames.Length}";

            if (index < Parts.Length - 1)
                name += ".";
        }

        name += ".g.cs";

        return name;
    }

    public void WriteHeader(StringBuilder builder)
    {
        var writer = new IndentWriter(builder);
        WriteHeader(ref writer);
    }

    public void WriteHeader(ref IndentWriter builder, string? attributes = null)
    {
        if (Namespace != null)
            builder.AppendLine($"namespace {Namespace};\n");

        for (var index = 0; index < Parts.Length; index++)
        {
            var part = Parts[index];
            var access = part.Accessibility switch
            {
                Accessibility.Private => "private",
                Accessibility.ProtectedAndInternal => "private protected",
                Accessibility.ProtectedOrInternal => "protected internal",
                Accessibility.Protected => "protected",
                Accessibility.Internal => "internal",
                _ => "public"
            };

            string keyword;
            if (part.Kind == TypeKind.Interface)
            {
                keyword = "interface";
            }
            else
            {
                if (part.IsRecord)
                {
                    keyword = part.Kind == TypeKind.Struct ? "record struct" : "record";
                }
                else
                {
                    keyword = part.Kind == TypeKind.Struct ? "struct" : "class";
                }
            }

            if (attributes != null && index == Parts.Length - 1)
                builder.AppendLineIndented(attributes);

            builder.AppendIndents();
            builder.Append($"{access} {(part.IsAbstract ? "abstract " : "")}partial {keyword} {part.Name}");
            if (part.TypeParameterNames.Length > 0)
            {
                builder.Append($"<{string.Join(", ", part.TypeParameterNames.AsImmutableArray())}>");
            }

            if (index != Parts.Length - 1)
            {
                builder.AppendLine();
                builder.AppendOpeningBrace();
            }
        }
    }

    public void WriteFooter(StringBuilder builder)
    {
        var writer = new IndentWriter(builder);
        WriteFooter(ref writer);
    }

    public void WriteFooter(ref IndentWriter builder)
    {
        // Loop starts at 1, only write for nested classes.
        for (var i = 1; i < Parts.Length; i++)
        {
            builder.AppendClosingBrace();
        }
    }

    public string GetMetadataName()
    {
        var sb = new StringBuilder();

        if (Namespace != null)
        {
            sb.Append(Namespace);
            sb.Append('.');
        }

        for (var i = 0; i < Parts.Length; i++)
        {
            var part = Parts[i];
            sb.Append(part.MetadataName);
            if (i != Parts.Length - 1)
                sb.Append('+');
        }

        return sb.ToString();
    }

    public sealed record NestedPart(
        string Name,
        string MetadataName,
        string DisplayName,
        EquatableArray<string> TypeParameterNames,
        Accessibility Accessibility,
        TypeKind Kind,
        bool IsRecord,
        bool IsAbstract)
    {
        public static NestedPart FromNode(INamedTypeSymbol symbol, TypeDeclarationSyntax syntax)
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

            return new NestedPart(
                symbol.Name,
                symbol.MetadataName,
                symbol.ToDisplayString(),
                typeParameters,
                symbol.DeclaredAccessibility,
                symbol.TypeKind,
                symbol.IsRecord,
                symbol.IsAbstract);
        }
    }

    public sealed class WithoutLocationComparer : IEqualityComparer<PartialTypeInfo>
    {
        public static readonly WithoutLocationComparer Instance = new();

        public bool Equals(PartialTypeInfo? x, PartialTypeInfo? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null) return false;
            if (y is null) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Namespace == y.Namespace && x.Parts.Equals(y.Parts) && x.IsValid == y.IsValid;
        }

        public int GetHashCode(PartialTypeInfo obj)
        {
            unchecked
            {
                var hashCode = (obj.Namespace != null ? obj.Namespace.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ obj.Parts.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.IsValid.GetHashCode();
                return hashCode;
            }
        }
    }
}
