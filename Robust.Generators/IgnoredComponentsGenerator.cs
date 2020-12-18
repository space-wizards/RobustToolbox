using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Content.Generators
{
    [Generator]
    public class IgnoredComponentsGenerator : ISourceGenerator
    {
        static IEnumerable<SyntaxTree> GetSyntaxTrees(string projectpath)
        {
            return Directory
                .EnumerateFiles($"{projectpath}", "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.StartsWith($"{projectpath}/obj"))
                .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), CSharpParseOptions.Default));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new ComponentSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if(!(context.SyntaxReceiver is ComponentSyntaxReceiver receiver)) return;

            var comp = (CSharpCompilation) context.Compilation;

            var solutionPathFile = context.AdditionalFiles.FirstOrDefault(f => f.Path.EndsWith("SolutionPathForGenerator"));
            if (solutionPathFile == null)
            {
                var msg = "Unable to find SolutionPathForGenerator-File!";
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        new DiagnosticDescriptor("RIC0000",
                            msg,
                            msg, "MsBuild", DiagnosticSeverity.Error, true), Location.None));
                return;
            }

            var solutionPath = solutionPathFile.GetText()?.ToString();
            if (solutionPath == null)
            {
                var msg = "SolutionPathForGenerator-File was empty!";
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        new DiagnosticDescriptor("RIC0000",
                            msg,
                            msg, "MsBuild", DiagnosticSeverity.Error, true), Location.None));
                return;
            }

            var split = solutionPath.Split('/');
            solutionPath = string.Join("/", split.Take(split.Length-1));
            var serverPath = $"{solutionPath}/Content.Server";
            var clientPath = $"{solutionPath}/Content.Client";
            var sharedPath = $"{solutionPath}/Content.Shared";

            if (!Directory.Exists(serverPath))
            {
                var msg = $"Could not find server dir at: {serverPath}.";
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        new DiagnosticDescriptor("RIC0000",
                            msg,
                            msg, "MsBuild", DiagnosticSeverity.Warning, true), Location.None));
                return;
            }

            if (!Directory.Exists(clientPath))
            {
                var msg = $"Could not find client dir at: {clientPath}.";
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        new DiagnosticDescriptor("RIC0000",
                            msg,
                            msg, "MsBuild", DiagnosticSeverity.Warning, true), Location.None));
                return;
            }

            if (!Directory.Exists(sharedPath))
            {
                var msg = $"Could not find shared dir at: {sharedPath}.";
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        new DiagnosticDescriptor("RIC0000",
                            msg,
                            msg, "MsBuild", DiagnosticSeverity.Warning, true), Location.None));
                return;
            }
            var sharedTrees = GetSyntaxTrees(sharedPath);

            var serverComp = CSharpCompilation.Create("Server", GetSyntaxTrees(serverPath).Concat(sharedTrees));
            var serverWalker = new ComponentNameSearchWalker();
            foreach (var syntaxTree in GetSyntaxTrees(serverPath))
            {
                serverWalker.Model = comp.GetSemanticModel(syntaxTree);
                serverWalker.Visit(syntaxTree.GetRoot());
            }

            var clientComp = CSharpCompilation.Create("Client", GetSyntaxTrees(clientPath).Concat(sharedTrees));
            var clientWalker = new ComponentNameSearchWalker();
            foreach (var syntaxTree in GetSyntaxTrees(clientPath))
            {
                clientWalker.Model = comp.GetSemanticModel(syntaxTree);
                clientWalker.Visit(syntaxTree.GetRoot());
            }

            var sharedComp = CSharpCompilation.Create("Shared", sharedTrees);
            var sharedWalker = new ComponentNameSearchWalker();
            foreach (var syntaxTree in GetSyntaxTrees(sharedPath))
            {
                sharedWalker.Model = comp.GetSemanticModel(syntaxTree);
                sharedWalker.Visit(syntaxTree.GetRoot());
            }


            IEnumerable<string> names;
            switch (comp.AssemblyName)
            {
                case "Content.Client":
                    names = serverWalker.Names.Where(n => !clientWalker.Names.Contains(n) && !sharedWalker.Names.Contains(n));
                    break;
                case "Content.Server":
                    names = clientWalker.Names.Where(n => !serverWalker.Names.Contains(n) && !sharedWalker.Names.Contains(n));
                    break;
                default:
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            new DiagnosticDescriptor("RIC0000",
                                $"Unknown AssemblyName: {comp.AssemblyName}.",
                                "", "MsBuild", DiagnosticSeverity.Warning, true), Location.None));
                    return;
            }

            var ignoredCompCode = $@"// <auto-generated />
namespace Content.AutoGenerated
{{
    public static class IgnoredComponents
    {{
        public static string[] List => new [] {{
{string.Join(",\n", names)}
        }};
    }}
}}";

            context.AddSource("IgnoredComponents.g.cs", SourceText.From(ignoredCompCode, Encoding.UTF8));
        }

        private class ComponentNameSearchWalker : CSharpSyntaxWalker
        {
            private readonly List<string> _names = new List<string>();
            public SemanticModel Model;

            public IReadOnlyList<string> Names => _names;

            static bool IsComponent(INamedTypeSymbol typeSymbol)
            {
                return typeSymbol.MetadataName == "Component" ||
                       typeSymbol.BaseType != null && IsComponent(typeSymbol.BaseType);
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                var typeSymbol = Model.GetDeclaredSymbol(node);
                if(IsComponent(typeSymbol))
                {
                    base.VisitClassDeclaration(node);
                }
            }

            public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (node.Identifier.Text != "Name") return;

                string name;
                if (node.ExpressionBody != null && node.ExpressionBody.Expression is LiteralExpressionSyntax literalExpressionSyntax3)
                {
                    name = literalExpressionSyntax3.Token.Text;
                }
                else if(node.AccessorList != null)
                {
                    var getAccessor = node.AccessorList?.Accessors.FirstOrDefault(a => a.Keyword.Text == "get");
                    if(getAccessor == null)
                    {
                        return;
                    }
                    if (getAccessor.Body != null && getAccessor.Body.Statements.Count == 1 &&
                        getAccessor.Body.Statements[0] is ReturnStatementSyntax returnStatementSyntax &&
                        returnStatementSyntax.Expression is LiteralExpressionSyntax literalExpressionSyntax1)
                    {
                        name = literalExpressionSyntax1.Token.Text;
                    }else if (getAccessor.ExpressionBody != null && getAccessor.ExpressionBody.Expression is LiteralExpressionSyntax literalExpressionSyntax2)
                    {
                        name = literalExpressionSyntax2.Token.Text;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }

                _names.Add(name);
            }
        }
    }
}
