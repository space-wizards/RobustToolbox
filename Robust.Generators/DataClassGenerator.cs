using System;
using System.Collections.Generic;
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
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new AutoDataClassRegistrationReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if(!(context.SyntaxReceiver is AutoDataClassRegistrationReceiver receiver)) return;

            var comp = (CSharpCompilation)context.Compilation;
            var iCompType = comp.GetTypeByMetadataName("Robust.Shared.Interfaces.GameObjects.IComponent");

            //resolve all custom dataclasses
            var resolvedCustomDataClasses = new Dictionary<ITypeSymbol, ITypeSymbol>();
            foreach (var classDeclarationSyntax in receiver.CustomDataClassRegistrations)
            {
                var symbol = comp.GetSemanticModel(classDeclarationSyntax.SyntaxTree)
                    .GetDeclaredSymbol(classDeclarationSyntax);

                var arg = symbol?.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.Name == "CustomDataClassAttribute")?.ConstructorArguments[0];
                if (arg == null)
                {
                    var msg = $"Could not resolve argument of CustomDataClassAttribute for class {classDeclarationSyntax.Identifier.Text}";
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("RADC0002", msg, msg, "Usage", DiagnosticSeverity.Error, true),
                        classDeclarationSyntax.GetLocation()));
                    return;
                }
                resolvedCustomDataClasses.Add(symbol, (ITypeSymbol)arg.Value.Value);
            }

            string ResolveParentDataClass(ITypeSymbol typeSymbol)
            {
                if(typeSymbol.Interfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, iCompType)))
                    return "Robust.Shared.Prototypes.ComponentData";

                var baseType = typeSymbol.BaseType;

                if (resolvedCustomDataClasses.TryGetValue(baseType, out var customDataClass))
                    return $"{customDataClass.ContainingNamespace}.{customDataClass.Name}";

                var metaName = GetDataClassName(baseType);
                var dataClass = comp.GetTypeByMetadataName(metaName);
                if (dataClass == null) return ResolveParentDataClass(baseType);
                return metaName;
            }

            //resolve autodata registrations (we need the to validate the customdataclasses)
            var resolvedAutoDataRegistrations =
                receiver.Registrations.Select(cl => comp.GetSemanticModel(cl.SyntaxTree).GetDeclaredSymbol(cl)).ToArray();

            //generate all autodata classes
            foreach (var symbol in resolvedAutoDataRegistrations)
            {
                var fields = new List<FieldTemplate>();
                foreach (var member in symbol.GetMembers())
                {
                    var attribute = member.GetAttributes()
                        .FirstOrDefault(a => a.AttributeClass?.Name == "YamlFieldAttribute");
                    if(attribute == null) continue;
                    var fieldName = (string)attribute.ConstructorArguments[0].Value;
                    string type;
                    switch (member)
                    {
                        case IFieldSymbol fieldSymbol:
                            type = fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            break;
                        case IPropertySymbol propertySymbol:
                            type = propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            break;
                        default:
                            var msg =
                                $"YamlFieldAttribute assigned for Member {member} of type {symbol} which is neither Field or Property! It will be ignored.";
                            context.ReportDiagnostic(Diagnostic.Create(
                                new DiagnosticDescriptor("RADC0000", msg, msg, "Usage", DiagnosticSeverity.Warning, true),
                                member.Locations.First()));
                            continue;
                    }
                    fields.Add(new FieldTemplate(fieldName, type));
                }

                var name = $"{symbol.Name}_AUTODATA";
                var @namespace = symbol.ContainingNamespace.ToString();

                string inheriting = ResolveParentDataClass(symbol);

                context.AddSource($"{name}.g.cs", SourceText.From(GenerateCode(name, @namespace, inheriting, fields), Encoding.UTF8));
            }

            //check if all custom dataclasses are inheriting the correct parent
            foreach (var pair in resolvedCustomDataClasses)
            {
                var component = pair.Key;
                var dataclass = pair.Value;

                var shouldInherit = ResolveParentDataClass(component);
                if (dataclass.BaseType?.ToDisplayString() == shouldInherit)
                {
                    continue;

                }

                if (resolvedAutoDataRegistrations.Any(r => SymbolEqualityComparer.Default.Equals(component, r)))
                {
                    shouldInherit = GetDataClassName(component);
                    if (dataclass.BaseType?.ToDisplayString() == shouldInherit)
                    {
                        continue;
                    }
                }

                var msg = $"Custom Dataclass is inheriting {dataclass.BaseType} when it should inherit {shouldInherit}";
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor("RADC0001", msg, msg, "Usage", DiagnosticSeverity.Error, true),
                    dataclass.Locations.First()));
            }
        }

        private string GetDataClassName(ITypeSymbol typeSymbol)
        {
            return $"{typeSymbol.ContainingNamespace}.{typeSymbol.Name}_AUTODATA";
        }

        private string GenerateCode(string name, string @namespace, string inheriting, List<FieldTemplate> fields)
        {
            var code = $@"#nullable enable
using System;
using System.Linq;
using Robust.Shared.Serialization;
namespace {@namespace} {{
    public class {name} : {inheriting} {{

        /// <inheritdoc />
        public override string[] Tags => base.Tags.Concat(new string[]
        {{";

            foreach (var field in fields)
            {
                code += $@"
            ""{field.Name}"",";
            }

            code += @"
        }).ToArray();
";

            //generate fields
            foreach (var field in fields)
            {
                code += $@"
        public {field.Type} {field.Name};";
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
            serializer.DataField(ref {field.Name}, ""{field.Name}"", null);";
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
                ""{field.Name}"" => {field.Name},";
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
                    {field.Name} = ({field.Type})value;
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

            public FieldTemplate(string name, string type)
            {
                Name = name;
                Type = type.EndsWith("?") ? type : $"{type}?";
            }
        }
    }
}
