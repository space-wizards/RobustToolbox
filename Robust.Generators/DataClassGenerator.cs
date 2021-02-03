using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Robust.Generators
{
    [Generator]
    public class DataClassGenerator : ISourceGenerator
    {
        private bool NeedsDataClass(ITypeSymbol symbol, ITypeSymbol serializableAttrSymbol)
        {
            if (symbol is INamedTypeSymbol nsym && nsym.IsSerializable) return false;

            switch (symbol.BaseType?.SpecialType)
            {
                case SpecialType.System_Enum:
                    return false;
                case SpecialType.System_Collections_Generic_IList_T:
                case SpecialType.System_Collections_Generic_IReadOnlyList_T:
                case SpecialType.System_Nullable_T:
                    return NeedsDataClass(((INamedTypeSymbol) symbol).TypeArguments.First(), serializableAttrSymbol);
            }

            //TODO Paul: Make this work for dicts
            switch (symbol.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Char:
                case SpecialType.System_String:
                case SpecialType.System_Decimal:
                    return false;
                default:
                    return true;
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new AutoDataClassRegistrationReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if(!(context.SyntaxReceiver is AutoDataClassRegistrationReceiver receiver)) return;

            var comp = (CSharpCompilation)context.Compilation;
            var serializableAttrSymbol = comp.GetTypeByMetadataName("System.SerializableAttribute");
            //var iCompType = comp.GetTypeByMetadataName("Robust.Shared.Interfaces.GameObjects.IComponent");

            //resolve autodata registrations (we need the to validate the customdataclasses)
            var resolvedAutoDataRegistrations =
                receiver.Registrations.Select(cl => comp.GetSemanticModel(cl.SyntaxTree).GetDeclaredSymbol(cl)).ToImmutableHashSet();

            var resolvedCustomDataClasses = new Dictionary<ITypeSymbol, ITypeSymbol>();

            bool TryResolveCustomDataClass(ITypeSymbol typeSymbol, out ITypeSymbol customDataClass)
            {
                if (resolvedCustomDataClasses.TryGetValue(typeSymbol, out customDataClass))
                    return true;

                var arg = typeSymbol?.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.Name == "CustomDataClassAttribute")?.ConstructorArguments.FirstOrDefault();
                if (arg == null) return false;
                customDataClass = arg.Value.Value as ITypeSymbol;
                return customDataClass != null;
            }

            //resolve all custom dataclasses
            foreach (var classDeclarationSyntax in receiver.CustomDataClassRegistrations)
            {
                var symbol = comp.GetSemanticModel(classDeclarationSyntax.SyntaxTree)
                    .GetDeclaredSymbol(classDeclarationSyntax);

                if (!TryResolveCustomDataClass(symbol, out var customDataClass))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.FailedCustomDataClassAttributeResolve,
                        classDeclarationSyntax.GetLocation()));
                    continue;
                }

                string shouldInherit;
                if (resolvedAutoDataRegistrations.Any(r => SymbolEqualityComparer.Default.Equals(symbol, r)))
                {
                    shouldInherit = $"{symbol}_AUTODATA";
                }
                else
                {
                    shouldInherit = ResolveParentDataClass(symbol, true) ?? "Robust.Shared.Prototypes.DataClass";
                }
                context.AddSource($"{customDataClass.Name}_INHERIT.g.cs", SourceText.From(GenerateCustomDataClassInheritanceCode(customDataClass.Name, customDataClass.ContainingNamespace.ToString(), shouldInherit), Encoding.UTF8));

                resolvedCustomDataClasses.Add(symbol, customDataClass);
            }

            string ResolveParentDataClass(ITypeSymbol typeS, bool forCustom = false)
            {
                var typeSymbol = typeS;
                if (typeSymbol is INamedTypeSymbol tSym)
                {
                    typeSymbol = tSym.ConstructedFrom; //todo Paul: properly do generics
                }

                if (!forCustom && TryResolveCustomDataClass(typeSymbol, out var customDataClass))
                    return $"{customDataClass.ContainingNamespace}.{customDataClass.Name}";

                var metaName = $"{typeSymbol.ContainingNamespace}.{typeSymbol.Name}_AUTODATA";
                var dataClass = comp.GetTypeByMetadataName(metaName);
                if (dataClass != null || resolvedAutoDataRegistrations.Any(r => SymbolEqualityComparer.Default.Equals(r, typeSymbol))) return metaName;

                if(typeSymbol.BaseType == null)
                    return null;

                return ResolveParentDataClass(typeSymbol.BaseType);
            }

            T GetCtorArg<T>(ImmutableArray<TypedConstant> ctorArgs, int i)
            {
                try
                {
                    return (T) ctorArgs[i].Value;

                }
                catch
                {
                    return default;
                }
            }

            //generate all autodata classes
            foreach (var symbol in resolvedAutoDataRegistrations)
            {
                var fields = new List<FieldTemplate>();
                foreach (var member in symbol.GetMembers())
                {
                    var attribute = member.GetAttributes()
                        .FirstOrDefault(a => a.AttributeClass?.Name == "YamlFieldAttribute");
                    if(attribute == null) continue;
                    var fieldName = GetCtorArg<string>(attribute.ConstructorArguments, 0);
                    if (fieldName == null || !SyntaxFacts.IsValidIdentifier(GetFieldName(fieldName)))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.InvalidYamlTag,
                            member.Locations.First()));
                        continue;
                    }

                    var @readonly = GetCtorArg<bool>(attribute.ConstructorArguments, 1);
                    var flagType = GetCtorArg<ITypeSymbol>(attribute.ConstructorArguments, 2);
                    var constType = GetCtorArg<ITypeSymbol>(attribute.ConstructorArguments, 3);

                    ITypeSymbol type;
                    switch (member)
                    {
                        case IFieldSymbol fieldSymbol:
                            type = fieldSymbol.Type;
                            break;
                        case IPropertySymbol propertySymbol:
                            type = propertySymbol.Type;
                            break;
                        default:
                            context.ReportDiagnostic(Diagnostic.Create(
                                Diagnostics.InvalidYamlAttrTarget,
                                member.Locations.First()));
                            continue;
                    }

                    string typeString;
                    if (NeedsDataClass(type, serializableAttrSymbol))
                    {
                        typeString = ResolveParentDataClass(type);
                        if (typeString == null)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                Diagnostics.DataClassNotFound,
                                member.Locations.First()));
                            continue;
                        }
                    }
                    else
                    {
                        typeString = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    }

                    fields.Add(new FieldTemplate(fieldName, typeString, @readonly, flagType, constType));
                }

                var name = $"{symbol.Name}_AUTODATA";
                var @namespace = symbol.ContainingNamespace.ToString();

                var inheriting = ResolveParentDataClass(symbol.BaseType) ?? "Robust.Shared.Prototypes.DataClass";

                context.AddSource($"{name}.g.cs",
                    SourceText.From(GenerateCode(name, @namespace, inheriting, fields), Encoding.UTF8));
            }
        }

        private string GenerateCustomDataClassInheritanceCode(string name, string @namespace, string inheriting)
        {
            return $@"namespace {@namespace} {{
    public partial class {name} : {inheriting} {{}}
}}
";
        }

        private string GetFieldName(string fieldname) => $"{fieldname}_field";

        private string GenerateCode(string name, string @namespace, string inheriting, List<FieldTemplate> fields)
        {
            var code = $@"#nullable enable
using System;
using System.Linq;
using Robust.Shared.Serialization;
namespace {@namespace} {{
    public class {name} : {inheriting} {{

";

            //generate fields
            foreach (var field in fields)
            {
                code += $@"
        public {field.Type} {GetFieldName(field.Name)};";
            }

            //generate exposedata
            code += @"

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);";

            foreach (var field in fields)
            {
                code += $@"
            {(field.ReadOnly ? "if(serializer.Reading) " : "")}serializer.NullableDataField(ref {GetFieldName(field.Name)}, ""{field.Name}"", null";
                if (field.FlagType != default)
                {
                    code +=
                        $", withFormat: WithFormat.Flags<{field.FlagType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()";
                }else if (field.ConstantsType != default)
                {
                    code +=
                        $", withFormat: WithFormat.Constants<{field.ConstantsType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()";
                }
                code += ");";
            }
            code += @"
        }";

            //generate getvalue
            code += @"

        /// <inheritdoc />
        public override object? GetValue(string tag)
        {
            return tag switch
            {";

            foreach (var field in fields)
            {
                code += $@"
                ""{field.Name}"" => {GetFieldName(field.Name)},";
            }

            code += @"
                _ => base.GetValue(tag)
            };
        }";

            //generate setvalue
            code += @"

        /// <inheritdoc />
        public override void SetValue(string tag, object? value)
        {
            switch (tag)
            {";

            foreach (var field in fields)
            {
                code += $@"
                case ""{field.Name}"":
                    {GetFieldName(field.Name)} = ({field.Type})value;
                    break;";
            }
            code += @"
                default:
                    base.SetValue(tag, value);
                    break;
            }
        }";

            code += @"
    }
}";
            return code;
        }

        private struct FieldTemplate
        {
            public readonly string Name;
            public readonly string Type;
            public readonly bool ReadOnly;
            public readonly ITypeSymbol FlagType;
            public readonly ITypeSymbol ConstantsType;

            public FieldTemplate(string name, string type, bool readOnly, ITypeSymbol flagType, ITypeSymbol constantsType)
            {
                Name = name;
                ReadOnly = readOnly;
                FlagType = flagType;
                ConstantsType = constantsType;
                Type = type.EndsWith("?") ? type : $"{type}?";
            }
        }
    }
}
