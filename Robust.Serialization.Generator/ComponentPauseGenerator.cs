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
public sealed class ComponentPauseGenerator : IIncrementalGenerator
{
    private const string AutoGenerateComponentPauseAttributeName = "Robust.Shared.Analyzers.AutoGenerateComponentPauseAttribute";
    private const string AutoPausedFieldAttributeName = "Robust.Shared.Analyzers.AutoPausedFieldAttribute";
    private const string AutoNetworkFieldAttributeName = "Robust.Shared.Analyzers.AutoNetworkedFieldAttribute";
    // ReSharper disable once InconsistentNaming
    private const string IComponentTypeName = "Robust.Shared.GameObjects.IComponent";

    private static readonly DiagnosticDescriptor NotComponentDiagnostic = new(
        Diagnostics.IdComponentPauseNotComponent,
        "Class must be an IComponent to use AutoGenerateComponentPause",
        "Class '{0}' must implement IComponent to be used with [AutoGenerateComponentPause]",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor NoFieldsDiagnostic = new(
        Diagnostics.IdComponentPauseNoFields,
        "AutoGenerateComponentPause has no fields",
        "Class '{0}' has [AutoGenerateComponentPause] but has no fields or properties with [AutoPausedField]",
        "Usage",
        DiagnosticSeverity.Warning,
        true);

    private static readonly DiagnosticDescriptor NoParentAttributeDiagnostic = new(
        Diagnostics.IdComponentPauseNoParentAttribute,
        "AutoPausedField on type of field without AutoGenerateComponentPause",
        "Field '{0}' has [AutoPausedField] but its containing type does not have [AutoGenerateComponentPause]",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor WrongTypeAttributeDiagnostic = new(
        Diagnostics.IdComponentPauseWrongTypeAttribute,
        "AutoPausedField has wrong type",
        "Field '{0}' has [AutoPausedField] but is not of type TimeSpan",
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

                var dirty = AttributeHelper.GetNamedArgumentBool(syntaxContext.Attributes[0], "Dirty", false);

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
                    if (namedType.Name != "TimeSpan")
                    {
                        if (namedType is { Name: "Nullable", TypeArguments: [{Name: "TimeSpan"}] })
                        {
                            nullable = true;
                        }
                        else
                        {
                            invalid = true;
                        }
                    }

                    // If any pause field has [AutoNetworkedField], automatically mark it to dirty on unpause.
                    if (AttributeHelper.HasAttribute(member, AutoNetworkFieldAttributeName, out var _))
                        dirty = true;

                    fieldBuilder.Add(new FieldInfo(member.Name, nullable, invalid, member.Locations[0]));
                }

                return new ComponentInfo(
                    partialTypeInfo,
                    EquatableArray<FieldInfo>.FromImmutableArray(fieldBuilder.ToImmutable()),
                    dirty,
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

            var builder = new StringBuilder();

            builder.AppendLine("""
                // <auto-generated />

                using Robust.Shared.GameObjects;

                """);

            info.PartialTypeInfo.WriteHeader(builder);

            builder.AppendLine();
            builder.AppendLine("{");

            builder.AppendLine($$"""
                [RobustAutoGenerated]
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                public sealed class {{info.PartialTypeInfo.Name}}_AutoPauseSystem : EntitySystem
                {
                    public override void Initialize()
                    {
                        SubscribeLocalEvent<{{info.PartialTypeInfo.Name}}, EntityUnpausedEvent>(OnEntityUnpaused);
                    }

                    private void OnEntityUnpaused(EntityUid uid, {{info.PartialTypeInfo.Name}} component, ref EntityUnpausedEvent args)
                    {
                """);

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
                    builder.AppendLine($"""
                                if (component.{field.Name}.HasValue)
                                    component.{field.Name} = component.{field.Name}.Value + args.PausedTime;
                        """);
                }
                else
                {
                    builder.AppendLine($"        component.{field.Name} += args.PausedTime;");
                }

                anyValidField = true;
            }

            if (!anyValidField)
                return;

            if (info.Dirty)
                builder.AppendLine("        Dirty(uid, component);");

            builder.AppendLine("""
                    }
                }
                """);

            builder.AppendLine("}");

            info.PartialTypeInfo.WriteFooter(builder);

            productionContext.AddSource(info.PartialTypeInfo.GetGeneratedFileName(), builder.ToString());
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
        bool NotComponent,
        Location Location);

    public sealed record FieldInfo(string Name, bool Nullable, bool Invalid, Location Location);

    public sealed record AllFieldInfo(string Name, string ParentDisplayName, Location Location);
}
