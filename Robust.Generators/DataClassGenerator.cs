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

            var errors = receiver.Registrations.Where(r => receiver.CustomDataClassRegistrations.Contains(r)).ToArray();
            if (errors.Length != 0)
            {
                foreach (var classDeclarationSyntax in errors)
                {
                    var msg = $"Class {classDeclarationSyntax.Identifier.Text} has both a CustomDataClass & AutoDataClass Attribute!";
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("RADC0001", msg, msg, "Usage", DiagnosticSeverity.Error, true),
                        classDeclarationSyntax.GetLocation()));
                }
                return;
            }

            var comp = (CSharpCompilation)context.Compilation;

            var iCompType = comp.GetTypeByMetadataName("Robust.Shared.Interfaces.GameObjects.IComponent");

            var resolvedCustomDataClasses = new Dictionary<ITypeSymbol, string>();
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
                resolvedCustomDataClasses.Add(symbol, (string)arg.Value.Value); //todo this is inadequate
            }


            foreach (var classDeclarationSyntax in receiver.Registrations)
            {
                var symbol = comp.GetSemanticModel(classDeclarationSyntax.SyntaxTree)
                    .GetDeclaredSymbol(classDeclarationSyntax);

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
                            type = $"{fieldSymbol.Type.ContainingNamespace}.{fieldSymbol.Type.Name}";
                            break;
                        case IPropertySymbol propertySymbol:
                            type = $"{propertySymbol.ContainingNamespace}.{propertySymbol.Type.MetadataName}";
                            break;
                        default:
                            var msg =
                                $"YamlFieldAttribute assigned for Member {member} of type {symbol} which is neither Field or Property! It will be ignored.";
                            context.ReportDiagnostic(Diagnostic.Create(
                                new DiagnosticDescriptor("RADC0000", msg, msg, "Usage", DiagnosticSeverity.Warning, true),
                                symbol.Locations.First()));
                            continue;
                    }
                    fields.Add(new FieldTemplate(fieldName, type));
                }

                var name = $"{symbol.Name}_AUTODATA";
                var @namespace = symbol.ContainingNamespace.ToString();
                string inheriting = null;
                if(symbol.Interfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, iCompType)))
                    inheriting = "Robust.Shared.Prototypes.ComponentData";
                else
                {
                    var baseType = symbol.BaseType;
                    if (resolvedCustomDataClasses.TryGetValue(baseType, out var customDataClass))
                        inheriting = customDataClass; //todo aaaahhhhhh
                    else
                        inheriting = $"{baseType.ContainingNamespace}.{baseType.Name}_AUTODATA";
                }

                context.AddSource($"{name}.g.cs", SourceText.From(GenerateCode(name, @namespace, inheriting, fields), Encoding.UTF8));
            }
        }

        private string GenerateCode(string name, string @namespace, string inheriting, List<FieldTemplate> fields)
        {
            var code = $@"#nullable enable
using System;
using System.Linq;
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
        public global::{field.Type} {field.Name};";
            }

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
                    {field.Name} = (global::{field.Type})value;
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
