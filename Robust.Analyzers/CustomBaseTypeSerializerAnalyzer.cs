using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CustomBaseTypeSerializerAnalyzer : DiagnosticAnalyzer
{
    [SuppressMessage("ReSharper", "RS2008")]
    private static readonly DiagnosticDescriptor ResultRule = new DiagnosticDescriptor(
        Diagnostics.IdTypeEndsWithBase,
        "Only abstract types with names ending in 'Base' are supported",
        "Type parameter {0} does not end with 'Base'",
        "Usage",
        DiagnosticSeverity.Error,
        true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ResultRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Check, SyntaxKind.TypeOfExpression);
    }

    private void Check(SyntaxNodeAnalysisContext symbolContext)
    {
        var serializerType =
            symbolContext.Compilation.GetTypeByMetadataName("Robust.Shared.Serialization.TypeSerializers.Implementations.Generic.CustomBaseTypeSerializer`1");

        if (serializerType != null)
        {
            var typeOfNode = (TypeOfExpressionSyntax)symbolContext.Node;
            var variableTypeInfo =
                symbolContext.SemanticModel.GetTypeInfo(typeOfNode.Type).ConvertedType as INamedTypeSymbol;


            if (variableTypeInfo == null)
                return;

            if (!SymbolEqualityComparer.Default.Equals(variableTypeInfo.OriginalDefinition, serializerType))
                return;

            var arguments = (typeOfNode.Type as GenericNameSyntax)?.TypeArgumentList.Arguments;
            if (arguments is not { Count: 1 })
            {
                symbolContext.ReportDiagnostic(Diagnostic.Create(ResultRule, symbolContext.Node.GetLocation(), ""));
                return;
            }

            var genericTypeArgument = arguments.First<TypeSyntax>();
            var argumentType = symbolContext.SemanticModel.GetTypeInfo(genericTypeArgument).Type;


            if (argumentType != null && !argumentType.Name.EndsWith("Base"))
            {
                symbolContext.ReportDiagnostic(Diagnostic.Create(ResultRule, symbolContext.Node.GetLocation(), argumentType.Name));
            }
        }
    }
}
