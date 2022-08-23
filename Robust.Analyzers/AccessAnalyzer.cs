using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Shared.Analyzers.Implementation;

namespace Robust.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AccessAnalyzer : DiagnosticAnalyzer
    {
        private const string AccessAttributeType = "Robust.Shared.Analyzers.AccessAttribute";
        private const string PureAttributeType = "System.Diagnostics.Contracts.PureAttribute";

        [SuppressMessage("ReSharper", "RS2008")]
        private static readonly DiagnosticDescriptor AccessRule = new (
            Diagnostics.IdAccess,
            "Invalid access",
            "Tried to perform {0} access to member '{1}' in type '{2}', despite {3} access. {4}.",
            "Usage",
            DiagnosticSeverity.Error,
            true,
            "Make sure to give the accessing type the correct access permissions.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(AccessRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterOperationAction(CheckFriendship,
                OperationKind.FieldReference,
                OperationKind.PropertyReference,
                OperationKind.MethodReference,
                OperationKind.Invocation);
        }

        private void CheckFriendship(OperationAnalysisContext context)
        {
            var operation = context.Operation;

            // The symbol representing the member being accessed.
            ISymbol member;

            // The operation to target when determining access type.
            IOperation targetAccess;

            switch (operation)
            {
                case IMemberReferenceOperation memberRef:
                {
                    member = memberRef.Member;
                    targetAccess = memberRef.Parent;
                    break;
                }

                case IInvocationOperation invocation:
                {
                    member = invocation.TargetMethod;
                    targetAccess = invocation;
                    break;
                }

                default:
                    return;
            }

            // Get the info of the type defining the member, so we can check the attributes later...
            var accessedType = member.ContainingType;

            // Get the attributes
            var friendAttribute = context.Compilation.GetTypeByMetadataName(AccessAttributeType);

            // Get the type that is containing this expression, or, the type where this is happening.
            if (context.ContainingSymbol?.ContainingType is not {} accessingType)
                return;

            // Determine which type of access is happening here... Read, write or execute?
            var accessAttempt = DetermineAccess(context, targetAccess, operation);

            // Check whether this is a "self" access, including inheritors.
            var selfAccess = InheritsFromOrEquals(accessingType, accessedType);

            // Helper function to deduplicate attribute-checking code.
            bool CheckAttributeFriendship(AttributeData attribute, bool isMemberAttribute)
            {
                // If the attribute isn't the friend attribute, we don't care about it.
                if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, friendAttribute))
                    return false;

                var self    = AccessAttribute.SelfDefaultPermissions;
                var friends = AccessAttribute.FriendDefaultPermissions;
                var others  = AccessAttribute.OtherDefaultPermissions;

                foreach (var kv in attribute.NamedArguments)
                {
                    if (kv.Value.Value is not byte value)
                        continue;

                    var permissions = (AccessPermissions) value;

                    switch (kv.Key)
                    {
                        case nameof(AccessAttribute.Self):
                        {
                            self = permissions;
                            break;
                        }

                        case nameof(AccessAttribute.Friend):
                        {
                            friends = permissions;
                            break;
                        }

                        case nameof(AccessAttribute.Other):
                        {
                            others = permissions;
                            break;
                        }

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
                    Diagnostic.Create(AccessRule, operation.Syntax.GetLocation(),
                        $"a{(accessAttempt == AccessPermissions.Execute ? "n" : "")} '{accessAttempt}' {accessingRelation}",
                        $"{member.Name}",
                        $"{accessedType.Name}",
                        $"{(permissionCheck == AccessPermissions.None ? "having no" : $"only having '{permissionCheck}'")}",
                        $"{(isMemberAttribute ? "Member" : "Type")} Permissions: {self.ToUnixPermissions()}{friends.ToUnixPermissions()}{others.ToUnixPermissions()}"));

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
                if(CheckAttributeFriendship(attribute, false))
                    return;
            }
        }

        private static AccessPermissions DetermineAccess(OperationAnalysisContext context, IOperation operation, IOperation original)
        {
            switch (operation)
            {
                case IAssignmentOperation assign:
                {
                    return assign.Target.Equals(original) ? AccessPermissions.Write : AccessPermissions.Read;
                }

                case IInvocationOperation invoke:
                {
                    var pureAttribute = context.Compilation.GetTypeByMetadataName(PureAttributeType);

                    foreach (var attribute in invoke.TargetMethod.GetAttributes())
                    {
                        // Pure methods are treated as read accesses.
                        if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, pureAttribute))
                            return AccessPermissions.Read;
                    }

                    return AccessPermissions.Execute;
                }

                case IMemberReferenceOperation member:
                {
                    return DetermineAccess(context, member.Parent, operation);
                }

                default:
                {
                    return AccessPermissions.Read;
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
