#nullable enable
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
public sealed class ProxyForAnalyzer : DiagnosticAnalyzer
{
    private const string ProxyForAttributeType = "Robust.Shared.Analyzers.ProxyForAttribute";
    private const string ProxyForName = "ProxyFor";

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
        "Unable to find a method named {0} with a matching signature ({2}) on target {1}",
        "Usage",
        DiagnosticSeverity.Error,
        true,
        "Make sure a method exists with the target name."
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
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterSyntaxNodeAction(AnalyzeDeclaration, SyntaxKind.MethodDeclaration);
    }

    /// <summary>
    /// Check for failures to use proxy methods when available.
    /// </summary>
    private void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation operation)
            return;

        // Get the method being invoked
        var invokedMethod = operation.TargetMethod;
        // Get the class this code is in
        var containingType = context.ContainingSymbol.ContainingType;

        // Find all methods in this class and its parents
        HashSet<IMethodSymbol> methods = [];
        // Start search from the parent of this class, so we don't have violations on the proxy methods themselves
        var baseType = containingType.BaseType;
        while (baseType != null && baseType.SpecialType == SpecialType.None)
        {
            methods.UnionWith(baseType.GetMembers().Where(m => m is IMethodSymbol method).Cast<IMethodSymbol>());
            baseType = baseType.BaseType;
        }

        // Check each method we found
        foreach (var method in methods)
        {
            // We only care about methods with the ProxyFor attribute
            if (!AttributeHelper.HasAttribute(method, ProxyForAttributeType, out var attributeData))
                continue;

            // Make sure the Type specified by the attribute is the one containing the method being invoked
            var targetType = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol;
            if (!SymbolEqualityComparer.Default.Equals(targetType, invokedMethod.ContainingType))
                continue;

            // Make sure the method name specified by the attribute is same as the one being invoked
            var targetMethod = attributeData.ConstructorArguments[1].Value as string;
            // If no name was specified, use the name of the proxy method
            targetMethod ??= method.Name;
            if (targetMethod != invokedMethod.Name)
                continue;

            // Make sure this method has the same signature as the one being invoked
            if (!DoSignaturesMatch(invokedMethod, method))
                continue;

            context.ReportDiagnostic(Diagnostic.Create(
                PreferProxyDescriptor,
                operation.Syntax.GetLocation(),
                method.MetadataName,
                $"{invokedMethod.ContainingType.Name}.{invokedMethod.Name}"
            ));

            // We should only need to report one violation
            break;
        }
    }

    /// <summary>
    /// Check for incorrect use of the attribute.
    /// </summary>
    private void AnalyzeDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not MethodDeclarationSyntax declarationSyntax)
            return;

        if (context.SemanticModel.GetDeclaredSymbol(declarationSyntax) is not { } methodSymbol)
            return;

        // We only care about methods that have our attribute
        if (!AttributeHelper.HasAttribute(methodSymbol, ProxyForAttributeType, out var attribute))
            return;

        // Get the syntax node for the attribute
        TryGetAttributeSyntax(declarationSyntax, ProxyForName, out var attributeSyntax);

        // Try to get the set method name from the attribute constructor
        var targetMethodName = attribute.ConstructorArguments[1].Value as string;
        // Check for a redundant set method name
        if (targetMethodName == methodSymbol.Name)
        {
            var location = attributeSyntax?.ArgumentList?.Arguments[1].GetLocation() ?? declarationSyntax.GetLocation();
            context.ReportDiagnostic(Diagnostic.Create(
                RedundantMethodNameDescriptor,
                location
            ));
        }
        // Fall back to the method name
        targetMethodName ??= methodSymbol.Name;

        // Get the target Type specified by the attribute
        if (attribute.ConstructorArguments[0].Value is not ITypeSymbol targetType)
            return;

        // Find all methods belonging to the target Type that have the right name
        var members = targetType.GetMembers(targetMethodName).Where(m => m is IMethodSymbol).Cast<IMethodSymbol>();

        // Find the location of the argument's node
        var targetArgumentLocation = attributeSyntax?.ArgumentList?.Arguments[0].GetLocation() ?? declarationSyntax.GetLocation();

        // Make sure there's a method with the right name and matching signature
        var found = false;
        foreach (var member in members)
        {
            if (DoSignaturesMatch(member, methodSymbol))
                found = true;
        }
        if (!found)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                TargetMethodNotFoundDescriptor,
                targetArgumentLocation,
                targetMethodName,
                targetType.Name//,
                //string.Join(", ", methodSymbol.Parameters.Select(p => p.ToDisplayString()))
            ));
        }
    }

    private bool DoSignaturesMatch(IMethodSymbol first, IMethodSymbol second)
    {
        // Make sure the number of type arguments is the same
        if (first.TypeArguments.Length != second.TypeArguments.Length)
            return false;

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

    private bool TryGetAttributeSyntax(MethodDeclarationSyntax declarationSyntax, string attributeName, [NotNullWhen(true)] out AttributeSyntax? syntax)
    {
        foreach (var list in declarationSyntax.AttributeLists)
        {
            foreach (var attribute in list.Attributes)
            {
                if (attribute.Name.ToString() == attributeName)
                {
                    syntax = attribute;
                    return true;
                }
            }
        }
        syntax = null;
        return false;
    }
}
