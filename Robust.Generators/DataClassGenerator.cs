using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Robust.Generators
{
    [Generator]
    public class DataClassGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new AutoDataClassRegistrationReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if(!(context.SyntaxReceiver is AutoDataClassRegistrationReceiver receiver)) return;

            var comp = (CSharpCompilation)context.Compilation;

            INamedTypeSymbol ResolveRegistration(ClassDeclarationSyntax cl)
            {
                return comp.GetSemanticModel(cl.SyntaxTree).GetDeclaredSymbol(cl);
            }

            var allResolvedClasses = receiver.AllClasses
                .Select(ResolveRegistration)
                .ToList();

            var implicitDataClassForInheritorsAttribute = comp.GetTypeByMetadataName("Robust.Shared.Serialization.Manager.Attributes.ImplicitDataClassForInheritorsAttribute");
            var dataClassAttribute =
                comp.GetTypeByMetadataName("Robust.Shared.Serialization.Manager.Attributes.DataClassAttribute");
            var dataFieldAttribute = new[]
            {
                comp.GetTypeByMetadataName("Robust.Shared.Serialization.Manager.Attributes.DataFieldAttribute"),
                comp.GetTypeByMetadataName("Robust.Shared.Serialization.Manager.Attributes.DataFieldWithFlagAttribute"),
                comp.GetTypeByMetadataName("Robust.Shared.Serialization.Manager.Attributes.DataFieldWithConstantAttribute")
            };


            bool IsAutoDataReg(INamedTypeSymbol namedTypeSymbol)
            {
                return namedTypeSymbol.GetAttribute(dataClassAttribute) != null ||
                       HasAnnotatedBaseType(namedTypeSymbol);
            }

            bool HasAnnotatedBaseType(INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol == null) return false;
                if (namedTypeSymbol.GetAttribute(implicitDataClassForInheritorsAttribute) != null) return true;
                return HasAnnotatedBaseType(namedTypeSymbol.BaseType);
            }

            //resolve autodata registrations (we need them to validate the customdataclasses)
            var autoDataRegistrations = allResolvedClasses.Where(IsAutoDataReg).RemoveDuplicates().ToList();

            var customDataClassRegistrations = receiver.CustomDataClassRegistrations.Select(ResolveRegistration)
                .Where(sym => sym.GetAttribute(dataClassAttribute)?.ConstructorArguments[0].Value != null).RemoveDuplicates()
                .ToDictionary(sym => sym,
                    sym => (INamedTypeSymbol) (sym.GetAttribute(dataClassAttribute).ConstructorArguments[0].Value));

            string ResolveParentDataClass(INamedTypeSymbol typeS, bool skipFirstCustom = false)
            {
                typeS = typeS.ConstructedFrom;//todo Paul: properly do generics(?)

                if (!skipFirstCustom && customDataClassRegistrations.TryGetValue(typeS, out var customDataClass))
                    return $"{customDataClass.ContainingNamespace}.{customDataClass.Name}";

                var metadataName = $"{typeS.ContainingNamespace}.{typeS.Name}_AUTODATA";
                if (autoDataRegistrations.Any(sym => SymbolEqualityComparer.Default.Equals(sym, typeS)) ||
                    comp.GetTypeByMetadataName(metadataName) != null)
                {
                    return metadataName;
                }

                if (typeS.BaseType == null)
                    return null;

                return ResolveParentDataClass(typeS.BaseType);
            }

            //adding autoinheritance for custom data classes
            foreach (var customDataClassRegistration in customDataClassRegistrations)
            {
                string shouldInherit = ResolveParentDataClass(customDataClassRegistration.Key, true) ?? "Robust.Shared.Serialization.Manager.DataClass";

                context.AddSource($"{customDataClassRegistration.Value.ContainingNamespace.ToString()}.{customDataClassRegistration.Value.Name}_INHERIT.g.cs",
                    SourceText.From(
                        GenerateCustomDataClassInheritanceCode(customDataClassRegistration.Value.Name,
                            customDataClassRegistration.Value.ContainingNamespace.ToString(), shouldInherit),
                        Encoding.UTF8));
            }

            //generate all autodata classes
            foreach (var symbol in autoDataRegistrations)
            {
                var fields = new List<string>();
                foreach (var member in symbol.GetMembers())
                {
                    foreach (var dataFieldAttributeSymbol in dataFieldAttribute)
                    {
                        var attribute = member.GetAttribute(dataFieldAttributeSymbol);
                        if(attribute == null) continue;

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

                        var fieldStr = $"[{dataFieldAttributeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(";
                        var parseFault = false;
                        for (var i = 0; i < attribute.ConstructorArguments.Length; i++)
                        {
                            var typedConstant = attribute.ConstructorArguments[i];
                            switch (typedConstant.Value)
                            {
                                case null:
                                    fieldStr += "null";
                                    break;
                                case string strVal:
                                    fieldStr += $"\"{strVal}\"";
                                    break;
                                case char charVal:
                                    fieldStr += $"'{charVal}'";
                                    break;
                                case bool boolVal:
                                    fieldStr += boolVal ? "true" : "false";
                                    break;
                                case int _:
                                case uint _:
                                case short _:
                                case ushort _:
                                case float _:
                                case double _:
                                case decimal _:
                                case byte _:
                                case sbyte _:
                                case long _:
                                case ulong _:
                                    fieldStr += typedConstant.Value.ToString();
                                    break;
                                case INamedTypeSymbol typeVal:
                                    fieldStr +=
                                        $"typeof({typeVal.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
                                    break;
                                default:
                                    context.ReportDiagnostic(Diagnostic.Create(
                                        Diagnostics.UnsupportedValue,
                                        member.Locations.First()));
                                    parseFault = true;
                                    break;
                            }
                            if(parseFault) break;

                            if(i < attribute.ConstructorArguments.Length - 1)
                                fieldStr += ", ";
                        }

                        if(parseFault) continue;

                        fieldStr += ")]\npublic readonly ";
                        switch (member)
                        {
                            case IFieldSymbol fieldSymbol:
                                var ftypeStr = fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                fieldStr += (ftypeStr.EndsWith("?") ? ftypeStr : ftypeStr+"?" )+" ";
                                fieldStr += fieldSymbol.Name+"_field;";
                                break;
                            case IPropertySymbol propertySymbol:
                                var ptypeStr = propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                fieldStr += (ptypeStr.EndsWith("?") ? ptypeStr : ptypeStr+"?" )+" ";
                                fieldStr += propertySymbol.Name+"_field;";
                                break;
                        }

                        fields.Add(fieldStr);
                    }
                }

                var name = $"{symbol.Name}_AUTODATA";
                var @namespace = symbol.ContainingNamespace.ToString();

                var inheriting = ResolveParentDataClass(symbol.BaseType) ?? "Robust.Shared.Serialization.Manager.DataClass";

                context.AddSource($"{@namespace}.{name}.g.cs",
                    SourceText.From(GenerateCode(name, @namespace, inheriting, fields), Encoding.UTF8));
            }
        }

        private string GenerateCustomDataClassInheritanceCode(string name, string @namespace, string inheriting)
        {
            return $@"namespace {@namespace} {{
    public partial class {name} : {inheriting}, Robust.Shared.Serialization.ISerializationHooks {{}}
}}
";
        }

        private string GenerateCode(string name, string @namespace, string inheriting, List<string> fields)
        {
            var code = $@"#nullable enable
namespace {@namespace} {{
    public class {name} : {inheriting} {{
";
            foreach (var field in fields)
            {
                code += $@"
        {field}";
            }

            code += @"
    }
}";
            return code;
        }
    }
}
