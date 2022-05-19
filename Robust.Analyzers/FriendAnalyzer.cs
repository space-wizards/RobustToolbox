using System.Collections.Generic;
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
        private const string BestFriendAttribute = "Robust.Shared.Analyzers.BestFriendAttribute";
        private const string FriendAttribute = "Robust.Shared.Analyzers.FriendAttribute";

        [SuppressMessage("ReSharper", "RS2008")]
        private static readonly DiagnosticDescriptor FriendRule = new (
            Diagnostics.IdFriend,
            "Tried to access friend-only member",
            "Tried to access member \"{0}\" in type \"{1}\" which can only be accessed by friend types",
            "Usage",
            DiagnosticSeverity.Error,
            true,
            "Make sure to specify the accessing type in the friends attribute.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(FriendRule);

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

            // Get the attributes
            var friendAttr = context.Compilation.GetTypeByMetadataName(FriendAttribute);
            var bestFriendAttr = context.Compilation.GetTypeByMetadataName(BestFriendAttribute);

            // Get the type that is containing this expression, or, the type where this is happening.
            if (context.ContainingSymbol?.ContainingType is not {} containingType)
                return;

            // True if read access, false if write or execute access.
            var read =
                // Being invoked.
                context.Node.Parent is not InvocationExpressionSyntax &&
                // Being assigned.
                !(context.Node.Parent is AssignmentExpressionSyntax assignParent && assignParent.Left == memberAccess);

            // Get the syntax representing the member being accessed
            var memberIdentifier = memberAccess.Name;

            // Get the expression representing the object that the member being accessed belongs to.
            var typeIdentifier = memberAccess.Expression;

            if (context.SemanticModel.GetSymbolInfo(memberIdentifier).Symbol is not {} member)
                return;

            // Get the info of the type defining the member, so we can check the attributes...
            if (context.SemanticModel.GetTypeInfo(typeIdentifier).ConvertedType is not { } type)
                return;

            // Same-type access is always fine.
            if (SymbolEqualityComparer.Default.Equals(type, containingType))
                return;

            // Helper function to deduplicate attribute-checking code.
            bool CheckAttributeFriendship(AttributeData attribute)
            {
                var bestFriend = false;

                // If the attribute isn't the friend attribute, we don't care about it.
                // We also assume there's only one Friend/BestFriend attribute here, as they're mutually exclusive.
                if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, friendAttr))
                {
                    if(!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, bestFriendAttr))
                        return false;

                    // This is the best friend attribute instead!
                    bestFriend = true;
                }

                // If we're working with a Friend attribute, we only care about write/execute.
                if (!bestFriend && read)
                    return true;

                // Check all types allowed in the friend attribute. (We assume there's only one constructor arg.)
                var types = attribute.ConstructorArguments[0].Values;

                // There are no specified types, therefore
                if (types.Length == 0)
                    return true;

                foreach (var constant in types)
                {
                    // Check if the value is a type...
                    if (constant.Value is not INamedTypeSymbol t)
                        continue;

                    // If we find that the containing type is specified in the attribute, return! All is good.
                    if (InheritsFromOrEquals(containingType, t))
                        return true;
                }

                // Not in a friend type! Report an error.
                context.ReportDiagnostic(
                    Diagnostic.Create(FriendRule, context.Node.GetLocation(),
                        $"{context.Node.ToString().Split('.').LastOrDefault()}", $"{type.Name}"));

                // Only return ONE error.
                return true;
            }

            // Check attributes in the member first, since they take priority and can override type restrictions.
            foreach (var attribute in member.GetAttributes())
            {
                if(CheckAttributeFriendship(attribute))
                    return;
            }

            // Check attributes in the type containing the member last.
            foreach (var attribute in type.GetAttributes())
            {
                if(CheckAttributeFriendship(attribute))
                    return;
            }
        }

        private bool InheritsFromOrEquals(INamedTypeSymbol type, INamedTypeSymbol baseType)
        {
            foreach (var otherType in GetBaseTypesAndThis(type))
            {
                if (SymbolEqualityComparer.Default.Equals(otherType, baseType))
                    return true;
            }

            return false;
        }

        private IEnumerable<INamedTypeSymbol> GetBaseTypesAndThis(INamedTypeSymbol namedType)
        {
            var current = namedType;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        private string GetPrettyNodeName(SyntaxNode node)
        {
            return node switch
            {
                TypeDeclarationSyntax type => type.Identifier.Text,
                PropertyDeclarationSyntax property => property.Identifier.Text,
                MethodDeclarationSyntax method => method.Identifier.Text,
                ConstructorDeclarationSyntax constructor => constructor.Identifier.Text,
                FieldDeclarationSyntax field => field.Declaration.Variables.ToString(),
                _ => node.ToString()
            };
        }
    }
}
