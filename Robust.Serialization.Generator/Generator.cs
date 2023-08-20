using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Robust.Serialization.Generator.CustomSerializerType;
using static Robust.Serialization.Generator.Types;

namespace Robust.Serialization.Generator;

[Generator]
public class Generator : IIncrementalGenerator
{
    private const string ComponentNamespace = "Robust.Shared.GameObjects.Component";
    private const string ComponentInterfaceNamespace = "Robust.Shared.GameObjects.IComponent";
    private const string TypeCopierInterfaceNamespace = "Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeCopier";
    private const string TypeCopyCreatorInterfaceNamespace = "Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeCopyCreator";

    private static readonly DiagnosticDescriptor DataDefinitionPartialRule = new(
        Diagnostics.IdDataDefinitionPartial,
        "Type must be partial",
        "Type {0} is a DataDefinition but is not partial.",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure to mark any type that is a data definition as partial."
    );

    private static readonly DiagnosticDescriptor NestedDataDefinitionPartialRule = new(
        Diagnostics.IdNestedDataDefinitionPartial,
        "Type must be partial",
        "Type {0} contains nested data definition {1} but is not partial.",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure to mark any type containing a nested data definition as partial."
    );

    private static readonly DiagnosticDescriptor DataFieldWritableRule = new(
        Diagnostics.IdDataFieldWritable,
        "Data field must not be readonly",
        "Field {0} in data definition {1} is marked as a DataField but is readonly.",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure to add a setter or remove the readonly modifier."
    );

    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        IncrementalValuesProvider<TypeDeclarationSyntax> dataDefinitions = initContext.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is TypeDeclarationSyntax,
            static (context, _) =>
            {
                var type = (TypeDeclarationSyntax) context.Node;
                var symbol = (ITypeSymbol) context.SemanticModel.GetDeclaredSymbol(type)!;
                return IsDataDefinition(symbol) ? type : null;
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

                    var namespaceString = symbol.ContainingNamespace.IsGlobalNamespace
                        ? string.Empty
                        : $"namespace {symbol.ContainingNamespace.ToDisplayString()};";

                    builder.AppendLine($"""
#nullable enable
using Robust.Shared.Analyzers;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
#pragma warning disable CS0618 // Type or member is obsolete

{namespaceString}
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
[RobustAutoGenerated]
{{GetPartialTypeDefinitionLine(symbol)}} : ISerializationGenerated<{{definition.GenericTypeName}}>
{
    {{GetConstructor(definition)}}

    {{GetCopyMethods(definition, sourceContext)}}

    {{GetInstantiators(definition)}}
}
"""
                    );

                    for (var i = 0; i < containingTypes.Count; i++)
                    {
                        builder.AppendLine("}");
                    }

                    var symbolName = symbol
                        .ToDisplayString()
                        .Replace('<', '{')
                        .Replace('>', '}');

                    var sourceText = CSharpSyntaxTree
                        .ParseText(builder.ToString())
                        .GetRoot()
                        .NormalizeWhitespace()
                        .ToFullString();

                    sourceContext.AddSource($"{symbolName}.g.cs", sourceText);
                }
            }
        );
    }

    private static DataDefinition GetDataFields(ITypeSymbol definition)
    {
        var fields = new List<DataField>();

        foreach (var member in definition.GetMembers())
        {
            if (member is not IFieldSymbol && member is not IPropertySymbol)
                continue;

            if (member.IsStatic)
                continue;

            if (IsDataField(member, out var type, out var attribute))
            {
                if (attribute.ConstructorArguments.FirstOrDefault(arg => arg.Kind == TypedConstantKind.Type).Value is INamedTypeSymbol customSerializer)
                {
                    if (ImplementsInterface(customSerializer, TypeCopierInterfaceNamespace))
                    {
                        fields.Add(new DataField(member, type, (customSerializer, Copier)));
                        continue;
                    }
                    else if (ImplementsInterface(customSerializer, TypeCopyCreatorInterfaceNamespace))
                    {
                        fields.Add(new DataField(member, type, (customSerializer, CopyCreator)));
                        continue;
                    }
                }

                fields.Add(new DataField(member, type, null));
            }
        }

        var typeName = GetGenericTypeName(definition);
        return new DataDefinition(definition, typeName, fields);
    }

     private static string GetConstructor(DataDefinition definition)
     {
         if (definition.Type.TypeKind == TypeKind.Interface)
             return string.Empty;

         var builder = new StringBuilder();

         if (NeedsEmptyConstructor(definition.Type))
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

    private static string GetCopyMethods(DataDefinition definition, SourceProductionContext context)
    {
        var builder = new StringBuilder();

        var modifiers = IsVirtualClass(definition.Type) ? "virtual " : string.Empty;
        var baseCall = string.Empty;
        if (definition.Type.BaseType is { } baseType &&
            IsImplicitDataDefinition(baseType))
        {
            var baseName = baseType.ToDisplayString();
            baseCall = $"""
                        var definitionCast = ({baseName}) target;
                        base.InternalCopy(ref definitionCast, serialization, hookCtx, context);
                        target = ({definition.GenericTypeName}) definitionCast;
                        """;

             builder.AppendLine($$"""
                                   public override void Copy(ref {{baseName}} target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                                  {
                                      var cast = ({{definition.GenericTypeName}}) target;
                                      ((ISerializationGenerated<{{definition.GenericTypeName}}>) this).Copy(ref cast, serialization, hookCtx, context);
                                      target = cast!;
                                  }
                                  """);
        }

        builder.AppendLine($$"""
                             public {{modifiers}}void InternalCopy(ref {{definition.GenericTypeName}} target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                             {
                                {{baseCall}}
                                {{CopyDataFields(definition, context)}}
                             }
                             """);


        builder.AppendLine($$"""
                             public {{modifiers}}void Copy(ref {{definition.GenericTypeName}} target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                             {
                                 InternalCopy(ref target, serialization, hookCtx, context);
                             }
                             """);

        builder.AppendLine($$"""
                             public {{modifiers}}void Copy(ref object target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                             {
                                 var cast = ({{definition.GenericTypeName}}) target;
                                 ((ISerializationGenerated<{{definition.GenericTypeName}}>) this).Copy(ref cast, serialization, hookCtx, context);
                                 target = cast!;
                             }
                             """);

        foreach (var @interface in GetImplicitDataDefinitionInterfaces(definition.Type, true))
        {
            var interfaceName = @interface.ToDisplayString();

            builder.AppendLine($$"""
                                 public {{modifiers}}void InternalCopy(ref {{interfaceName}} target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                                 {
                                     var def = ({{definition.GenericTypeName}}) target;
                                     ((ISerializationGenerated<{{definition.GenericTypeName}}>) this).Copy(ref def, serialization, hookCtx, context);
                                     target = def;
                                 }

                                 public {{modifiers}}void Copy(ref {{interfaceName}} target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                                 {
                                     InternalCopy(ref target, serialization, hookCtx, context);
                                 }
                                 """);
        }

        return builder.ToString();
    }

    private static string GetInstantiators(DataDefinition definition)
    {
        var builder = new StringBuilder();
        var modifiers = string.Empty;

        if (definition.Type.BaseType is { } baseType && IsDataDefinition(baseType))
            modifiers = "override ";
        else if (!definition.Type.IsAbstract && IsVirtualClass(definition.Type))
            modifiers = "virtual ";

        // TODO skip locals init
        if (definition.Type.IsAbstract)
        {
//             foreach (var @interface in interfaces)
//             {
//                 var interfaceName = @interface.ToDisplayString();
//                 builder.AppendLine($"""
//                                     public abstract {interfaceName} Instantiate();
//                                     """);
//             }

            builder.AppendLine($"""
                                public abstract {modifiers}{definition.GenericTypeName} Instantiate();
                                """);
        }
        else
        {
//             foreach (var @interface in interfaces)
//             {
//                 var interfaceName = @interface.ToDisplayString();
//                 builder.AppendLine($$"""
//                                     public {{overrideKeyword}}{{interfaceName}} Instantiate({{interfaceName}}? _ = default)
//                                     {
//                                         return new {{definition.GenericTypeName}}();
//                                     }
//                                     """);
//             }

            builder.AppendLine($$"""
                                 public {{modifiers}}{{definition.GenericTypeName}} Instantiate()
                                 {
                                     return new {{definition.GenericTypeName}}();
                                 }
                                 """);
        }

        foreach (var @interface in GetImplicitDataDefinitionInterfaces(definition.Type, false))
        {
            var interfaceName = @interface.ToDisplayString();
            builder.AppendLine($$"""
                                 {{interfaceName}} {{interfaceName}}.Instantiate()
                                 {
                                     return Instantiate();
                                 }

                                 {{interfaceName}} ISerializationGenerated<{{interfaceName}}>.Instantiate()
                                 {
                                     return Instantiate();
                                 }
                                 """);
        }

        return builder.ToString();
    }

    private static StringBuilder CopyDataFields(DataDefinition definition, SourceProductionContext context)
    {
        var builder = new StringBuilder();

        builder.AppendLine("""
if (serialization.TryCustomCopy(this, ref target, hookCtx, context))
    return;
""");

        var structCopier = new StringBuilder();
        foreach (var field in definition.Fields)
        {
            if (IsReadOnlyMember(definition.Type, field.Symbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(DataFieldWritableRule, field.Symbol.Locations.First(),
                    field.Symbol.Name, definition.Type.Name));
                continue;
            }

            var type = field.Type;
            var typeName = type.ToDisplayString();
            if (type is IArrayTypeSymbol { Rank: > 1 })
            {
                typeName = typeName.Replace("*", "");
            }

            var isClass = type.IsReferenceType || type.SpecialType == SpecialType.System_String;
            var isNullable = type.NullableAnnotation == NullableAnnotation.Annotated;
            var nullableOverride = isClass && !isNullable ? ", true" : string.Empty;
            var name = field.Symbol.Name;
            var tempVarName = $"{name}Temp";

            if (field.CustomSerializer is { Serializer: var serializer, Type: var serializerType })
            {
                switch (serializerType)
                {
                    case Copier:
                        builder.AppendLine($$"""
                                             var {{name}}Temp = target.{{name}};
                                             serialization.CopyTo<{{typeName}}, {{serializer.ToDisplayString()}}>(this.{{name}}, ref {{name}}Temp, hookCtx, context{{nullableOverride}});
                                             target.{{name}} = {{name}}Temp;
                                             """);
                        break;
                    case CopyCreator:
                        builder.AppendLine($$"""
                                             target.{{name}} = serialization.CreateCopy<{{typeName}}, {{serializer.ToDisplayString()}}>(this.{{name}}, hookCtx, context{{nullableOverride}});
                                             """);
                        break;
                }
            }
            else
            {
                builder.AppendLine($$"""
                                     {{typeName}} {{tempVarName}} = default!;
                                     """);

                if (isClass)
                {
                    builder.AppendLine($$"""
                                         if ({{name}} != null)
                                         {
                                         """);
                }

                builder.AppendLine($$"""
                                     if (!serialization.TryCustomCopy(this.{{name}}, ref {{tempVarName}}, hookCtx, context))
                                     {
                                     """);

                if (CanBeCopiedByValue(type))
                {
                    builder.AppendLine($"{tempVarName} = {name};");
                }
                else if (IsDataDefinition(type) && !type.IsAbstract &&
                         type is not INamedTypeSymbol { TypeKind: TypeKind.Interface })
                {
                    var nullability = type.IsValueType ? string.Empty : "?";
                    var orNew = type.IsReferenceType
                        ? $" ?? {name}{nullability}.Instantiate()"
                        : string.Empty; // TODO nullable structs
                    var nullable = !type.IsValueType || IsNullableType(type);


                    builder.AppendLine($"var temp = {name}{orNew};");

                    if (nullable)
                    {
                        builder.AppendLine("""
                                           if (temp != null)
                                           {
                                           """);
                    }

                    builder.AppendLine($$"""
                                         ((ISerializationGenerated<{{typeName}}>?) {{name}}){{nullability}}.Copy(ref temp, serialization, hookCtx, context);
                                         {{tempVarName}} = temp;
                                         """);

                    if (nullable)
                    {
                        builder.AppendLine("}");
                    }
                }
                else
                {
                    builder.AppendLine($"{tempVarName} = serialization.CreateCopy({name}, hookCtx, context);");
                }

                builder.AppendLine("}");

                if (isClass)
                {
                    builder.AppendLine("}");
                }

                if (definition.Type.IsValueType)
                {
                    structCopier.AppendLine($"{name} = {tempVarName},");
                }
                else
                {
                    builder.AppendLine($"target.{name} = {tempVarName};");
                }
            }
        }

        if (definition.Type.IsValueType)
        {
            builder.AppendLine($$"""
                                target = target with
                                {
                                    {{structCopier}}
                                };
                                """);
        }

        return builder;
    }
}
