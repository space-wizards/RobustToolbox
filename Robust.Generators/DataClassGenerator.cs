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
        private T GetCtorArg<T>(ImmutableArray<TypedConstant> ctorArgs, int i)
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

            var meansDataClassAttribute = comp.GetTypeByMetadataName("Robust.Shared.Prototypes.DataClasses.Attributes.MeansImplicitDataClassAttribute");
            var implicitDataClassForInheritorsAttribute = comp.GetTypeByMetadataName("Robust.Shared.Prototypes.DataClasses.Attributes.ImplicitDataClassForInheritorsAttribute");
            var dataClassAttribute =
                comp.GetTypeByMetadataName("Robust.Shared.Prototypes.DataClasses.Attributes.DataClassAttribute");
            var yamlFieldAttribute = comp.GetTypeByMetadataName("Robust.Shared.Prototypes.YamlFieldAttribute");

            var attributeSymbol = comp.GetTypeByMetadataName("System.Attribute");

            var dataClassAttributes = new List<INamedTypeSymbol>();
            foreach (var typeSymbol in allResolvedClasses)
            {
                var attr = typeSymbol.GetAttribute(meansDataClassAttribute);
                if(attr == null) continue;
                if (!attributeSymbol.AssignableFrom(typeSymbol))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.InvalidMeansImplicitDataClassAttributeAssigment,
                        typeSymbol.Locations.First()));
                    continue;
                }

                dataClassAttributes.Add(typeSymbol);
            }

            bool IsAutoDataReg(INamedTypeSymbol namedTypeSymbol)
            {
                return dataClassAttributes.Any(attr => namedTypeSymbol.GetAttribute(attr) != null) ||
                       HasAnnotatedBaseType(namedTypeSymbol);
            }

            bool HasAnnotatedBaseType(INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol == null) return false;
                if (namedTypeSymbol.GetAttribute(implicitDataClassForInheritorsAttribute) != null) return true;
                return HasAnnotatedBaseType(namedTypeSymbol.BaseType);
            }

            //resolve autodata registrations (we need them to validate the customdataclasses)
            var autoDataRegistrations = allResolvedClasses.Where(IsAutoDataReg).ToList();

            var customDataClassRegistrations = receiver.CustomDataClassRegistrations.Select(ResolveRegistration)
                .Where(sym => sym.GetAttribute(dataClassAttribute)?.ConstructorArguments.Length > 0)
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
                string shouldInherit = ResolveParentDataClass(customDataClassRegistration.Key, true) ?? "Robust.Shared.Prototypes.DataClass";

                context.AddSource($"{customDataClassRegistration.Value.Name}_INHERIT.g.cs",
                    SourceText.From(
                        GenerateCustomDataClassInheritanceCode(customDataClassRegistration.Value.Name,
                            customDataClassRegistration.Value.ContainingNamespace.ToString(), shouldInherit),
                        Encoding.UTF8));
            }


            var enumarableSymbols = new INamedTypeSymbol[]
            {
                comp.GetTypeByMetadataName("System.Collections.Generic.List<T>")
            };

            //generate all autodata classes
            foreach (var symbol in autoDataRegistrations)
            {
                var fields = new List<FieldTemplate>();
                foreach (var member in symbol.GetMembers())
                {
                    var attribute = member.GetAttribute(yamlFieldAttribute);
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

                    fields.Add(new FieldTemplate(member.Name, type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), ""));
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

        private struct FieldTemplate
        {
            public readonly string Name;
            public readonly string Type;
            public readonly string AttributeText;

            public FieldTemplate(string name, string type, string attributeText)
            {
                Name = name;
                AttributeText = attributeText;
                Type = type.EndsWith("?") ? type : $"{type}?";
            }
        }
    }
}
