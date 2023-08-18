using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Robust.Serialization.Generator.CustomSerializerType;

namespace Robust.Serialization.Generator;

[Generator]
public class Generator : IIncrementalGenerator
{
    private const string DataDefinitionNamespace = "Robust.Shared.Serialization.Manager.Attributes.DataDefinitionAttribute";
    private const string ImplicitDataDefinitionNamespace = "Robust.Shared.Serialization.Manager.Attributes.ImplicitDataDefinitionForInheritorsAttribute";
    private const string DataFieldBaseNamespace = "Robust.Shared.Serialization.Manager.Attributes.DataFieldBaseAttribute";
    private const string ComponentNamespace = "Robust.Shared.GameObjects.Component";
    private const string ComponentInterfaceNamespace = "Robust.Shared.GameObjects.IComponent";
    private const string TypeCopierInterfaceNamespace = "Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeCopier";
    private const string TypeCopyCreatorInterfaceNamespace = "Robust.Shared.Serialization.TypeSerializers.Interfaces.ITypeCopyCreator";

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

                    // TODO mute obsolete serialization manager warnings
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
    {{GetCopyConstructor(definition)}}

    {{GetCopyMethod(definition, sourceContext)}}

    {{GetInstantiator(definition)}}
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

        var abstractKeyword = symbol.IsAbstract ? "abstract " : string.Empty;
        var typeName = GetGenericTypeName(symbol);
        return $"{access} {abstractKeyword}partial {typeKeyword} {typeName}";
    }

    private static string GetGenericTypeName(ITypeSymbol symbol)
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

     private static string GetCopyConstructor(DataDefinition definition)
     {
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

    private static string GetCopyMethod(DataDefinition definition, SourceProductionContext context)
    {
        var builder = new StringBuilder();

        builder.AppendLine($$"""
                             public void Copy(ref {{definition.GenericTypeName}} target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                             {
                             """);

        CopyDataFields(builder, definition, context);

        builder.AppendLine("}");

        if (ImplementsInterface(definition.Type, ComponentInterfaceNamespace))
        {
            builder.AppendLine($$"""
                                 public override void Copy(ref IComponent target, ISerializationManager serialization, SerializationHookContext hookCtx, ISerializationContext? context = null)
                                 {
                                     base.Copy(ref target, serialization, hookCtx, context);
                                     var comp = ({{definition.GenericTypeName}}) target;
                                     Copy(ref comp, serialization, hookCtx, context);
                                     target = comp;
                                 }
                                 """);
        }

        return builder.ToString();
    }

    private static string GetInstantiator(DataDefinition definition)
    {
        var builder = new StringBuilder();
        var modifiers = string.Empty;

        if (definition.Type.IsAbstract)
        {
            modifiers += "abstract ";
        }

        if (definition.Type.BaseType is { } baseType)
        {
            if (IsDataDefinition(baseType) || baseType.ToDisplayString() == ComponentNamespace)
            {
                modifiers += "override ";
            }
        }

        if (modifiers == string.Empty && definition.Type.IsReferenceType && !definition.Type.IsSealed)
            modifiers = "virtual ";

        if (definition.Type.IsAbstract)
        {
            builder.AppendLine($"public {modifiers}{definition.GenericTypeName} Instantiate();");
        }
        else
        {
            builder.AppendLine($$"""
                                 public {{modifiers}}{{definition.GenericTypeName}} Instantiate()
                                 {
                                     return new {{definition.GenericTypeName}}();
                                 }
                                 """);
        }


        return builder.ToString();
    }

    private static bool IsDataDefinition(ITypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == DataDefinitionNamespace)
                return true;
        }

        var baseType = type.BaseType;
        while (baseType != null)
        {
            foreach (var attribute in baseType.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == ImplicitDataDefinitionNamespace)
                    return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static bool IsNullableType(ITypeSymbol type)
    {
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
            return true;

        if (type.OriginalDefinition.ToDisplayString() == "System.Nullable<T>")
            return true;

        return false;
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

    private static bool NeedsEmptyConstructor(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named)
            return false;

        if (named.InstanceConstructors.Length == 0)
            return true;

        foreach (var constructor in named.InstanceConstructors)
        {
            if (constructor.Parameters.Length == 0 && !constructor.IsImplicitlyDeclared)
                return false;
        }

        return true;
    }

    private static bool ImplementsInterface(ITypeSymbol type, string interfaceName)
    {
        foreach (var @interface in type.AllInterfaces)
        {
            if (@interface.ToDisplayString().Contains(interfaceName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsReadOnlyMember(ISymbol member)
    {
        if (member is IFieldSymbol field)
        {
            return field.IsReadOnly;
        }
        else if (member is IPropertySymbol property)
        {
            return property.SetMethod == null;
        }

        return false;
    }

    private static bool IsDataField(ISymbol member, out ITypeSymbol type, out AttributeData attribute)
    {
        // TODO data records
        if (member is IFieldSymbol field)
        {
            foreach (var attr in field.GetAttributes())
            {
                if (attr.AttributeClass != null && Inherits(attr.AttributeClass, DataFieldBaseNamespace))
                {
                    type = field.Type;
                    attribute = attr;
                    return true;
                }
            }
        }
        else if (member is IPropertySymbol property)
        {
            foreach (var attr in property.GetAttributes())
            {
                if (attr.AttributeClass != null && Inherits(attr.AttributeClass, DataFieldBaseNamespace))
                {
                    type = property.Type;
                    attribute = attr;
                    return true;
                }
            }
        }

        type = null!;
        attribute = null!;
        return false;
    }

    private static bool Inherits(ITypeSymbol type, string parent)
    {
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.ToDisplayString() == parent)
                return true;

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static void CopyDataFields(StringBuilder builder, DataDefinition definition, SourceProductionContext context)
    {
        var definitionVarName = "___definition";
        var definitionIsNullable = definition.Type.NullableAnnotation == NullableAnnotation.Annotated;
        var definitionNullableOverride = definition.Type.IsReferenceType && !definitionIsNullable ? ", true" : string.Empty;

        builder.AppendLine($$"""
                             if (serialization.TryGetCopierOrCreator<{{definition.GenericTypeName}}>(out var {{definitionVarName}}Copier, out var {{definitionVarName}}CopyCreator, context))
                             {
                                 if ({{definitionVarName}}Copier != null)
                                 {
                                     serialization.CopyTo<{{definition.GenericTypeName}}>({{definitionVarName}}Copier, this, ref target, hookCtx, context{{definitionNullableOverride}});
                                 }
                                 else
                                 {
                                     target = {{definitionVarName}}CopyCreator!.CreateCopy(serialization, this, IoCManager.Instance!, hookCtx, context)!;
                                 }

                                 return;
                             }
                             """);

        var structCopier = new StringBuilder();
        foreach (var field in definition.Fields)
        {
            if (IsReadOnlyMember(field.Symbol))
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
                                     if (serialization.TryGetCopierOrCreator<{{typeName}}>(out var {{name}}Copier, out var {{name}}CopyCreator, context))
                                     {
                                     """);

                if (isClass)
                {
                    builder.AppendLine($$"""
                                             if ({{name}} == null)
                                             {
                                                 {{tempVarName}} = null!;
                                             }
                                             else
                                             {
                                         """);
                }

                builder.AppendLine($$"""
                                             if ({{name}}Copier != null)
                                             {
                                                 {{typeName}} temp = default!;
                                                 serialization.CopyTo<{{typeName}}>({{name}}Copier, {{name}}, ref temp, hookCtx, context{{nullableOverride}});
                                                 {{tempVarName}} = temp!;
                                             }
                                             else
                                             {
                                                 {{tempVarName}} = {{name}}CopyCreator!.CreateCopy(serialization, {{name}}!, IoCManager.Instance!, hookCtx, context)!;
                                             }
                                     """);

                if (isClass)
                {
                    builder.AppendLine("}");
                }

                builder.AppendLine("""
                                   }
                                   else
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
    }
}
