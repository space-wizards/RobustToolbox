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
    private const string TypeCopierInterfaceNamespace = "Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeCopier";
    private const string TypeCopyCreatorInterfaceNamespace = "Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeCopyCreator";
    private const string SerializationHooksNamespace = "Robust.Shared.Serialization.ISerializationHooks";

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
                    }

                    var namespaceString = symbol.ContainingNamespace.IsGlobalNamespace
                        ? string.Empty
                        : $"namespace {symbol.ContainingNamespace.ToDisplayString()};";

                    var containingType = symbol.ContainingType;
                    while (containingType != null)
                    {
                        containingTypes.Push(containingType);
                        containingType = containingType.ContainingType;
                    }

                    var nonPartial = false;
                    var containingTypesBuilder = new StringBuilder();
                    var containingTypesClosing = new StringBuilder();
                    foreach (var parent in containingTypes)
                    {
                        var syntax = (ClassDeclarationSyntax) parent.DeclaringSyntaxReferences[0].GetSyntax();
                        if (syntax.Modifiers.IndexOf(SyntaxKind.PartialKeyword) == -1)
                        {
                            sourceContext.ReportDiagnostic(Diagnostic.Create(NestedDataDefinitionPartialRule, syntax.Keyword.GetLocation(), parent.Name, symbol.Name));
                            nonPartial = true;
                            continue;
                        }

                        containingTypesBuilder.AppendLine($"{GetPartialTypeDefinitionLine(parent)}\n{{");
                        containingTypesClosing.AppendLine("}");
                    }

                    if (nonPartial)
                        continue;

                    var definition = GetDataFields(symbol);

                    builder.AppendLine($$"""
#nullable enable
using Robust.Shared.Analyzers;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
#pragma warning disable CS0618 // Type or member is obsolete

{{namespaceString}}

{{containingTypesBuilder}}

[RobustAutoGenerated]
{{GetPartialTypeDefinitionLine(symbol)}} : ISerializationGenerated<{{definition.GenericTypeName}}>
{
    {{GetConstructor(definition)}}

    {{GetCopyMethods(definition, sourceContext)}}

    {{GetInstantiators(definition)}}
}

{{containingTypesClosing}}
""");

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
                    if (ImplementsInterface(customSerializer, TypeCopierInterfaceNamespace, out _))
                    {
                        fields.Add(new DataField(member, type, (customSerializer, Copier)));
                        continue;
                    }
                    else if (ImplementsInterface(customSerializer, TypeCopyCreatorInterfaceNamespace, out _))
                    {
                        fields.Add(new DataField(member, type, (customSerializer, CopyCreator)));
                        continue;
                    }
                }

                fields.Add(new DataField(member, type, null));
            }
        }

        var typeName = GetGenericTypeName(definition);
        var hasHooks = ImplementsInterface(definition, SerializationHooksNamespace, out _);

        return new DataDefinition(definition, typeName, fields, hasHooks);
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
                                  #pragma warning disable CS8618
                                  public {{definition.Type.Name}}()
                                  #pragma warning enable CS8618
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
        var overrideKeyword = string.Empty;
        var baseCall = string.Empty;
        var baseCopy = string.Empty;
        var baseType = definition.Type.BaseType;

        if (baseType != null && IsDataDefinition(definition.Type.BaseType))
        {
            overrideKeyword = "override ";
            var baseName = baseType.ToDisplayString();
            baseCall = $"""
                        var definitionCast = ({baseName}) target;
                        base.InternalCopy(ref definitionCast, serialization, hookCtx, context);
                        target = ({definition.GenericTypeName}) definitionCast;
                        """;

             baseCopy = $$"""
                          public override void Copy(ref {{baseName}} target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                          {
                              var cast = ({{definition.GenericTypeName}}) target;
                              Copy(ref cast, serialization, hookCtx, context);
                              target = cast!;
                          }

                          public override void Copy(ref object target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                          {
                              var cast = ({{definition.GenericTypeName}}) target;
                              Copy(ref cast, serialization, hookCtx, context);
                              target = cast!;
                          }
                          """;
        }
        else
        {
            baseCopy = $$"""
                         public {{modifiers}} void Copy(ref object target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                         {
                             var cast = ({{definition.GenericTypeName}}) target;
                             Copy(ref cast, serialization, hookCtx, context);
                             target = cast!;
                         }
                         """;
        }

        builder.AppendLine($$"""
                             public {{modifiers}} void InternalCopy(ref {{definition.GenericTypeName}} target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                             {
                                {{baseCall}}
                                {{CopyDataFields(definition, context)}}
                             }

                             public {{modifiers}} void Copy(ref {{definition.GenericTypeName}} target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                             {
                                 InternalCopy(ref target, serialization, hookCtx, context);
                             }

                             {{baseCopy}}
                             """);

        foreach (var @interface in GetImplicitDataDefinitionInterfaces(definition.Type, true))
        {
            var interfaceModifiers = baseType != null && baseType.AllInterfaces.Contains(@interface, SymbolEqualityComparer.Default)
                ? "override "
                : modifiers;
            var interfaceName = @interface.ToDisplayString();

            builder.AppendLine($$"""
                                 public {{interfaceModifiers}} void InternalCopy(ref {{interfaceName}} target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                                 {
                                     var def = ({{definition.GenericTypeName}}) target;
                                     Copy(ref def, serialization, hookCtx, context);
                                     target = def;
                                 }

                                 public {{interfaceModifiers}} void Copy(ref {{interfaceName}} target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
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

        if (definition.Type.IsAbstract)
        {
            builder.AppendLine($"""
                                public abstract {modifiers} {definition.GenericTypeName} Instantiate();
                                """);
        }
        else
        {
            builder.AppendLine($$"""
                                 public {{modifiers}} {{definition.GenericTypeName}} Instantiate()
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

        builder.AppendLine($"""
if (serialization.TryCustomCopy(this, ref target, hookCtx, {definition.HasHooks.ToString().ToLower()}, context))
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
            if (IsMultidimensionalArray(type))
            {
                typeName = typeName.Replace("*", "");
            }

            var isNullableValueType = IsNullableValueType(type);
            var nonNullableTypeName = type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();
            if (isNullableValueType)
            {
                nonNullableTypeName = typeName.Substring(0, typeName.Length - 1);
            }

            var isClass = type.IsReferenceType || type.SpecialType == SpecialType.System_String;
            var isNullable = type.NullableAnnotation == NullableAnnotation.Annotated;
            var nullableOverride = isClass && !isNullable ? ", true" : string.Empty;
            var name = field.Symbol.Name;
            var tempVarName = $"{name}Temp";
            var nullableValue = isNullableValueType ? ".Value" : string.Empty;

            if (field.CustomSerializer is { Serializer: var serializer, Type: var serializerType })
            {
                if (isClass || isNullableValueType)
                {
                    builder.AppendLine($$"""
                                         if ({{name}} != null)
                                         {
                                         """);
                }

                var serializerName = serializer.ToDisplayString();
                switch (serializerType)
                {
                    case Copier:
                        CopyToCustom(
                            builder,
                            nonNullableTypeName,
                            serializerName,
                            tempVarName,
                            name,
                            isNullable,
                            isClass,
                            isNullableValueType
                        );
                        break;
                    case CopyCreator:
                        CreateCopyCustom(
                            builder,
                            name,
                            nonNullableTypeName,
                            serializerName,
                            nullableValue,
                            nullableOverride
                        );
                        break;
                }

                if (isClass || isNullableValueType)
                {
                    builder.AppendLine("}");
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

                var instantiator = string.Empty;
                if (!type.IsAbstract &&
                    HasEmptyPublicConstructor(type) &&
                    (type.IsReferenceType || IsNullableType(type)))
                {
                    instantiator = $"{tempVarName} = new();";
                }

                var hasHooks = ImplementsInterface(type, SerializationHooksNamespace, out _) || !type.IsSealed;
                builder.AppendLine($$"""
                                     {{instantiator}}
                                     if (!serialization.TryCustomCopy(this.{{name}}, ref {{tempVarName}}, hookCtx, {{hasHooks.ToString().ToLower()}}, context))
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
                                         {{name}}{{nullability}}.Copy(ref temp, serialization, hookCtx, context);
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

    private static void CopyToCustom(
        StringBuilder builder,
        string typeName,
        string serializerName,
        string tempVarName,
        string varName,
        bool isNullable,
        bool isClass,
        bool isNullableValueType)
    {
        var newTemp = isNullable && isClass ? $"{tempVarName} ??= new();" : string.Empty;
        var nullableOverride = isClass ? ", true" : string.Empty;
        var nullableValue = isNullableValueType ? ".Value" : string.Empty;

        builder.AppendLine($$"""
                             {{typeName}} {{tempVarName}} = default!;
                             {{newTemp}}
                             serialization.CopyTo<{{typeName}}, {{serializerName}}>(this.{{varName}}{{nullableValue}}, ref {{tempVarName}}, hookCtx, context{{nullableOverride}});
                             target.{{varName}} = {{tempVarName}};
                             """);
    }

    private static void CreateCopyCustom(
        StringBuilder builder,
        string varName,
        string nonNullableTypeName,
        string serializerName,
        string nullableValue,
        string nullableOverride)
    {
        builder.AppendLine($$"""
                             target.{{varName}} = serialization.CreateCopy<{{nonNullableTypeName}}, {{serializerName}}>(this.{{varName}}{{nullableValue}}, hookCtx, context{{nullableOverride}});
                             """);
    }
}
