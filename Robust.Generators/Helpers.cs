using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Generators.DependencyInjector;

namespace Robust.Generators;


public static class Helpers
{
    public static (string start, string end, bool allPartial) GenerateContainingTypeCode(INamedTypeSymbol type)
    {
        var containingTypes = new Stack<INamedTypeSymbol>();

        var containingType = type.ContainingType;
        while (containingType != null)
        {
            containingTypes.Push(containingType);
            containingType = containingType.ContainingType;
        }

        var allPartial = true;

        var containingTypesStart = new StringBuilder();
        var containingTypesEnd = new StringBuilder();
        foreach (var parent in containingTypes)
        {
            var syntax = (ClassDeclarationSyntax) parent.DeclaringSyntaxReferences[0].GetSyntax();
            if (!IsPartial(syntax))
            {
                allPartial = false;
                continue;
            }

            containingTypesStart.AppendLine($"{GetPartialTypeDefinitionLine(parent)}\n{{");
            containingTypesEnd.AppendLine("}");
        }

        return (containingTypesStart.ToString(), containingTypesEnd.ToString(), allPartial);
    }

    public static (string start, string end) GenerateContainingTypeCode(ImmutableArray<PartialTypeDeclarationData> data)
    {
        var containingTypesStart = new StringBuilder();
        var containingTypesEnd = new StringBuilder();
        foreach (var parent in data)
        {
            containingTypesStart.AppendLine($"{GetPartialTypeDefinitionLine(parent)}\n{{");
            containingTypesEnd.AppendLine("}");
        }

        return (containingTypesStart.ToString(), containingTypesEnd.ToString());
    }

    public static ImmutableArray<PartialTypeDeclarationData>? GenerateContainingTypeData(
        INamedTypeSymbol type, CancellationToken cancel)
    {
        var containingTypes = new Stack<INamedTypeSymbol>();

        var containingType = type.ContainingType;
        while (containingType != null)
        {
            containingTypes.Push(containingType);
            containingType = containingType.ContainingType;
        }

        var data = new List<PartialTypeDeclarationData>();
        foreach (var parent in containingTypes)
        {
            var syntax = (ClassDeclarationSyntax) parent.DeclaringSyntaxReferences[0].GetSyntax(cancel);

            if (!IsPartial(syntax))
                return null;

            data.Add(Helpers.GetPartialTypeDeclarationData(parent));
        }

        return data.ToImmutableArray();
    }

    public static PartialTypeDeclarationData GetPartialTypeDeclarationData(ITypeSymbol type)
    {
        return new PartialTypeDeclarationData(GetGenericTypeName(type), GetPartialTypeKind(type));
    }

    public static PartialTypeKind GetPartialTypeKind(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Interface)
            return PartialTypeKind.Interface;

        if (type.IsRecord)
            return type.IsValueType ? PartialTypeKind.RecordStruct : PartialTypeKind.Record;

        return type.IsValueType ? PartialTypeKind.Struct : PartialTypeKind.Class;
    }

    public static bool IsPartial(TypeDeclarationSyntax type)
    {
        return type.Modifiers.IndexOf(SyntaxKind.PartialKeyword) != -1;
    }

    public static bool IsPartial(INamedTypeSymbol type, CancellationToken cancel)
    {
        var references = type.DeclaringSyntaxReferences;
        if (references.Length == 0)
            return false;

        var typeDecl = (TypeDeclarationSyntax) references[0].GetSyntax(cancel);
        return IsPartial(typeDecl);
    }

    public static string GetPartialTypeDefinitionLine(ITypeSymbol symbol)
    {
        var typeKeyword = "partial ";
        if (symbol.TypeKind == TypeKind.Interface)
        {
            typeKeyword += "interface";
        }
        else
        {
            if (symbol.IsRecord)
            {
                typeKeyword += symbol.IsValueType ? "record struct" : "record";
            }
            else
            {
                typeKeyword += symbol.IsValueType ? "struct" : "class";
            }
        }

        var typeName = GetGenericTypeName(symbol);
        return $"{typeKeyword} {typeName}";
    }

    public static string GetPartialTypeDefinitionLine(PartialTypeDeclarationData data)
    {
        var keyword = data.Kind switch
        {
            PartialTypeKind.Interface => "interface",
            PartialTypeKind.Class => "class",
            PartialTypeKind.Struct => "struct",
            PartialTypeKind.Record => "record",
            PartialTypeKind.RecordStruct => "record struct",
            _ => throw new ArgumentOutOfRangeException()
        };

        return $"partial {keyword} {data.Name}";
    }

    public static string GetGenericTypeName(ITypeSymbol symbol)
    {
        var name = symbol.Name;

        if (symbol is INamedTypeSymbol { TypeParameters: { Length: > 0 } parameters })
        {
            name += "<";

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                name += parameter.Name;

                if (i < parameters.Length - 1)
                {
                    name += ", ";
                }
            }

            name += ">";
        }

        return name;
    }
}
