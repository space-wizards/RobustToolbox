using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ForbidLiteralAnalyzer : DiagnosticAnalyzer
{
    private const string ForbidLiteralType = "Robust.Shared.Analyzers.ForbidLiteralAttribute";

    public static DiagnosticDescriptor ForbidLiteralRule = new(
        Diagnostics.IdForbidLiteral,
        "Parameter forbids literal values",
        "The {0} parameter of {1} forbids literal values",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        "Pass in a validated wrapper type like ProtoId, or a const or static value."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [ForbidLiteralRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
    }

    private void AnalyzeOperation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
            return;

        // Check each parameter of the method invocation
        foreach (var argumentOperation in invocationOperation.Arguments)
        {
            // Check for our attribute on the parameter
            if (!AttributeHelper.HasAttribute(argumentOperation.Parameter, ForbidLiteralType, out _))
                continue;

            // Handle parameters using the params keyword
            if (argumentOperation.Syntax is InvocationExpressionSyntax subExpressionSyntax)
            {
                // Check each param value
                foreach (var subArgument in subExpressionSyntax.ArgumentList.Arguments)
                {
                    CheckArgumentSyntax(context, argumentOperation, subArgument);
                }
                continue;
            }

            // Not params, so just check the single parameter
            if (argumentOperation.Syntax is not ArgumentSyntax argumentSyntax)
                continue;

            CheckArgumentSyntax(context, argumentOperation, argumentSyntax);
        }
    }

    private void CheckArgumentSyntax(OperationAnalysisContext context, IArgumentOperation operation, ArgumentSyntax argumentSyntax)
    {
        // Handle collection types
        if (argumentSyntax.Expression is CollectionExpressionSyntax collectionExpressionSyntax)
        {
            // Check each value of the collection
            foreach (var elementSyntax in collectionExpressionSyntax.Elements)
            {
                if (elementSyntax is not ExpressionElementSyntax expressionSyntax)
                    continue;

                // Check if a literal was passed in
                if (expressionSyntax.Expression is not LiteralExpressionSyntax)
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(ForbidLiteralRule,
                    expressionSyntax.GetLocation(),
                    operation.Parameter.Name,
                    (context.Operation as IInvocationOperation).TargetMethod.Name
                ));
            }
            return;
        }

        // Not a collection, just a single value to check
        // Check if it's a literal
        if (argumentSyntax.Expression is not LiteralExpressionSyntax)
            return;

        context.ReportDiagnostic(Diagnostic.Create(ForbidLiteralRule,
            argumentSyntax.GetLocation(),
            operation.Parameter.Name,
            (context.Operation as IInvocationOperation).TargetMethod.Name
        ));
    }
}
