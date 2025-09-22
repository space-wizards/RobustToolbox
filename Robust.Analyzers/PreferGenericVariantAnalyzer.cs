using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PreferGenericVariantAnalyzer : DiagnosticAnalyzer
{
    private const string AttributeType = "Robust.Shared.Analyzers.PreferGenericVariantAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        UseGenericVariantDescriptor, UseGenericVariantInvalidUsageDescriptor,
        UseGenericVariantAttributeValueErrorDescriptor);

    private static readonly DiagnosticDescriptor UseGenericVariantDescriptor = new(
        Diagnostics.IdUseGenericVariant,
        "Consider using the generic variant of this method",
        "Consider using the generic variant of this method to avoid potential allocations",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        "Consider using the generic variant of this method to avoid potential allocations.");

    private static readonly DiagnosticDescriptor UseGenericVariantInvalidUsageDescriptor = new(
        Diagnostics.IdUseGenericVariantInvalidUsage,
        "Invalid generic variant provided",
        "Generic variant provided mismatches the amount of type parameters of non-generic variant",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "The non-generic variant should have at least as many type parameter at the beginning of the method as there are generic type parameters on the generic variant.");

    private static readonly DiagnosticDescriptor UseGenericVariantAttributeValueErrorDescriptor = new(
        Diagnostics.IdUseGenericVariantAttributeValueError,
        "Failed resolving generic variant value",
        "Failed resolving generic variant value: {0}",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Consider using nameof to avoid any typos.");

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics | GeneratedCodeAnalysisFlags.Analyze);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(CheckForGenericVariant, OperationKind.Invocation);
    }

    private void CheckForGenericVariant(OperationAnalysisContext obj)
    {
        if(obj.Operation is not IInvocationOperation invocationOperation) return;

        var preferGenericAttribute = obj.Compilation.GetTypeByMetadataName(AttributeType);

        string genericVariant = null;
        AttributeData foundAttribute = null;
        foreach (var attribute in invocationOperation.TargetMethod.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, preferGenericAttribute))
                continue;

            genericVariant = attribute.ConstructorArguments[0].Value as string ?? invocationOperation.TargetMethod.Name;
            foundAttribute = attribute;
            break;
        }

        if(genericVariant == null) return;

        var maxTypeParams = 0;
        var typeTypeSymbol = obj.Compilation.GetTypeByMetadataName("System.Type");
        foreach (var parameter in invocationOperation.TargetMethod.Parameters)
        {
            if(!SymbolEqualityComparer.Default.Equals(parameter.Type, typeTypeSymbol)) break;

            maxTypeParams++;
        }

        if (maxTypeParams == 0)
        {
            obj.ReportDiagnostic(
                Diagnostic.Create(UseGenericVariantInvalidUsageDescriptor,
                    foundAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()));
            return;
        }

        IMethodSymbol genericVariantMethod = null;
        foreach (var member in invocationOperation.TargetMethod.ContainingType.GetMembers())
        {
            if (member is not IMethodSymbol methodSymbol
                || methodSymbol.Name != genericVariant
                || !methodSymbol.IsGenericMethod
                || methodSymbol.TypeParameters.Length > maxTypeParams
                || methodSymbol.Parameters.Length > invocationOperation.TargetMethod.Parameters.Length - methodSymbol.TypeParameters.Length
                ) continue;

            var typeParamCount = methodSymbol.TypeParameters.Length;
            var failedParamComparison = false;
            var objType = obj.Compilation.GetSpecialType(SpecialType.System_Object);
            for (int i = 0; i < methodSymbol.Parameters.Length; i++)
            {
                if (methodSymbol.Parameters[i].Type is ITypeParameterSymbol && SymbolEqualityComparer.Default.Equals(invocationOperation.TargetMethod.Parameters[i + typeParamCount].Type, objType))
                    continue;

                if (!SymbolEqualityComparer.Default.Equals(methodSymbol.Parameters[i].Type,
                        invocationOperation.TargetMethod.Parameters[i + typeParamCount].Type))
                {
                    failedParamComparison = true;
                    break;
                }
            }

            if(failedParamComparison) continue;

            genericVariantMethod = methodSymbol;
        }

        if (genericVariantMethod == null)
        {
            obj.ReportDiagnostic(Diagnostic.Create(
                UseGenericVariantAttributeValueErrorDescriptor,
                foundAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                genericVariant));
            return;
        }

        var typeOperands = new string[genericVariantMethod.TypeParameters.Length];
        for (var i = 0; i < genericVariantMethod.TypeParameters.Length; i++)
        {
            switch (invocationOperation.Arguments[i].Value)
            {
                //todo figure out if ILocalReferenceOperation, IPropertyReferenceOperation or IFieldReferenceOperation is referencing static typeof assignments
                case ITypeOfOperation typeOfOperation:
                    typeOperands[i] = typeOfOperation.TypeOperand.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    continue;
                default:
                    return;
            }
        }

        obj.ReportDiagnostic(Diagnostic.Create(
            UseGenericVariantDescriptor,
            invocationOperation.Syntax.GetLocation(),
            ImmutableDictionary.CreateRange(new Dictionary<string, string>()
            {
                {"typeOperands", string.Join(",", typeOperands)}
            })));
    }
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class PreferGenericVariantCodeFixProvider : CodeFixProvider
{
    private static string Title(string method, string[] types) => $"Use {method}<{string.Join(",", types)}>.";

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync();
        if(root == null) return;

        foreach (var diagnostic in context.Diagnostics)
        {
            if (!diagnostic.Properties.TryGetValue("typeOperands", out var typeOperandsRaw)
                || typeOperandsRaw == null) continue;

            var node = root.FindNode(diagnostic.Location.SourceSpan);
            if (node is ArgumentSyntax argumentSyntax)
                node = argumentSyntax.Expression;

            if(node is not InvocationExpressionSyntax invocationExpression)
                continue;

            var typeOperands = typeOperandsRaw.Split(',');

            context.RegisterCodeFix(
                CodeAction.Create(
                    Title(invocationExpression.Expression.ToString(), typeOperands),
                    c => FixAsync(context.Document, invocationExpression, typeOperands, c),
                    Title(invocationExpression.Expression.ToString(), typeOperands)),
                diagnostic);
        }
    }

    private async Task<Document> FixAsync(
        Document contextDocument,
        InvocationExpressionSyntax invocationExpression,
        string[] typeOperands,
        CancellationToken cancellationToken)
    {
        var memberAccess = (MemberAccessExpressionSyntax)invocationExpression.Expression;

        var root = (CompilationUnitSyntax) await contextDocument.GetSyntaxRootAsync(cancellationToken);

        var arguments = new ArgumentSyntax[invocationExpression.ArgumentList.Arguments.Count - typeOperands.Length];
        var types = new TypeSyntax[typeOperands.Length];

        for (int i = 0; i < typeOperands.Length; i++)
        {
            types[i] = ((TypeOfExpressionSyntax)invocationExpression.ArgumentList.Arguments[i].Expression).Type;
        }



        Array.Copy(
            invocationExpression.ArgumentList.Arguments.ToArray(),
            typeOperands.Length,
            arguments,
            0,
            arguments.Length);

        memberAccess = memberAccess.WithName(SyntaxFactory.GenericName(memberAccess.Name.Identifier,
            SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(types))));

        root = root!.ReplaceNode(invocationExpression,
            invocationExpression.WithArgumentList(invocationExpression.ArgumentList.WithArguments(SyntaxFactory.SeparatedList(arguments)))
                .WithExpression(memberAccess));

        return contextDocument.WithSyntaxRoot(root);
    }

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(Diagnostics.IdUseGenericVariant);
}
