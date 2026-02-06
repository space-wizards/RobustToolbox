#nullable enable
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProxyForAnalyzer : DiagnosticAnalyzer
{
    private const string ProxyForAttributeType = "Robust.Shared.Analyzers.ProxyForAttribute";

    public static readonly string ProxyMethodName = "proxy";

    public static readonly DiagnosticDescriptor PreferProxyDescriptor = new(
        Diagnostics.IdPreferProxy,
        "Use the proxy method",
        "Use the proxy method {0} instead of calling {1} directly",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        "Use the proxy method."
    );

    public static readonly DiagnosticDescriptor RedundantMethodNameDescriptor = new(
        Diagnostics.IdProxyForRedundantMethodName,
        "Method name is redundant",
        "Set method name matches the proxy method name and can be omitted",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        "Remove the method name from the attribute."
    );

    public static readonly DiagnosticDescriptor TargetMethodNotFoundDescriptor = new(
        Diagnostics.IdProxyForTargetMethodNotFound,
        "Target method not found",
        "Unable to find target method {0}",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure a method exists with the target name and matching signature."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        PreferProxyDescriptor,
        RedundantMethodNameDescriptor,
        TargetMethodNotFoundDescriptor,
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics | GeneratedCodeAnalysisFlags.Analyze);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static ctx =>
        {
            if (ctx.Compilation.GetTypeByMetadataName(ProxyForAttributeType) is not { } proxyForAttributeType)
                return;

            ctx.RegisterSymbolStartAction(symbolContext =>
            {
                // We only care about classes
                if (symbolContext.Symbol is not INamedTypeSymbol typeSymbol || typeSymbol.TypeKind != TypeKind.Class)
                    return;

                // Find information about all marked proxy methods available to this class
                if (!TryGetProxyMethods(typeSymbol, proxyForAttributeType, out var proxyMethods))
                    return;

                // No proxy methods are available to this class, so we're done
                if (proxyMethods.Length == 0)
                    return;

                // Pass proxy method information to the analyzer state
                var state = new AnalyzerState(proxyMethods);
                // Analyze each method invocation within the class
                symbolContext.RegisterOperationAction(state.AnalyzeInvocation, OperationKind.Invocation);
            }, SymbolKind.NamedType);

            ctx.RegisterOperationAction(operationContext => AnalyzeAttribute(operationContext, proxyForAttributeType), OperationKind.Attribute);
        });

    }

    /// <summary>
    /// Data about a proxy method and its target.
    /// </summary>
    private record class ProxyMethod(
        IMethodSymbol Method,
        INamedTypeSymbol TargetType,
        string TargetMethod
    );

    /// <summary>
    /// Returns information about all proxy methods available to the specified class.
    /// </summary>
    private static bool TryGetProxyMethods(INamedTypeSymbol typeSymbol, INamedTypeSymbol proxyForAttribute, [NotNullWhen(true)] out ProxyMethod[]? proxyMethods)
    {
        proxyMethods = null;

        // Don't fault the proxy type for not using its own proxy methods
        if (typeSymbol.BaseType is null)
            return false;

        HashSet<ProxyMethod> proxySet = [];
        // Search for methods in each type this inherits from
        foreach (var baseType in TypeSymbolHelper.GetBaseTypes(typeSymbol))
        {
            HashSet<ProxyMethod> classMethods = [];
            // Check each member
            foreach (var member in baseType.GetMembers())
            {
                // We only care about methods
                if (member is not IMethodSymbol method)
                    continue;

                // Make sure the method is marked as a proxy
                if (!AttributeHelper.HasAttribute(method, proxyForAttribute, out var attributeData))
                    continue;

                var targetType = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol;
                var targetMethod = attributeData.ConstructorArguments[1].Value as string ?? member.Name;

                classMethods.Add(new ProxyMethod(method, targetType!, targetMethod));
            }
            proxySet.UnionWith(classMethods);
        }

        if (proxySet.Count == 0)
            return false;

        proxyMethods = proxySet.ToArray();
        return true;
    }

    private sealed class AnalyzerState(ProxyMethod[] ProxyMethods)
    {
        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            if (context.Operation is not IInvocationOperation operation)
                return;

            // Make sure the invocation is happening on a member, not a parameter or something else
            if (operation.Instance is not IMemberReferenceOperation reference)
                return;

            // Make sure the member belongs to the proxy class
            if (!TypeSymbolHelper.Inherits(context.ContainingSymbol.ContainingType, reference.Member.ContainingType))
                return;

            // Get the method being invoked
            var invokedMethod = operation.TargetMethod;

            // Check each method we found
            foreach (var (method, targetType, targetMethod) in ProxyMethods)
            {
                // Make sure the Type specified by the attribute is the one containing the method being invoked
                if (!SymbolEqualityComparer.Default.Equals(targetType, invokedMethod.ContainingType))
                    continue;

                // Make sure the method name specified by the attribute is same as the one being invoked
                if (targetMethod != invokedMethod.Name)
                    continue;

                // Make sure this method has the same signature as the one being invoked
                if (!DoSignaturesMatch(invokedMethod, method))
                    continue;

                var props = new Dictionary<string, string?>
                {
                    { ProxyMethodName, method.Name }
                };

                context.ReportDiagnostic(Diagnostic.Create(
                    PreferProxyDescriptor,
                    operation.Syntax.GetLocation(),
                    props.ToImmutableDictionary(),
                    method.MetadataName,
                    $"{invokedMethod.ContainingType.Name}.{invokedMethod.Name}"
                ));

                // We should only need to report one violation
                break;
            }
        }
    }

    private static void AnalyzeAttribute(OperationAnalysisContext context, INamedTypeSymbol proxyForAttribute)
    {
        if (context.ContainingSymbol is not IMethodSymbol methodSymbol)
            return;

        if (context.Operation is not IAttributeOperation operation)
            return;

        if (operation.Syntax is not AttributeSyntax attributeSyntax)
            return;

        if (operation.Operation is not IObjectCreationOperation creationOperation)
            return;

        // Make sure we're looking at the right attribute
        if (!SymbolEqualityComparer.Default.Equals(creationOperation.Type, proxyForAttribute))
            return;

        // Get the target Type specified by the attribute
        if ((creationOperation.Arguments[0].Value as ITypeOfOperation)?.TypeOperand is not { } targetType)
            return;

        // Try to get the set method name from the attribute constructor
        var targetMethodName = creationOperation.Arguments[1].Value.ConstantValue.Value as string;
        // Check for a redundant set method name
        if (targetMethodName == methodSymbol.Name)
        {
            var location = creationOperation.Arguments[1].Syntax.GetLocation();
            context.ReportDiagnostic(Diagnostic.Create(
                RedundantMethodNameDescriptor,
                location
            ));
        }
        // Fall back to the method name
        targetMethodName ??= methodSymbol.Name;

        // Find all methods belonging to the target Type that have the right name
        var members = targetType.GetMembers(targetMethodName).Where(m => m is IMethodSymbol).Cast<IMethodSymbol>();

        // Find the location of the argument's node
        var targetArgumentLocation = creationOperation.Arguments[0].Syntax.GetLocation();

        // Make sure there's a method with the right name and matching signature
        var found = false;
        foreach (var member in members)
        {
            if (DoSignaturesMatch(member, methodSymbol))
                found = true;
        }
        if (!found)
        {
            var methodParams = methodSymbol.Parameters.Length > 0 ? methodSymbol.Parameters.Select(p => p.ToDisplayString()) : [];
            var methodSignature = $"{targetType.Name}.{targetMethodName}({string.Join(", ", methodParams)})";
            context.ReportDiagnostic(Diagnostic.Create(
                TargetMethodNotFoundDescriptor,
                targetArgumentLocation,
                methodSignature
            ));
        }
    }

    private static bool DoSignaturesMatch(IMethodSymbol first, IMethodSymbol second)
    {
        // Make sure the number of type arguments is the same
        if (first.TypeArguments.Length != second.TypeArguments.Length)
            return false;

        // Make sure any type constraints on the methods are the same
        for (var i = 0; i < first.TypeParameters.Length; i++)
        {
            var firstConstraints = first.TypeParameters[i].ConstraintTypes;
            var secondConstraints = second.TypeParameters[i].ConstraintTypes;
            for (var j = 0; j < firstConstraints.Length; j++)
            {
                if (!SymbolEqualityComparer.Default.Equals(firstConstraints[j], secondConstraints[j]))
                    return false;
            }
        }

        // Convert any type arguments in second to use the types of first
        if (second.IsGenericMethod)
            second = second.Construct(first.TypeArguments, first.TypeArgumentNullableAnnotations);

        // Filter out any optional parameters
        var firstParams = first.Parameters.Where(p => !p.IsOptional).ToArray();
        var secondParams = second.Parameters.Where(p => !p.IsOptional).ToArray();

        // A different number of parameters means no match
        if (firstParams.Length != secondParams.Length)
            return false;

        for (var i = 0; i < firstParams.Length; i++)
        {
            // Check if the parameter type is a generic type symbol (like T, TComp, etc.)
            if (firstParams[i].Type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                // If the compared parameter also is a generic type symbol, consider that a match
                if (secondParams[i].Type is INamedTypeSymbol namedTypeSecond && namedTypeSecond.IsGenericType)
                    continue;

                // Otherwise, no match
                return false;
            }

            // Make sure the Types match
            if (!SymbolEqualityComparer.IncludeNullability.Equals(firstParams[i].Type, secondParams[i].Type))
                return false;
        }

        return true;
    }
}
