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
    public partial class DataClassGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new AutoDataClassRegistrationReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is DeepCloneCandidates deepCloneCandidates)
            {
                AnalyzeDeepCloneCandidates(context, deepCloneCandidates.Candidates);
            }
            if(!(context.SyntaxReceiver is AutoDataClassRegistrationReceiver receiver)) return;

            Debugger.Launch();

            var comp = (CSharpCompilation)context.Compilation;
            var iCompType = comp.GetTypeByMetadataName("Robust.Shared.Interfaces.GameObjects.IComponent");

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
                    var msg = $"Could not resolve CustomDataClassAttribute for class {classDeclarationSyntax.Identifier.Text}";
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("RADC0001", msg, msg, "Usage", DiagnosticSeverity.Error, true),
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
                    shouldInherit = ResolveParentDataClass(symbol, true);
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

                if(typeSymbol.Interfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, iCompType)) || typeSymbol.BaseType == null)
                    return "Robust.Shared.Prototypes.ComponentData";

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
                        var msg =
                            $"YamlFieldAttribute for Member {member} of type {symbol} has an invalid tag {fieldName}.";
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor("RADC0003", msg, msg, "Usage", DiagnosticSeverity.Error, true),
                            member.Locations.First()));
                        continue;
                    }

                    var @readonly = GetCtorArg<bool>(attribute.ConstructorArguments, 1);
                    var flagType = GetCtorArg<ITypeSymbol>(attribute.ConstructorArguments, 2);
                    var constType = GetCtorArg<ITypeSymbol>(attribute.ConstructorArguments, 3);

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
                    fields.Add(new FieldTemplate(fieldName, type, @readonly, flagType, constType));
                }

                var name = $"{symbol.Name}_AUTODATA";
                var @namespace = symbol.ContainingNamespace.ToString();

                var inheriting = ResolveParentDataClass(symbol.BaseType);

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
