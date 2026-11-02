using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Roslyn.Shared;
using Robust.Roslyn.Shared.Helpers;

namespace Robust.Serialization.Generator;

/// <summary>
/// Automatically generates implementations for handling entity relation reference resetting.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class EntityRelationsGenerator : IIncrementalGenerator
{
    public const string AutoGenerateEntityRelationsAttributeName = "Robust.Shared.Analyzers.AutoGenerateEntityRelationsAttribute";
    public const string AutoRelationFieldAttributeName = "Robust.Shared.Analyzers.AutoRelationFieldAttribute";
    // ReSharper disable once InconsistentNaming
    public const string IComponentTypeName = "Robust.Shared.GameObjects.IComponent";

    private const string AutoNetworkFieldAttributeName = "Robust.Shared.Analyzers.AutoNetworkedFieldAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var componentInfos = context.SyntaxProvider.ForAttributeWithMetadataName(
            AutoGenerateEntityRelationsAttributeName,
            (syntaxNode, _) => syntaxNode is TypeDeclarationSyntax,
            Transform
        );

        context.RegisterImplementationSourceOutput(componentInfos, Generate);
    }

    /// <summary> Extracts valid EntityRelations fields that needs code generated (with additional info about field/property). </summary>
    private static ComponentInfo Transform(GeneratorAttributeSyntaxContext syntaxContext, CancellationToken _)
    {
        var symbol = (INamedTypeSymbol)syntaxContext.TargetSymbol;

        var typeDeclarationSyntax = (TypeDeclarationSyntax)syntaxContext.TargetNode;
        var partialTypeInfo = PartialTypeInfo.FromSymbol(symbol, typeDeclarationSyntax);

        var autoGenerateRelationshipAttribute = syntaxContext.Attributes[0];
        if (autoGenerateRelationshipAttribute.ConstructorArguments.Length > 0)
            AttributeHelper.GetNamedArgumentBool(autoGenerateRelationshipAttribute, "Dirty", false);

        var dirty = false;
        if (autoGenerateRelationshipAttribute.ConstructorArguments[0].Value is bool dirtyBool)
            dirty = dirtyBool;

        var fieldBuilder = ImmutableArray.CreateBuilder<FieldInfo>();
        foreach (var member in symbol.GetMembers())
        {
            if (!TryGetValidFieldOrProperty(member, out var namedType))
                continue;

            bool invalid = false,
                 nullable = false,
                 dictionaryKey = false,
                 dictionaryValue = false,
                 collection = false;

            if (namedType.Name != "EntityRelation")
            {
                switch (namedType)
                {
                    case { Name: "Nullable", TypeArguments: [{ Name: "EntityRelation" }] }:
                        nullable = true;
                        break;
                    case { Name: "Dictionary", TypeArguments: [{ Name: "EntityRelation" }, not null] }:
                        dictionaryKey = true;
                        break;
                    case { Name: "Dictionary", TypeArguments: [not null, { Name: "EntityRelation" }] }:
                        dictionaryValue = true;
                        break;
                    default:
                    {
                        if (namedType.Name == "List" || namedType.Name == "HashSet" && namedType is { TypeArguments: [{ Name: "EntityRelation" }] })
                        {
                            collection = true;
                        }
                        else
                        {
                            invalid = true;
                        }

                        break;
                    }
                }
            }

            // If any relation field has [AutoNetworkedField], automatically mark it to dirty on reference reset.
            if (AttributeHelper.HasAttribute(member, AutoNetworkFieldAttributeName, out var _))
                dirty = true;

            fieldBuilder.Add(new FieldInfo(member.Name, nullable, invalid, dictionaryKey, dictionaryValue, collection));
        }

        return new ComponentInfo(partialTypeInfo,
            EquatableArray<FieldInfo>.FromImmutableArray(fieldBuilder.ToImmutable()),
            dirty,
            !symbol.ImplementsInterface(IComponentTypeName)
        );
    }

    private static bool TryGetValidFieldOrProperty(ISymbol member,[NotNullWhen(true)] out INamedTypeSymbol? namedTypeSymbol)
    {
        if (AttributeHelper.HasAttribute(member, AutoRelationFieldAttributeName, out _))
        {
            var type = member switch
            {
                IPropertySymbol property => property.Type,
                IFieldSymbol field => field.Type,
                _ => null
            };

            if (type is INamedTypeSymbol namedType)
            {
                namedTypeSymbol = namedType;
                return true;
            }
        }

        namedTypeSymbol = null;
        return false;
    }

    private static void Generate(SourceProductionContext productionContext, ComponentInfo info)
    {
        if (info.NotComponent)
            return;

        if (!info.PartialTypeInfo.IsValid)
            return;

        if (info.Fields.AsImmutableArray().Length == 0)
            return;

        // Clears a specific EntityRelation from all fields
        var relationBuilder = new StringBuilder();

        // Silently sets all EntityRelations to null
        var clearBuilder = new StringBuilder();

        // Properly clears all EntityRelations and fixes them in target entities
        var shutdownBuilder = new StringBuilder();

        var anyValidField = false;
        foreach (var field in info.Fields)
        {
            if (field.Invalid)
                continue;

            if (field.Nullable)
            {
                relationBuilder.AppendLine($"""
                            if (ent.Comp.{field.Name}.HasValue && ent.Comp.{field.Name}.Value == args.Relation)
                                ent.Comp.{field.Name} = null;
                    """);

                clearBuilder.AppendLine($"        ent.Comp.{field.Name} = null;");

                shutdownBuilder.AppendLine($"        entMan.ClearRelation(ent.Owner, ref ent.Comp.{field.Name}, false);");
            }
            else if (field.DictionaryKey)
            {
                relationBuilder.AppendLine($"        ent.Comp.{field.Name}.Remove(args.Relation);");

                clearBuilder.AppendLine($"        ent.Comp.{field.Name}.Clear();");

                shutdownBuilder.AppendLine($"        entMan.ClearRelations(ent.Owner, ent.Comp.{field.Name}, false);");
            }
            else if (field.DictionaryValue)
            {
                relationBuilder.AppendLine($$"""
                            foreach (var (key, value) in ent.Comp.{{field.Name}})
                            {
                                if (value == args.Relation)
                                    ent.Comp.{{field.Name}}[key] = EntityRelation.Null;
                            }
                    """);

                clearBuilder.AppendLine($$"""
                            foreach (var key in ent.Comp.{{field.Name}}.Keys)
                            {
                                ent.Comp.{{field.Name}}[key] = EntityRelation.Null;
                            }
                    """);

                shutdownBuilder.AppendLine($"        entMan.ClearRelations(ent.Owner, ent.Comp.{field.Name}, false);");
            }
            else if (field.Collection)
            {
                relationBuilder.AppendLine($"        ent.Comp.{field.Name}.Remove(args.Relation);");
                clearBuilder.AppendLine($"        ent.Comp.{field.Name}.Clear();");
                shutdownBuilder.AppendLine($"        entMan.ClearRelations(ent.Owner, ent.Comp.{field.Name}, false);");
            }
            else
            {
                relationBuilder.AppendLine($"""
                            if (ent.Comp.{field.Name} == args.Relation)
                                ent.Comp.{field.Name} = EntityRelation.Null;
                    """);

                clearBuilder.AppendLine($"        ent.Comp.{field.Name} = EntityRelation.Null;");

                shutdownBuilder.AppendLine($"        entMan.ClearRelation(ent.Owner, ref ent.Comp.{field.Name}, false);");
            }

            anyValidField = true;
        }

        if (!anyValidField)
            return;

        if (info.Dirty)
        {
            relationBuilder.AppendLine("        Dirty(ent);");
            clearBuilder.AppendLine("        Dirty(ent);");
            shutdownBuilder.AppendLine("""
                        if (entMan.GetEntityQuery<MetaDataComponent>().Comp(ent.Owner).EntityLifeStage < EntityLifeStage.Terminating)
                            entMan.Dirty(ent);
                """);
        }

        var result = new StringBuilder();

        result.AppendLine("""
            // <auto-generated />

            using Robust.Shared.GameObjects;

            """);

        info.PartialTypeInfo.WriteHeader(result);

        result.AppendLine($$"""

            {
            [RobustAutoGenerated]
            [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
            public sealed partial class {{info.PartialTypeInfo.Name}}_AutoRelationsSystem : EntitySystem
            {
                public override void Initialize()
                {
                    base.Initialize();
                    SubscribeLocalEvent<{{info.PartialTypeInfo.Name}}, EntityRelationDeleteEvent>(OnRelationDeleted);
                    SubscribeLocalEvent<{{info.PartialTypeInfo.Name}}, EntityRelationShutdownEvent>(OnRelationsClear);
                    SubscribeLocalEvent<{{info.PartialTypeInfo.Name}}, ComponentRemove>(OnRelationRemove);
                }

                private void OnRelationDeleted(Entity<{{info.PartialTypeInfo.Name}}> ent, ref EntityRelationDeleteEvent args)
                {
            {{relationBuilder}}
                }

                private void OnRelationsClear(Entity<{{info.PartialTypeInfo.Name}}> ent, ref EntityRelationShutdownEvent args)
                {
            {{clearBuilder}}
                }

                private void OnRelationRemove(Entity<{{info.PartialTypeInfo.Name}}> ent, ref ComponentRemove args)
                {
            {{shutdownBuilder}}
                }
            }
            """);

        info.PartialTypeInfo.WriteFooter(result);

        productionContext.AddSource(info.PartialTypeInfo.GetGeneratedFileName(), result.ToString());
    }

    private record struct ComponentInfo(
        PartialTypeInfo PartialTypeInfo,
        EquatableArray<FieldInfo> Fields,
        bool Dirty,
        bool NotComponent
    );

    private record struct FieldInfo(
        string Name,
        bool Nullable,
        bool Invalid,
        bool DictionaryKey,
        bool DictionaryValue,
        bool Collection
    );
}
