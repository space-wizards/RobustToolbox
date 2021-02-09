using Microsoft.CodeAnalysis;

namespace Robust.Generators
{
    public static class Diagnostics
    {
        public static Diagnostic DebugDiag(string msg, Location loc = null) => Diagnostic.Create(
            new DiagnosticDescriptor("RADC9999", "", msg, "Usage", DiagnosticSeverity.Error, true), loc ?? Location.None);

        public static SuppressionDescriptor YamlMeansImplicitUse =>
            new SuppressionDescriptor("RADC1000", "CS0649", "Used by ComponentDataManager.");

        public static DiagnosticDescriptor InvalidYamlAttrTarget = new DiagnosticDescriptor(
            "RADC0000",
            "",
            $"YamlFieldAttribute assigned for Member which is neither Field or Property! It will be ignored.",
            "Usage",
            DiagnosticSeverity.Warning,
            true);

        public static DiagnosticDescriptor FailedCustomDataClassAttributeResolve = new DiagnosticDescriptor(
            "RADC0001",
            "",
            $"Could not resolve CustomDataClassAttribute",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public static DiagnosticDescriptor InvalidYamlTag = new DiagnosticDescriptor(
            "RADC0003",
            "",
            $"YamlFieldAttribute has an invalid tag.",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public static DiagnosticDescriptor DataClassNotFound = new DiagnosticDescriptor(
            "RADC0004",
            "",
            "Dataclass not found.",
            "Usage",
            DiagnosticSeverity.Error,
            true);
    }
}
