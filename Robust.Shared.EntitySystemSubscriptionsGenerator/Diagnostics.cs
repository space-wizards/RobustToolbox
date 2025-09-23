using Microsoft.CodeAnalysis;

namespace Robust.Shared.EntitySystemSubscriptionsGenerator;

public static class Diagnostics
{
    private const DiagnosticSeverity Sev = DiagnosticSeverity.Warning;

    public static readonly DiagnosticDescriptor BadMethodSignature = new(
        "cent2",
        "Invalid method signature",
        "Method signature is incompatible with required delegate type(s) for \"{0}\". Compatible types are: {1}.",
        "Usage",
        Sev,
        true
    );

    public static readonly DiagnosticDescriptor NotIEntitySystem = new(
        "cent4",
        $"Method not in {KnownTypes.IEntitySystemTypeName}",
        $"Method is declared in type \"{{0}}\" which does not implement {KnownTypes.IEntitySystemTypeName}",
        "Usage",
        Sev,
        true
    );

    public static readonly DiagnosticDescriptor NotPartial = new(
        "cent5",
        "Containing class must be declared as Partial",
        $"Method is declared in type \"{{0}}\" which is not Partial",
        "Usage",
        Sev,
        true
    );
}
