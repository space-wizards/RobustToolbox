using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Roslyn.Shared;
using Robust.Roslyn.Shared.Helpers;

namespace Robust.Serialization.Generator;

/// <summary>
/// Automatically generates implementations for handling timer unpausing.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class EntityRelationsGenerator : IIncrementalGenerator
{
    private const string AutoGenerateComponentPauseAttributeName = "Robust.Shared.Analyzers.AutoGenerateEntityRelationsAttribute";
    private const string AutoPausedFieldAttributeName = "Robust.Shared.Analyzers.AutoRelationFieldAttribute";
    private const string AutoNetworkFieldAttributeName = "Robust.Shared.Analyzers.AutoNetworkedFieldAttribute";
    // ReSharper disable once InconsistentNaming
    private const string IComponentTypeName = "Robust.Shared.GameObjects.IComponent";

    private static readonly DiagnosticDescriptor NotComponentDiagnostic = new(
        Diagnostics.IdComponentPauseNotComponent,
        "Class must be an IComponent to use AutoGenerateEntityRelations",
        "Class '{0}' must implement IComponent to be used with [AutoGenerateEntityRelations]",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor NoFieldsDiagnostic = new(
        Diagnostics.IdComponentPauseNoFields,
        "AutoGenerateEntityRelations has no fields",
        "Class '{0}' has [AutoGenerateEntityRelations] but has no fields or properties with [AutoRelationField]",
        "Usage",
        DiagnosticSeverity.Warning,
        true);

    private static readonly DiagnosticDescriptor NoParentAttributeDiagnostic = new(
        Diagnostics.IdComponentPauseNoParentAttribute,
        "AutoRelationField on type of field without AutoGenerateEntityRelations",
        "Field '{0}' has [AutoRelationField] but its containing type does not have [AutoGenerateEntityRelations]",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor WrongTypeAttributeDiagnostic = new(
        Diagnostics.IdComponentPauseWrongTypeAttribute,
        "AutoRelationField has wrong type",
        "Field '{0}' has [AutoRelationField] but is not of type EntityRelation",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var componentInfos = context.SyntaxProvider.ForAttributeWithMetadataName(
            AutoGenerateComponentPauseAttributeName,
            (syntaxNode, _) => syntaxNode is TypeDeclarationSyntax,
            (syntaxContext, _) =>
            {
                var symbol = (INamedTypeSymbol)syntaxContext.TargetSymbol;

                var typeDeclarationSyntax = (TypeDeclarationSyntax) syntaxContext.TargetNode;
                var partialTypeInfo = PartialTypeInfo.FromSymbol(
                    symbol,
                    typeDeclarationSyntax);

                if (syntaxContext.Attributes[0].ConstructorArguments.Length > 0)
                    AttributeHelper.GetNamedArgumentBool(syntaxContext.Attributes[0], "Dirty", false);

                var dirty = false;
                if (syntaxContext.Attributes[0].ConstructorArguments[0].Value is bool dirtyBool)
                    dirty = dirtyBool;

                var shutdownSub = false;
                if (syntaxContext.Attributes[0].ConstructorArguments[1].Value is bool shutdownSubBool)
                    shutdownSub = shutdownSubBool;

                var fieldBuilder = ImmutableArray.CreateBuilder<FieldInfo>();
                foreach (var member in symbol.GetMembers())
                {
                    if (!AttributeHelper.HasAttribute(member, AutoPausedFieldAttributeName, out var _))
                        continue;

                    var type = member switch
                    {
                        IPropertySymbol property => property.Type,
                        IFieldSymbol field => field.Type,
                        _ => null
                    };

                    if (type is not INamedTypeSymbol namedType)
                        continue;

                    var invalid = false;
                    var nullable = false;
                    var dictionaryKey = false;
                    var dictionaryValue = false;
                    var collection = false;
                    if (namedType.Name != "EntityRelation")
                    {
                        if (namedType is { Name: "Nullable", TypeArguments: [{Name: "EntityRelation"}] })
                        {
                            nullable = true;
                        }
                        else if (namedType is { Name: "Dictionary", TypeArguments: [{Name: "EntityRelation"}, {}]})
                        {
                            dictionaryKey = true;
                        }
                        else if (namedType is { Name: "Dictionary", TypeArguments: [{}, {Name: "EntityRelation"}]})
                        {
                            dictionaryValue = true;
                        }
                        if (namedType.Name == "List" || namedType.Name == "HashSet" && namedType is { TypeArguments: [{ Name: "EntityRelation" }]})
                        {
                            collection = true;
                        }
                        else
                        {
                            invalid = true;
                        }
                    }

                    // If any pause field has [AutoNetworkedField], automatically mark it to dirty on unpause.
                    if (AttributeHelper.HasAttribute(member, AutoNetworkFieldAttributeName, out var _))
                        dirty = true;

                    fieldBuilder.Add(new FieldInfo(member.Name, nullable, invalid, dictionaryKey, dictionaryValue, collection, member.Locations[0]));
                }

                return new ComponentInfo(
                    partialTypeInfo,
                    EquatableArray<FieldInfo>.FromImmutableArray(fieldBuilder.ToImmutable()),
                    dirty,
                    shutdownSub,
                    !TypeSymbolHelper.ImplementsInterface(symbol, IComponentTypeName),
                    typeDeclarationSyntax.Identifier.GetLocation());
            });

        context.RegisterImplementationSourceOutput(componentInfos, static (productionContext, info) =>
        {
            if (info.NotComponent)
            {
                productionContext.ReportDiagnostic(Diagnostic.Create(
                    NotComponentDiagnostic,
                    info.Location,
                    info.PartialTypeInfo.Name));
                return;
            }

            // Component always have to be partial anyways due to the serialization generator.
            // So I can't be arsed to define a diagnostic for this.
            if (!info.PartialTypeInfo.IsValid)
                return;

            if (info.Fields.AsImmutableArray().Length == 0)
            {
                productionContext.ReportDiagnostic(Diagnostic.Create(
                    NoFieldsDiagnostic,
                    info.Location,
                    info.PartialTypeInfo.Name));
                return;
            }

            var relationBuilder = new StringBuilder();
            var shutdownBuilder = new StringBuilder();

            var anyValidField = false;
            foreach (var field in info.Fields)
            {
                if (field.Invalid)
                {
                    productionContext.ReportDiagnostic(Diagnostic.Create(WrongTypeAttributeDiagnostic, field.Location));
                    continue;
                }

                if (field.Nullable)
                {
                    relationBuilder.AppendLine($"""
                                if (ent.Comp.{field.Name}.HasValue && ent.Comp.{field.Name}.Value == args.Relation)
                                    ent.Comp.{field.Name} = null;
                        """);

                    shutdownBuilder.AppendLine($"        entMan.ClearRelation(ent.Owner, ref ent.Comp.{field.Name});");
                }
                else if (field.DictionaryKey)
                {
                    relationBuilder.AppendLine($"        ent.Comp.{field.Name}.Remove(args.Relation);");

                    shutdownBuilder.AppendLine($"        entMan.ClearRelation(ent.Owner, ent.Comp.{field.Name});");
                }
                else if (field.DictionaryValue)
                {
                    relationBuilder.AppendLine($$"""
                                foreach (var (key, value) in ent.Comp.{{field.Name}})
                                {
                                    if (ent.Comp.{{field.Name}}[key] == args.Relation)
                                    ent.Comp.{{field.Name}}[key] = EntityRelation.Null;
                                }
                        """);

                    shutdownBuilder.AppendLine($"        entMan.ClearRelation(ent.Owner, ent.Comp.{field.Name});");
                }
                else if (field.Collection)
                {
                    relationBuilder.AppendLine($"        ent.Comp.{field.Name}.Remove(args.Relation);");
                    shutdownBuilder.AppendLine($"        entMan.ClearRelation(ent.Owner, ent.Comp.{field.Name});");
                }
                else
                {
                    relationBuilder.AppendLine($"""
                                if (ent.Comp.{field.Name} == args.Relation)
                                    ent.Comp.{field.Name} = EntityRelation.Null;
                        """);

                    shutdownBuilder.AppendLine($"        entMan.ClearRelation(ent.Owner, ref ent.Comp.{field.Name});");
                }

                anyValidField = true;
            }

            if (!anyValidField)
                return;

            if (info.Dirty)
                relationBuilder.AppendLine("        Dirty(ent);");

            var shutdownSub = info.ShutdownEvent
                ? $"        SubscribeLocalEvent<{info.PartialTypeInfo.Name}, ComponentShutdown>(OnRelationShutdown);"
                : string.Empty;

            var shutdownSubMethod = info.ShutdownEvent
                ? $$"""
                        private void OnRelationShutdown(Entity<{{info.PartialTypeInfo.Name}}> ent, ref ComponentShutdown args)
                        {
                            {{info.PartialTypeInfo.Name}}.ClearComponentRelations(ent, EntityManager);
                        }
                    """
                : string.Empty;

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
                {{shutdownSub}}
                        SubscribeLocalEvent<{{info.PartialTypeInfo.Name}}, EntityRelationDeleteEvent>(OnRelationDeleted);
                    }

                    private void OnRelationDeleted(Entity<{{info.PartialTypeInfo.Name}}> ent, ref EntityRelationDeleteEvent args)
                    {
                {{relationBuilder}}
                    }

                {{shutdownSubMethod}}
                }

                    /// <summary>
                    /// Auto-generated method that clears all relations in a certain entity.
                    /// This has to be called on component shutdown to keep all relations correct.
                    /// </summary>
                    public static void ClearComponentRelations(Entity<{{info.PartialTypeInfo.Name}}> ent, IEntityManager entMan)
                    {
                {{shutdownBuilder}}
                    }
                }
                """);

            info.PartialTypeInfo.WriteFooter(result);

            productionContext.AddSource(info.PartialTypeInfo.GetGeneratedFileName(), result.ToString());
        });

        // Code to report diagnostic for fields that have it but don't have the attribute on the parent.
        var allFields = context.SyntaxProvider.ForAttributeWithMetadataName(
            AutoPausedFieldAttributeName,
            (syntaxNode, _) => syntaxNode is VariableDeclaratorSyntax or PropertyDeclarationSyntax,
            (syntaxContext, _) =>
            {
                var errorTarget = syntaxContext.TargetNode is PropertyDeclarationSyntax prop
                    ? prop.Identifier.GetLocation()
                    : syntaxContext.TargetNode.GetLocation();
                return new AllFieldInfo(
                    syntaxContext.TargetSymbol.Name,
                    syntaxContext.TargetSymbol.ContainingType.ToDisplayString(),
                    errorTarget);
            });

        var allComponentsTogether = componentInfos.Collect();
        var allFieldsTogether = allFields.Collect();
        var componentFieldJoin = allFieldsTogether.Combine(allComponentsTogether);

        context.RegisterImplementationSourceOutput(componentFieldJoin, (productionContext, info) =>
        {
            var componentsByName = new HashSet<string>(info.Right.Select(x => x.PartialTypeInfo.DisplayName));
            foreach (var field in info.Left)
            {
                if (!componentsByName.Contains(field.ParentDisplayName))
                {
                    productionContext.ReportDiagnostic(
                        Diagnostic.Create(NoParentAttributeDiagnostic, field.Location, field.Name));
                }
            }
        });
    }

    public sealed record ComponentInfo(
        PartialTypeInfo PartialTypeInfo,
        EquatableArray<FieldInfo> Fields,
        bool Dirty,
        bool ShutdownEvent,
        bool NotComponent,
        Location Location);

    public sealed record FieldInfo(string Name, bool Nullable, bool Invalid, bool DictionaryKey, bool DictionaryValue, bool Collection, Location Location);

    public sealed record AllFieldInfo(string Name, string ParentDisplayName, Location Location);
}
