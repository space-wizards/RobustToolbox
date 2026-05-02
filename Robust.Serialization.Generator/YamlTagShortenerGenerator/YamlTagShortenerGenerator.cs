using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Robust.Roslyn.Shared;

namespace Robust.Serialization.Generator.YamlTagShortenerGenerator;

[Generator]
public class YamlTagShortenerGenerator : IIncrementalGenerator
{
    private const string YamlTagShortenerAttributeNamespace = "Robust.Shared.Serialization.Manager.Attributes.YamlTagShortenerAttribute";
    private const string CustomChildTagAttributeNamespace = "Robust.Shared.Serialization.Manager.Attributes.CustomChildTagAttribute";
    private const string CustomChildTagAttributeName = "CustomChildTagAttribute`1";


    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<YamlTagShortenerDefinition?> shortenersToCreate = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        context.RegisterSourceOutput(shortenersToCreate,
            static (spc, source) => Execute(source, spc));

    }

    private static void Execute(YamlTagShortenerDefinition? shortenerToCreate, SourceProductionContext context)
    {
        if (shortenerToCreate is null)
            return;

        var result = GenerateSerializer(shortenerToCreate);
        context.AddSource($"{shortenerToCreate.BaseTypeNamespace}.{shortenerToCreate.BaseTypeName}.g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => node is TypeDeclarationSyntax m && m.AttributeLists.Count > 0;

    private static YamlTagShortenerDefinition? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var typeDeclarationSyntax = (TypeDeclarationSyntax)context.Node;

        foreach (var attributeListSyntax in typeDeclarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                if (attributeContainingTypeSymbol.ToDisplayString() == YamlTagShortenerAttributeNamespace ||
                    attributeContainingTypeSymbol.MetadataName == CustomChildTagAttributeName)
                {
                    return GetTypeToGenerate(context.SemanticModel, typeDeclarationSyntax);
                }
            }
        }

        return null;
    }

    private static YamlTagShortenerDefinition? GetTypeToGenerate(SemanticModel semanticModel, SyntaxNode typeDeclarationSyntax)
    {
        if (semanticModel.GetDeclaredSymbol(typeDeclarationSyntax) is not INamedTypeSymbol typeSymbol)
            return null;


        var typeName = typeSymbol.Name;
        var typeNamespace = typeSymbol.ContainingNamespace.ToDisplayString();
        var endsInBase = typeName.EndsWith("Base");

        var customChildTags = new List<(string, string)>();
        // Look at other attributes and iterate through all CustomChildTag attributes.
        var attrs = typeSymbol.GetAttributes();
        foreach (var attr in attrs)
        {
            if (attr.AttributeClass == null ||
                !CustomChildTagAttributeNamespace.EndsWith(attr.AttributeClass.Name) ||
                attr.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax attributeSyntax ||
                attributeSyntax.Name is not GenericNameSyntax genericNameSyntax) continue;

            var typeArg = genericNameSyntax.TypeArgumentList.Arguments[0];
            var constructorArg = attr.ConstructorArguments[0];

            if(constructorArg.Value is string str)
            {
                customChildTags.Add((str, typeArg.ToString()));
            }
        }

        if (!endsInBase && customChildTags.Count == 0)
            return null;

        return new YamlTagShortenerDefinition(typeName, typeNamespace, customChildTags);
    }

    private static string GenerateSerializer(YamlTagShortenerDefinition def)
    {
        var customChildTagsBuilder = new StringBuilder();

        var baseTypeName = def.BaseTypeName;
        var baseTypeNamespace = def.BaseTypeNamespace;
        var baseNameWithoutBase = YamlTagShortenerHelper.ReplaceLast(baseTypeName, "Base", string.Empty);

        foreach (var item in def.CustomChildTags)
        {
            customChildTagsBuilder.Append($"        {{\"!{item.Item1}\", typeof({item.Item2})}},\n");
        }

        var builder = new StringBuilder();

        builder.AppendLine($$"""
            #nullable enable

            using System;
            using System.Collections.Generic;
            using Robust.Shared.IoC;
            using Robust.Shared.Serialization;
            using Robust.Shared.Serialization.Manager;
            using Robust.Shared.Serialization.Manager.Attributes;
            using Robust.Shared.Serialization.Markdown;
            using Robust.Shared.Serialization.Markdown.Mapping;
            using Robust.Shared.Serialization.Markdown.Validation;
            using Robust.Shared.Serialization.Markdown.Value;
            using Robust.Shared.Serialization.TypeSerializers.Interfaces;
            using Robust.Shared.Utility;

            namespace {{baseTypeNamespace}};

            [TypeSerializer]
            internal sealed class {{baseTypeName}}YamlShortenerSerializer :
                ITypeReader<{{baseTypeName}}, MappingDataNode>,
                ITypeReader<{{baseTypeName}}, ValueDataNode>
            {

                private static readonly Dictionary<string, Type> CustomChildTags = new Dictionary<string, Type>()
                {
            {{customChildTagsBuilder}}
                };

                private static string ExpandName(string name)
                {
                    // Check for a custom child tag definition first.
                    if (CustomChildTags.ContainsKey(name))
                        return $"!type:{CustomChildTags[name].Name}";
                    // !ConcreteThing -> !type:CustomTypeConcreteThing
                    return name.Contains(':')
                        ? name
                        : name.Replace("!", $"!type:{{baseNameWithoutBase}}");
                }

                private static void ThrowOnNullTag(DataNode node)
                {
                    if (node.Tag != null)
                        return;

                    throw new InvalidMappingException(
                        $"{node.Start}: Node does not have a tag (value starting with '!').");
                }

                private static string ReplaceLast(string currentString, string stringToReplace, string replacement)
                {
                    var lastStart = currentString.LastIndexOf(stringToReplace, StringComparison.Ordinal);
                    return currentString.Remove(lastStart, stringToReplace.Length) + replacement;
                }

                private static {{baseTypeName}} ReadDataNode(
                    ISerializationManager serializationManager,
                    DataNode node,
                    SerializationHookContext hookCtx,
                    ISerializationContext? context = null,
                    ISerializationManager.InstantiationDelegate<{{baseTypeName}}>? instanceProvider = null)
                {
                    ThrowOnNullTag(node);
                    node.Tag = ExpandName(node.Tag!);
                    return serializationManager.Read(node, hookCtx, context, instanceProvider, false);
                }

                private static ValidationNode ValidateDataNode(
                    ISerializationManager serializationManager,
                    DataNode node,
                    ISerializationContext? context = null)
                {
                    if (node.Tag == null)
                        return new ErrorNode(node, "Node does not have a tag (value starting with '!').");
                    var copy = node.Copy();
                    copy.Tag = ExpandName(node.Tag);
                    return serializationManager.ValidateNode<{{baseTypeName}}>(copy, context);
                }

                public {{baseTypeName}} Read(
                    ISerializationManager serializationManager,
                    ValueDataNode node,
                    IDependencyCollection dependencies,
                    SerializationHookContext hookCtx,
                    ISerializationContext? context = null,
                    ISerializationManager.InstantiationDelegate<{{baseTypeName}}>? instanceProvider = null)
                {
                    return ReadDataNode(serializationManager, node, hookCtx, context, instanceProvider);
                }

                public {{baseTypeName}} Read(
                    ISerializationManager serializationManager,
                    MappingDataNode node,
                    IDependencyCollection dependencies,
                    SerializationHookContext hookCtx,
                    ISerializationContext? context = null,
                    ISerializationManager.InstantiationDelegate<{{baseTypeName}}>? instanceProvider = null)
                {
                    return ReadDataNode(serializationManager, node, hookCtx, context, instanceProvider);
                }

                public ValidationNode Validate(
                    ISerializationManager serializationManager,
                    MappingDataNode node,
                    IDependencyCollection dependencies,
                    ISerializationContext? context = null)
                {
                    return ValidateDataNode(serializationManager, node, context);
                }

                public ValidationNode Validate(
                    ISerializationManager serializationManager,
                    ValueDataNode node,
                    IDependencyCollection dependencies,
                    ISerializationContext? context = null)
                {
                    return ValidateDataNode(serializationManager, node, context);
                }
            }
            """);

        return builder.ToString();

    }
}
