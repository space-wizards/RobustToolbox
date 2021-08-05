using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Robust.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FriendAnalyzer : DiagnosticAnalyzer
    {
        const string FriendAttribute = "Robust.Shared.Analyzers.FriendAttribute";

        public const string DiagnosticId = "RA0002";

        private const string Title = "Tried to access friend-only member";
        private const string MessageFormat = "Tried to access member \"{0}\" in class \"{1}\" which can only be accessed by friend classes";
        private const string Description = "Make sure to specify the accessing class in the friends attribute.";
        private const string Category = "Usage";

        [SuppressMessage("ReSharper", "RS2008")]
        private static readonly DiagnosticDescriptor Rule = new (DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, true, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(CheckFriendship, SyntaxKind.SimpleMemberAccessExpression);
        }

        private void CheckFriendship(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not MemberAccessExpressionSyntax memberAccess)
                return;

            // We only do something if our parent is one of a few types.
            switch (context.Node.Parent)
            {
                // If we're being assigned...
                case AssignmentExpressionSyntax assignParent:
                {
                    if (assignParent.Left != memberAccess)
                        return;
                    break;
                }

                // If we're being invoked...
                case InvocationExpressionSyntax:
                    break;

                // Otherwise, do nothing.
                default:
                    return;
            }

            // Get the friend attribute, if it can't be found do nothing...
            if (context.Compilation.GetTypeByMetadataName(FriendAttribute) is not {} friendAttr)
                return;

            // Get the type that is containing this expression, or, the class where this is happening.
            if (context.ContainingSymbol?.ContainingType is not { } containingType)
                return;

            // We check all of our children and get only the identifiers.
            foreach (var identifier in memberAccess.ChildNodes().Select(node => node as IdentifierNameSyntax))
            {
                if (identifier == null) continue;

                // Get the type info of the identifier, so we can check the attributes...
                if (context.SemanticModel.GetTypeInfo(identifier).ConvertedType is not { } type)
                    continue;

                // Same-type access is always fine.
                if (SymbolEqualityComparer.Default.Equals(type, containingType))
                    continue;

                // Finally, get all attributes of the type, to check if we have any friend classes.
                foreach (var attribute in type.GetAttributes())
                {
                    // If the attribute isn't the friend attribute, continue.
                    if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, friendAttr))
                        continue;

                    // Check all types allowed in the friend attribute. (We assume there's only one constructor arg.)
                    foreach (var constant in attribute.ConstructorArguments[0].Values)
                    {
                        // Check if the value is a type...
                        if (constant.Value is not INamedTypeSymbol t)
                            continue;

                        // If we find that the containing class is specified in the attribute, return! All is good.
                        if (SymbolEqualityComparer.Default.Equals(containingType, t))
                            return;
                    }

                    // Not in a friend class! Report an error.
                    context.ReportDiagnostic(
                        Diagnostic.Create(Rule, context.Node.GetLocation(),
                            $"{context.Node.ToString().Split('.').LastOrDefault()}", $"{type.Name}"));
                }
            }
        }
    }
}
