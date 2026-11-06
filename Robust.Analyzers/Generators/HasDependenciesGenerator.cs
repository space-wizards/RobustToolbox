using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Roslyn.Shared;
using Robust.Roslyn.Shared.Helpers;
using Robust.Shared.IoC;
namespace Robust.Shared.IoC
{
    public enum IoCMode
    {
        Required,
        Optional,
        Differed,
        DifferedOptional
    }
}
namespace Robust.Analyzers.Generators
{
    [Generator(LanguageNames.CSharp)]
    public sealed class HasDependenciesGenerator : IIncrementalGenerator
    {
        private const string DependencyAttributeName = "Robust.Shared.IoC.DependencyAttribute";
        private const string IHasDependenciesName = "Robust.Shared.IoC.IHasDependencies";
        private const string IPostInjectHandlerName = "Robust.Shared.IoC.IPostInjectHandler";
        private const string IDifferedInjectHandlerName = "Robust.Shared.IoC.IDifferedInjectHandler";

        private readonly record struct FieldInfo(string Name, string TypeName,bool IsReadOnly,IoCMode Mode);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var fields = context.SyntaxProvider.ForAttributeWithMetadataName(
                DependencyAttributeName,
                static (node, _) => node is VariableDeclaratorSyntax,
                static (syntaxContext, token) =>
                {
                    var field = (IFieldSymbol)syntaxContext.TargetSymbol;
                    var fieldType = (INamedTypeSymbol)field.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                    var owningType = (INamedTypeSymbol)field.ContainingSymbol;

                    var mode = IoCMode.Required;
                    var attributeData = syntaxContext.Attributes.FirstOrDefault();
                    if (attributeData != null)
                    {
                        if (attributeData.ConstructorArguments.Length > 0)
                        {
                            if (attributeData.ConstructorArguments[0].Value is int intValue)
                            {
                                mode = (IoCMode)intValue;
                            }
                        }
                        else
                        {
                            var modeAssignment = attributeData.NamedArguments.FirstOrDefault(x => x.Key == "Mode");
                            if (modeAssignment.Value.Value is int intValue)
                            {
                                mode = (IoCMode)intValue;
                            }
                        }
                    }

                    var declarationSyntax = (TypeDeclarationSyntax)owningType.DeclaringSyntaxReferences[0]
                        .GetSyntax(token);

                    var partialTypeInfo = PartialTypeInfo.FromSymbol(owningType, declarationSyntax);

                    return (partialTypeInfo, Symbol:owningType,FieldInfo: new FieldInfo(field.Name,fieldType.ToDisplayString(),field.IsReadOnly, mode));
                });

            var grouped = fields
                .Where(p => p.partialTypeInfo.IsValid)
                .Collect()
                .SelectMany(static (array, _) =>
                {
                    return array.GroupBy(info => info.partialTypeInfo,
                            PartialTypeInfo.WithoutLocationComparer.Instance)
                        .Select(group => (
                            TypeInfo: group.Key,
                            TypeSymbol: group.First().Symbol, // Pull the symbol from the group
                            Fields: group.Select(e => e.FieldInfo).AsEquatableArray()
                        ));
                });

