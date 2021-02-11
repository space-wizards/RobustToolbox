using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Robust.Generators
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MeansImplicitAssigmentSuppressor : DiagnosticSuppressor
    {
        const string MeansImplicitAssignmentAttribute = "Robust.Shared.MeansImplicitAssignmentAttribute";

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            var implAttr = context.Compilation.GetTypeByMetadataName(MeansImplicitAssignmentAttribute);
            foreach (var reportedDiagnostic in context.ReportedDiagnostics)
            {
                if(reportedDiagnostic.Id != Diagnostics.MeansImplicitAssignment.SuppressedDiagnosticId) continue;

                var node = reportedDiagnostic.Location.SourceTree?.GetRoot(context.CancellationToken).FindNode(reportedDiagnostic.Location.SourceSpan);
                if (node == null) continue;

                var symbol = context.GetSemanticModel(reportedDiagnostic.Location.SourceTree).GetDeclaredSymbol(node);

                if (symbol == null || !symbol.GetAttributes().Any(a =>
                    a.AttributeClass?.GetAttributes().Any(attr =>
                        SymbolEqualityComparer.Default.Equals(attr.AttributeClass, implAttr)) == true))
                {
                    continue;
                }

                context.ReportSuppression(Suppression.Create(
                    Diagnostics.MeansImplicitAssignment,
                    reportedDiagnostic));
            }
        }

        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(Diagnostics.MeansImplicitAssignment);
    }
}
