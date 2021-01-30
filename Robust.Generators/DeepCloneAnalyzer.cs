using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Robust.Generators
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DeepCloneSuppressor : DiagnosticSuppressor
    {
        private string[] SuppressedAttributes => new[]
        {
            "Robust.Shared.Prototypes.YamlFieldAttribute",
            "Robust.Shared.Prototypes.CustomYamlFieldAttribute"
        };

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            var attributeSymbols = SuppressedAttributes.Select(attr => context.Compilation.GetTypeByMetadataName(attr));
            foreach (var reportedDiagnostic in context.ReportedDiagnostics)
            {
                if(reportedDiagnostic.Id != Diagnostics.YamlMeansImplicitUse.SuppressedDiagnosticId) continue;

                var node = reportedDiagnostic.Location.SourceTree?.GetRoot(context.CancellationToken).FindNode(reportedDiagnostic.Location.SourceSpan);
                if (node == null) continue;

                var symbol = context.GetSemanticModel(reportedDiagnostic.Location.SourceTree).GetDeclaredSymbol(node);

                ImmutableArray<AttributeData> attributes;
                switch (symbol)
                {
                    case IFieldSymbol field:
                        attributes = field.GetAttributes();
                        break;
                    case IPropertySymbol property:
                        attributes = property.GetAttributes();
                        break;
                    default:
                        throw new Exception($"Invalid SymbolType: {symbol?.GetType()}");
                }

                if(attributes.All(a => !attributeSymbols.Any(attr => SymbolEqualityComparer.Default.Equals(attr, a.AttributeClass))))
                {
                    continue;
                }

                context.ReportSuppression(Suppression.Create(
                    Diagnostics.YamlMeansImplicitUse,
                    reportedDiagnostic));
            }
        }

        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(Diagnostics.YamlMeansImplicitUse);
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DeepCloneAnalyzer : DiagnosticAnalyzer
    {
        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationAction((c) => AnalyzeDeepCloneCandidates(c));
            context.EnableConcurrentExecution();
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Diagnostics.NoDeepCloneImpl, Diagnostics.InvalidDeepCloneImpl);

        private void AnalyzeDeepCloneCandidates(CompilationAnalysisContext context)
        {
            var compilation = context.Compilation;
            var deepCloneSymbol =
                compilation.GetTypeByMetadataName("Robust.Shared.Interfaces.Serialization.IDeepClone");

            bool InheritsDeepCloneInterface(ITypeSymbol typeSymbol)
            {
                if (typeSymbol == null) return false;
                return SymbolEqualityComparer.Default.Equals(typeSymbol, deepCloneSymbol) || InheritsDeepCloneInterface(typeSymbol.BaseType);
            }

            IMethodSymbol GetDeepCloneImplementation(ITypeSymbol typeSymbol)
            {
                foreach (var member in typeSymbol.GetMembers())
                {
                    if (member is IMethodSymbol methodSymbol && methodSymbol.Name == "DeepClone") return methodSymbol;
                }

                return null;
            }

            foreach (var syntax in compilation.SyntaxTrees.SelectMany(s => CandidateWalker.GetClassDeclarationSyntaxes(s.GetRoot())))
            {
                var symbol = ModelExtensions.GetDeclaredSymbol(compilation.GetSemanticModel(syntax.SyntaxTree), syntax) as ITypeSymbol;
                if(symbol == null || !InheritsDeepCloneInterface(symbol)) continue;

                var implementation = GetDeepCloneImplementation(symbol);
                if (implementation == null)
                {
                    foreach (var loc in symbol.Locations)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.NoDeepCloneImpl,
                            loc));
                    }
                    continue;
                }

                if (implementation.IsAbstract)
                {
                    continue; //todo Paul: implementation deferred, allowed (?)
                }

                foreach (var syntaxReference in implementation.DeclaringSyntaxReferences)
                {
                    var methodSyntax = syntaxReference.SyntaxTree.GetRoot() as MethodDeclarationSyntax;
                    if(methodSyntax == null) continue;
                    foreach (var invalidAssignment in MethodSyntaxWalker.GetFaultyAssignments(methodSyntax))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.InvalidDeepCloneImpl,
                            invalidAssignment.GetLocation()));
                    }
                }
            }
        }

        class CandidateWalker : CSharpSyntaxWalker
        {
            public static List<ClassDeclarationSyntax> GetClassDeclarationSyntaxes(SyntaxNode node)
            {
                var walker = new CandidateWalker();
                walker.Visit(node);
                return walker._classDeclarationSyntaxes;
            }

            private List<ClassDeclarationSyntax> _classDeclarationSyntaxes = new List<ClassDeclarationSyntax>();

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                _classDeclarationSyntaxes.Add(node);
                base.VisitClassDeclaration(node);
            }
        }

        class MethodSyntaxWalker : CSharpSyntaxWalker
        {
            private MethodSyntaxWalker(){}

            private List<CSharpSyntaxNode> foundFaultyAssignments = new List<CSharpSyntaxNode>();

            public static List<CSharpSyntaxNode> GetFaultyAssignments(SyntaxNode syntaxNode)
            {
                var walker = new MethodSyntaxWalker();
                walker.Visit(syntaxNode);
                return walker.foundFaultyAssignments;
            }

            //makes sure we only analyze the returnstatement
            public override void VisitReturnStatement(ReturnStatementSyntax node)
            {
                base.VisitReturnStatement(node);
            }

            public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                base.VisitMethodDeclaration(node);
            }

            public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            {
                if(node.ArgumentList != null)
                {
                    foreach (var arg in node.ArgumentList.Arguments)
                    {
                        if (!IsValidExpression(arg.Expression))
                        {
                            foundFaultyAssignments.Add(arg.Expression);
                        }
                    }
                }

                base.VisitObjectCreationExpression(node);
            }

            public override void VisitInitializerExpression(InitializerExpressionSyntax node)
            {
                foreach (var expression in node.Expressions)
                {
                    if (!IsValidExpression(expression))
                    {
                        foundFaultyAssignments.Add(expression);
                    }
                }
                base.VisitInitializerExpression(node);
            }

            private bool IsValidExpression(ExpressionSyntax expressionSyntax)
            {
                var assignment = expressionSyntax as AssignmentExpressionSyntax;
                if (!(assignment?.Right is InvocationExpressionSyntax rightMethodAssignment) ||
                    rightMethodAssignment.Expression.ToString() != "IDeepClone.CloneValue")
                {
                    return false;
                }

                return true;
            }
        }
    }
}
