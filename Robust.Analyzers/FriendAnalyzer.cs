using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Robust.Shared.Analyzers;

namespace Robust.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FriendAnalyzer : DiagnosticAnalyzer
    {
        private const string FriendAttributeType = "Robust.Shared.Analyzers.FriendAttribute";

        [SuppressMessage("ReSharper", "RS2008")]
        private static readonly DiagnosticDescriptor FriendRule = new (
            Diagnostics.IdFriend,
            "Invalid member access",
            "Tried to perform {0} access to member \"{1}\" in type \"{2}\", despite {3} access. {4}",
            "Usage",
            DiagnosticSeverity.Error,
            true,
            "Make sure to give the accessing type the correct access permissions.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(FriendRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(CheckFriendship, SyntaxKind.ExpressionStatement);
        }

        private void CheckFriendship(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not ExpressionStatementSyntax expr)
                return;

            // The syntax representing the member being accessed
            if (context.SemanticModel.GetSymbolInfo(expr.Expression.ChildNodes().First()).Symbol is not {} member)
                return;

            // Get the info of the type defining the member, so we can check the attributes later...
            var accessedType = member.ContainingType;

            // Expression representing the object that the member being accessed belongs to.
            var typeIdentifier = expr.Expression;

            // The syntax to target when determining access attempt type.
            var targetAccess = typeIdentifier;

            // Get the attributes
            var friendAttribute = context.Compilation.GetTypeByMetadataName(FriendAttributeType);

            // Get the type that is containing this expression, or, the type where this is happening.
            if (context.ContainingSymbol?.ContainingType is not {} accessingType)
                return;

            // Determine which type of access is happening here.
            var accessAttempt = targetAccess switch
            {
                InvocationExpressionSyntax => AccessPermissions.Execute,
                AssignmentExpressionSyntax assign when assign.Left == typeIdentifier => AccessPermissions.Write,
                _ => AccessPermissions.Read
            };


            // Check whether this is a "self" access.
            var selfAccess = SymbolEqualityComparer.Default.Equals(accessedType, accessingType);

            // Helper function to deduplicate attribute-checking code.
            bool CheckAttributeFriendship(AttributeData attribute, bool isMember = false)
            {
                // If the attribute isn't the friend attribute, we don't care about it.
                if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, friendAttribute))
                    return false;

                var self    = FriendAttribute.SelfDefaultPermissions;
                var friends = FriendAttribute.FriendDefaultPermissions;
                var others  = FriendAttribute.OtherDefaultPermissions;

                foreach (var kv in attribute.NamedArguments)
                {
                    if (kv.Value.Value is not byte value)
                        continue;

                    var permissions = (AccessPermissions) value;

                    switch (kv.Key)
                    {
                        case nameof(FriendAttribute.Self):
                            self = permissions;
                            break;

                        case nameof(FriendAttribute.Friend):
                            friends = permissions;
                            break;

                        case nameof(FriendAttribute.Other):
                            others = permissions;
                            break;

                        default:
                            continue;
                    }
                }

                // By default, we will check the "other" permissions unless we find we're dealing with a friend or self.
                var permissionCheck = others;

                // Human-readable relation between accessing and accessed types.
                var accessingRelation = "other-type";

                if (!selfAccess)
                {
                    // This is not a self-access, so we need to determine whether the accessing type is a friend.
                    // Check all types allowed in the friend attribute. (We assume there's only one constructor arg.)
                    var types = attribute.ConstructorArguments[0].Values;

                    foreach (var constant in types)
                    {
                        // Check if the value is a type...
                        if (constant.Value is not INamedTypeSymbol friendType)
                            continue;

                        // Check if the accessing type is specified in the attribute...
                        if (!InheritsFromOrEquals(accessingType, friendType))
                            continue;

                        // Set the permissions check to the friend permissions!
                        permissionCheck = friends;
                        accessingRelation = "friend-type";
                        break;
                    }
                }
                else
                {
                    // Self-access, so simply set the permissions check to self.
                    permissionCheck = self;
                    accessingRelation = "same-type";
                }

                // If we allow this access, return! All is good.
                if ((accessAttempt & permissionCheck) != 0)
                    return true;

                // Access denied! Report an error.
                context.ReportDiagnostic(
                    Diagnostic.Create(FriendRule, context.Node.GetLocation(),
                        $"a{(accessAttempt == AccessPermissions.Execute ? "n" : "")} \"{accessAttempt}\" {accessingRelation}",
                        $"{member.Name}",
                        $"{accessedType.Name}",
                        $"{(permissionCheck == AccessPermissions.None ? "having no" : $"only having \"{permissionCheck}\"")}",
                        $"{(isMember ? "Member" : "Type")} Permissions: {self.ToUnixPermissions()}{friends.ToUnixPermissions()}{others.ToUnixPermissions()}"));

                // Only return ONE error.
                return true;
            }

            // Check attributes in the member first, since they take priority and can override type restrictions.
            foreach (var attribute in member.GetAttributes())
            {
                if(CheckAttributeFriendship(attribute, true))
                    return;
            }

            // Check attributes in the type containing the member last.
            foreach (var attribute in accessedType.GetAttributes())
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
    }
}