            var hasDependencyParents = grouped
                .Collect()
                .Combine(context.CompilationProvider)
                .Select(static (a, cancel) =>
                {
                    var (groups, compilation) = a;

                    var hasDependencyParents = new List<PartialTypeInfo>();

                    var ourAssemblyTypes = groups
                        .Where(g => g.Fields.All(static x => !x.IsReadOnly))
                        .Select(x => x.TypeInfo)
                        .ToDictionary<PartialTypeInfo, INamedTypeSymbol, PartialTypeInfo>(
                            x =>
                            {
                                var val = compilation.GetTypeByMetadataName(x.GetMetadataName());
                                if (val == null)
                                    throw new InvalidOperationException();

                                return val.OriginalDefinition;
                            },
                            static x => x,
                            SymbolEqualityComparer.Default);

                    var hasDependencies = compilation.GetTypeByMetadataName(IHasDependenciesName);
                    if (hasDependencies == null && ourAssemblyTypes.Count != 0)
                        throw new InvalidOperationException();

                    var postInjectHandler = compilation.GetTypeByMetadataName(IPostInjectHandlerName);
                    var differedInjectHandler = compilation.GetTypeByMetadataName(IDifferedInjectHandlerName);

                    foreach (var kvp in ourAssemblyTypes)
                    {
                        cancel.ThrowIfCancellationRequested();

                        var typeInfo = kvp.Key;

                        if (typeInfo.AllInterfaces.Contains(hasDependencies, SymbolEqualityComparer.Default))
                        {
                            hasDependencyParents.Add(kvp.Value);
                            continue;
                        }

                        for (
                            var ti = typeInfo.BaseType;
                            ti != null && SymbolEqualityComparer.Default.Equals(ti.ContainingAssembly, compilation.Assembly);
                            ti = ti.BaseType)
                        {
                            if (ourAssemblyTypes.ContainsKey(ti.OriginalDefinition))
                            {
                                hasDependencyParents.Add(kvp.Value);
                                break;
                            }
                        }
                    }

                    return hasDependencyParents.ToImmutableArray();
                });
            context.RegisterImplementationSourceOutput(
                grouped.Combine(hasDependencyParents),
                static (productionContext, tuple) =>
                {
                    var ((typeInfo, symbol,fields), hasParentList) = tuple;

                    if (fields.Any(a => a.IsReadOnly))
                        return;

                    var hasParent = hasParentList.Contains(typeInfo);

                    bool implementsPostInject = symbol.AllInterfaces.Any(i =>
                        i.ToDisplayString() == IPostInjectHandlerName);

                    bool implementsDifferedInject = symbol.AllInterfaces.Any(i =>
                        i.ToDisplayString() == IDifferedInjectHandlerName);

                    var sb = new IndentWriter(new StringBuilder());

                    sb.AppendLine("// <auto-generated />");
                    sb.AppendLine();

                    typeInfo.WriteHeader(ref sb, "[global::Robust.Shared.IoC.HasDependenciesGeneratedAttribute]");

                    if (!hasParent)
                    {
                        sb.AppendLine($" : global::{IHasDependenciesName}");
                    }
                    else
                    {
                        sb.AppendLine();
                    }

                    sb.AppendOpeningBrace(); // {
                    if (!hasParent && typeInfo.IsSealed)
                    {
                        // Explicit impl only
                        sb.AppendLineIndented("[global::Robust.Shared.Analyzers.RobustAutoGenerated]");
                        sb.AppendLineIndented($"void global::{IHasDependenciesName}.Inject(global::Robust.Shared.IoC.IDependencyCollection dependencies)");
                        sb.AppendOpeningBrace(); // {
                        WriteInject(ref sb, fields, false, typeInfo.DisplayName, false);
                        if (implementsPostInject) sb.AppendLineIndented($"this.PostInject();");
                        sb.AppendClosingBrace(); // }
                        sb.AppendLine();
                        // Explicit impl only, differed
                        sb.AppendLineIndented("[global::Robust.Shared.Analyzers.RobustAutoGenerated]");
                        sb.AppendLineIndented($"void global::{IHasDependenciesName}.DifferedInject(global::Robust.Shared.IoC.IDependencyCollection dependencies)");
                        sb.AppendOpeningBrace(); // {
                        WriteInject(ref sb, fields, false, typeInfo.DisplayName, true);
                        if (implementsDifferedInject) sb.AppendLineIndented($"this.PostDifferedInject();");
                        sb.AppendClosingBrace(); // }
                    }
                    else
                    {
                        if (!hasParent)
                        {
                            // Explicit impl -> protected virtual methods
                            sb.AppendLineIndented("[global::Robust.Shared.Analyzers.RobustAutoGenerated]");
                            sb.AppendLineIndented($"void global::{IHasDependenciesName}.Inject(global::Robust.Shared.IoC.IDependencyCollection dependencies)");
                            sb.AppendOpeningBrace(); // {
                            sb.AppendLineIndented("InjectImpl(dependencies);");
                            sb.AppendClosingBrace(); // }
                            sb.AppendLine();
                            sb.AppendLineIndented("[global::Robust.Shared.Analyzers.RobustAutoGenerated]");
                            sb.AppendLineIndented($"void global::{IHasDependenciesName}.DifferedInject(global::Robust.Shared.IoC.IDependencyCollection dependencies)");
                            sb.AppendOpeningBrace(); // {
                            sb.AppendLineIndented("DifferedInjectImpl(dependencies);");
                            sb.AppendClosingBrace(); // }
                            sb.AppendLine();
                        }

                        // Protected virtual/override methods
                        sb.AppendLineIndented("[global::Robust.Shared.Analyzers.RobustAutoGenerated]");
                        sb.AppendLineIndented("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
                        sb.AppendLineIndented($"protected {(hasParent ? "override" : "virtual")} void InjectImpl(global::Robust.Shared.IoC.IDependencyCollection dependencies)");
                        sb.AppendOpeningBrace(); // {
                        WriteInject(ref sb, fields, hasParent, typeInfo.DisplayName, false);
                        if (implementsPostInject) sb.AppendLineIndented($"this.PostInject();");
                        sb.AppendClosingBrace(); // }
                        sb.AppendLine();

                        //differed
                        sb.AppendLineIndented("[global::Robust.Shared.Analyzers.RobustAutoGenerated]");
                        sb.AppendLineIndented("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
                        sb.AppendLineIndented($"protected {(hasParent ? "override" : "virtual")} void DifferedInjectImpl(global::Robust.Shared.IoC.IDependencyCollection dependencies)");
                        sb.AppendOpeningBrace(); // {
                        WriteInject(ref sb, fields, hasParent, typeInfo.DisplayName, true);
                        if (implementsDifferedInject) sb.AppendLineIndented($"this.PostDifferedInject();");
                        sb.AppendClosingBrace(); // }
                    }
                    sb.AppendClosingBrace(); // }

                    typeInfo.WriteFooter(ref sb);

                    productionContext.AddSource(typeInfo.GetGeneratedFileName(), sb.ToString());
                });
        }

        private static void WriteInject(ref IndentWriter sb, EquatableArray<FieldInfo> fields, bool isOverride, string typeName, bool differed)
        {
            if (differed)
            {
                foreach (var field in fields)
                {
                    Write(ref sb, field);
                }
            }
            else
            {
                foreach (var field in fields)
                {
                    WriteDiffered(ref sb, field);
                }
            }
            if (!isOverride) return;
            sb.AppendLine();
            sb.AppendLineIndented(differed ? "base.DifferedInjectImpl(dependencies);" : "base.InjectImpl(dependencies);");

            void Write(ref IndentWriter sb, FieldInfo field)
            {
                string methodName;
                switch (field.Mode)
                {
                    case IoCMode.Required:
                        methodName = "ResolveInject";
                        break;
                    case IoCMode.Optional:
                        methodName = "TryResolveInject";
                        break;
                    case IoCMode.Differed:
                    case IoCMode.DifferedOptional:
                    default:
                        return;
                }
                sb.AppendLineIndented($"{field.Name} = dependencies.{methodName}<global::{field.TypeName}>(typeof({typeName}));");
            }

            void WriteDiffered(ref IndentWriter sb, FieldInfo field)
            {
                string methodName;
                switch (field.Mode)
                {
                    case IoCMode.Differed:
                        methodName = "ResolveInject";
                        break;
                    case IoCMode.DifferedOptional:
                        methodName = "TryResolveInject";
                        break;
                    case IoCMode.Required:
                    case IoCMode.Optional:
                    default:
                        return;
                }
                sb.AppendLineIndented($"{field.Name} = dependencies.{methodName}<global::{field.TypeName}>(typeof({typeName}));");
            }
        }
    }
}
