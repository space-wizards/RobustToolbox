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
        public const string BestFriendAttribute = "Robust.Shared.Analyzers.BestFriendAttribute";
        public const string FriendAttribute = "Robust.Shared.Analyzers.FriendAttribute";

        [SuppressMessage("ReSharper", "RS2008")]
        private static readonly DiagnosticDescriptor FriendRule = new (
            Diagnostics.IdFriend,
            "Tried to access friend-only member",
            "Tried to access member \"{0}\" in type \"{1}\" which can only be accessed by friend types",
            "Usage",
            DiagnosticSeverity.Error,
            true,
            "Make sure to specify the accessing type in the friends attribute.");

        [SuppressMessage("ReSharper", "RS2008")]
        private static readonly DiagnosticDescriptor BestFriendRule = new (
            Diagnostics.IdBestFriend,
            "Tried to access best-friends-only member",
            "Tried to access member \"{0}\" in type \"{1}\" which can only be accessed by best-friend types",
            "Usage",
            DiagnosticSeverity.Error,
            true,
            "Make sure to specify the accessing type in the best-friends attribute.");

        [SuppressMessage("ReSharper", "RS2008")]
        private static readonly DiagnosticDescriptor MutuallyExclusiveFriendAttributesRule = new (
            Diagnostics.IdMutuallyExclusiveFriendAttributes,
            "Tried to define both the Friend and BestFriend attributes on the same type",
            "Tried to define the mutually-exclusive \"Friend\" and \"BestFriend\" attributes in the same type \"{0}\"",
            "Usage",
            DiagnosticSeverity.Error,
            true,
            "Make sure to only specify one of these two attributes.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(FriendRule, BestFriendRule, MutuallyExclusiveFriendAttributesRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(CheckFriendship, SyntaxKind.SimpleMemberAccessExpression);
            context.RegisterSyntaxNodeAction(CheckFriendshipAttributes,
                SyntaxKind.ClassDeclaration,
                SyntaxKind.InterfaceDeclaration,
                SyntaxKind.StructDeclaration);
        }

        private void CheckFriendshipAttributes(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not TypeDeclarationSyntax typeDeclaration)
                return;

            // Get the attributes
            var friendAttr = context.Compilation.GetTypeByMetadataName(FriendAttribute);
            var bestFriendAttr = context.Compilation.GetTypeByMetadataName(BestFriendAttribute);

            var friendFound = false;
            var bestFriendFound = false;

            foreach (var attributeList in typeDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (context.SemanticModel.GetTypeInfo(attribute).ConvertedType is not {} type)
                        continue;

                    if (SymbolEqualityComparer.Default.Equals(type, friendAttr))
                    {
                        friendFound = true;
                    }

                    if (SymbolEqualityComparer.Default.Equals(type, bestFriendAttr))
                    {
                        bestFriendFound = true;
                    }

                    if (!friendFound || !bestFriendFound)
                        continue;

                    // Mutually exclusive attributes! Report error.
                    context.ReportDiagnostic(
                        Diagnostic.Create(MutuallyExclusiveFriendAttributesRule, attribute.GetLocation(),
                            $"{typeDeclaration.Identifier.Text}"));

                    return;
                }
            }


        }

        private void CheckFriendship(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not MemberAccessExpressionSyntax memberAccess)
                return;

            // Get the attributes
            var friendAttr = context.Compilation.GetTypeByMetadataName(FriendAttribute);
            var bestFriendAttr = context.Compilation.GetTypeByMetadataName(BestFriendAttribute);

            // Get the type that is containing this expression, or, the class where this is happening.
            if (context.ContainingSymbol?.ContainingType is not { } containingType)
                return;

            // We check all of our children and get only the identifiers.
            foreach (var identifier in memberAccess.ChildNodes().Select(node => node as IdentifierNameSyntax))
            {
                if (identifier == null)
                    continue;

                // Get the type info of the identifier, so we can check the attributes...
                if (context.SemanticModel.GetTypeInfo(identifier).ConvertedType is not { } type)
                    continue;

                // Same-type access is always fine.
                if (SymbolEqualityComparer.Default.Equals(type, containingType))
                    continue;

                // Finally, get all attributes of the type, to check if we have any friend types.
                foreach (var attribute in type.GetAttributes())
                {
                    var bestFriend = false;

                    // If the attribute isn't the friend attribute, continue.
                    if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, friendAttr))
                    {
                        if(!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, bestFriendAttr))
                            continue;

                        // This is the best friend attribute instead!
                        bestFriend = true;
                    }

                    // If we're working with a Friend attribute, we only care about write/execute.
                    if (!bestFriend)
                    {
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
                                continue;
                        }
                    }

                    // Check all types allowed in the friend attribute. (We assume there's only one constructor arg.)
                    foreach (var constant in attribute.ConstructorArguments[0].Values)
                    {
                        // Check if the value is a type...
                        if (constant.Value is not INamedTypeSymbol t)
                            continue;

                        // If we find that the containing class is specified in the attribute, return! All is good.
                        if (InheritsFromOrEquals(containingType, t))
                            return;
                    }

                    // Not in a friend class! Report an error.
                    context.ReportDiagnostic(
                        Diagnostic.Create(bestFriend ? BestFriendRule : FriendRule, context.Node.GetLocation(),
                            $"{context.Node.ToString().Split('.').LastOrDefault()}", $"{type.Name}"));
                }
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
    }
}
