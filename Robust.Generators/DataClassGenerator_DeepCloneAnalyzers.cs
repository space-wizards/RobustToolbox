using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Robust.Generators
{
    public partial class DataClassGenerator
    {
        private void AnalyzeDeepCloneCandidates(GeneratorExecutionContext context, List<ClassDeclarationSyntax> candidates)
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

            foreach (var syntax in candidates)
            {
                var symbol = ModelExtensions.GetDeclaredSymbol(compilation.GetSemanticModel(syntax.SyntaxTree), syntax) as ITypeSymbol;
                if(symbol == null || !InheritsDeepCloneInterface(symbol)) continue;

                var implementation = GetDeepCloneImplementation(symbol);
                if (implementation == null)
                {
                    foreach (var loc in symbol.Locations)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "RADC0004",
                                "",
                                $"{symbol} should implement IDeepClone.DeepClone",
                                "Usage",
                                DiagnosticSeverity.Error,
                                true),
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
                            new DiagnosticDescriptor(
                                "RADC0002",
                                "",
                                $"Invalid assignment found in DeepClone implementation: {invalidAssignment}",
                                "Usage",
                                DiagnosticSeverity.Error,
                                true),
                            invalidAssignment.GetLocation()));
                    }
                }
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

            //makes sure we only analyse the returnstatement
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
