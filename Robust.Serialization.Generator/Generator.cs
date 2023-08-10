using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Robust.Serialization.Generator;

[Generator]
public class Generator : IIncrementalGenerator
{
    private const string DataDefinitionNamespace = "Robust.Shared.Serialization.Manager.Attributes.DataDefinitionAttribute";
    private const string DataFieldNamespace = "Robust.Shared.Serialization.Manager.Attributes.DataFieldAttribute";

    private static readonly DiagnosticDescriptor DataDefinitionPartialRule = new(
        Diagnostics.IdDataDefinitionPartial,
        "Type must be partial",
        "Type {0} has a DataDefinition attribute but is not partial.",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure to mark any type that is a data definition as partial."
    );

    private static readonly DiagnosticDescriptor NestedDataDefinitionPartialRule = new(
        Diagnostics.IdDataDefinitionPartial,
        "Type must be partial",
        "Type {0} contains nested data definition {1} but is not partial.",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure to mark any type containing a nested data definition as partial."
    );

    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        Debugger.Launch();

        IncrementalValuesProvider<TypeDeclarationSyntax> dataDefinitions = initContext.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is TypeDeclarationSyntax { AttributeLists.Count: > 0 },
            static (context, _) =>
            {
                var type = (TypeDeclarationSyntax) context.Node;
                foreach (var attributeListSyntax in type.AttributeLists)
                {
                    foreach (var attributeSyntax in attributeListSyntax.Attributes)
                    {
                        var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;
                        if (symbol is IMethodSymbol attributeSymbol &&
                            attributeSymbol.ContainingType.ToDisplayString() == DataDefinitionNamespace)
                        {
                            return type;
                        }
                    }
                }

                return null;
            }
        ).Where(static type => type != null)!;

        var comparer = new DataDefinitionComparer();
        initContext.RegisterSourceOutput(
            initContext.CompilationProvider.Combine(dataDefinitions.WithComparer(comparer).Collect()),
            static (sourceContext, source) =>
            {
                var (compilation, types) = source;
                var builder = new StringBuilder();
                var containingTypes = new Stack<INamedTypeSymbol>();

                foreach (var type in types)
                {
                    builder.Clear();
                    containingTypes.Clear();

                    var symbol = (ITypeSymbol) compilation.GetSemanticModel(type.SyntaxTree).GetDeclaredSymbol(type)!;

                    if (type.Modifiers.IndexOf(SyntaxKind.PartialKeyword) == -1)
                    {
                        sourceContext.ReportDiagnostic(Diagnostic.Create(DataDefinitionPartialRule, type.Keyword.GetLocation(), symbol.Name));
                        continue;
                    }

                    builder.AppendLine($"""
#nullable enable
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;

namespace {symbol.ContainingNamespace.ToDisplayString()};
""");

                    var containingType = symbol.ContainingType;
                    while (containingType != null)
                    {
                        containingTypes.Push(containingType);
                        containingType = containingType.ContainingType;
                    }

                    var nonPartial = false;
                    foreach (var parent in containingTypes)
                    {
                        var syntax = (ClassDeclarationSyntax) parent.DeclaringSyntaxReferences[0].GetSyntax();
                        if (syntax.Modifiers.IndexOf(SyntaxKind.PartialKeyword) == -1)
                        {
                            sourceContext.ReportDiagnostic(Diagnostic.Create(NestedDataDefinitionPartialRule, syntax.Keyword.GetLocation(), parent.Name, symbol.Name));
                            nonPartial = true;
                            continue;
                        }

                        builder.AppendLine($"{GetPartialTypeDefinitionLine(parent)}\n{{");
                    }

                    if (nonPartial)
                        continue;

                    var definition = GetDataFields(symbol);

                    builder.Append($$"""
{{GetPartialTypeDefinitionLine(symbol)}} : ISerializationGenerated<{{symbol.Name}}>
{
    {{GetCopyConstructor(definition)}}

    {{GetCopyMethod(definition)}}
}
"""
                    );

                    for (var i = 0; i < containingTypes.Count; i++)
                    {
                        builder.AppendLine("}");
                    }

                    var symbolName = symbol
                        .ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)
                        .Replace('<', '{')
                        .Replace('>', '}');

                    var sourceText = CSharpSyntaxTree
                        .ParseText(builder.ToString())
                        .GetRoot()
                        .NormalizeWhitespace()
                        .ToFullString();

                    sourceContext.AddSource($"{symbol.ContainingNamespace.ToDisplayString()}.{symbolName}.g.cs", sourceText);
                }
            }
        );
    }

    private static string GetPartialTypeDefinitionLine(ITypeSymbol symbol)
    {
        var access = symbol.DeclaredAccessibility switch
        {
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "protected internal",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.Public => "public",
            _ => "public"
        };

        string typeKeyword;
        if (symbol.IsRecord)
        {
            typeKeyword = symbol.IsValueType ? "record struct" : "record";
        }
        else
        {
            typeKeyword = symbol.IsValueType ? "struct" : "class";
        }

        return $"{access} partial {typeKeyword} {symbol.Name}";
    }

    private static DataDefinition GetDataFields(ITypeSymbol symbol)
    {
        var fields = new List<DataField>();

        foreach (var member in symbol.GetMembers())
        {
            if (member is IFieldSymbol field)
            {
                foreach (var attribute in field.GetAttributes())
                {
                    if (attribute.AttributeClass?.ToDisplayString() == DataFieldNamespace)
                    {
                        fields.Add(new DataField(field, field.Type, attribute));
                        break;
                    }
                }
            }
            else if (member is IPropertySymbol property)
            {
                foreach (var attribute in property.GetAttributes())
                {
                    if (attribute.AttributeClass?.ToDisplayString() == DataFieldNamespace)
                    {
                        fields.Add(new DataField(property, property.Type, attribute));
                        break;
                    }
                }
            }
        }

        return new DataDefinition(symbol, fields);
    }

    private static string GetCopyConstructor(DataDefinition definition)
    {
        var builder = new StringBuilder();
        builder.AppendLine($$"""
public {{definition.Type.Name}}({{definition.Type.Name}} other, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null) : this()
{
""");

        foreach (var field in definition.Fields)
        {
            var type = field.Type;
            var name = field.Symbol.Name;

            if (CanBeCopiedByValue(type))
            {
                builder.AppendLine($"{name} = other.{name};");
            }
            else if (type.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == DataDefinitionNamespace))
            {
                var nullability = type.IsValueType ? string.Empty : "?";
                builder.AppendLine($"{name} = other.{name}{nullability}.Copy(serialization, hookCtx, context)!;");
            }
            else
            {
                builder.AppendLine($"{name} = serialization.CreateCopy(other.{name}, hookCtx, context);");
            }
        }

        builder.AppendLine("}");

        if (NeedsImplicitConstructor(definition.Type))
        {
            builder.AppendLine($$"""
// Implicit constructor
{{(definition.Type.IsValueType ? "#pragma warning disable CS8618" : string.Empty)}}
public {{definition.Type.Name}}()
{{(definition.Type.IsValueType ? "#pragma warning enable CS8618" : string.Empty)}}
{
}
""");
        }

        return builder.ToString();
    }

    private static string GetCopyMethod(DataDefinition definition)
    {
        var builder = new StringBuilder();
        builder.AppendLine($$"""
public {{definition.Type.Name}} Copy(ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
{
    return new {{definition.Type.Name}}(this, serialization, hookCtx, context);
}

public object CopyObject(ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
{
    return Copy(serialization, hookCtx, context);
}
""");

        return builder.ToString();
    }

    private static bool CanBeCopiedByValue(ITypeSymbol type)
    {
        if (type.OriginalDefinition.ToDisplayString() == "System.Nullable<T>")
            return CanBeCopiedByValue(((INamedTypeSymbol) type).TypeArguments[0]);

        if (type.TypeKind == TypeKind.Enum)
            return true;

        switch (type.SpecialType)
        {
            case SpecialType.System_Enum:
            case SpecialType.System_Boolean:
            case SpecialType.System_Char:
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Decimal:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_String:
            case SpecialType.System_DateTime:
                return true;
            default:
                return false;
        }
    }

    private static bool NeedsImplicitConstructor(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named)
            return false;

        if (named.InstanceConstructors.Length == 0)
            return true;

        foreach (var constructor in named.InstanceConstructors)
        {
            if (!constructor.IsImplicitlyDeclared)
                return false;
        }

        return true;
    }
}
