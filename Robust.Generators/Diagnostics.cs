using Microsoft.CodeAnalysis;

namespace Robust.Generators
{
    public static class Diagnostics
    {
        public static Diagnostic DebugDiag(string msg, Location loc = null) => Diagnostic.Create(
            new DiagnosticDescriptor("RADC9999", "", msg, "Usage", DiagnosticSeverity.Error, true), loc ?? Location.None);

        public static SuppressionDescriptor MeansImplicitAssignment =>
            new SuppressionDescriptor("RADC1000", "CS0649", "Marked as implicitly assigned.");

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

        public static DiagnosticDescriptor UnsupportedValue = new DiagnosticDescriptor(
            "RADC0002",
            "",
            $"Unsupported Value used in YamlFieldAttribute.",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public static DiagnosticDescriptor FailedYamlFieldResolve = new DiagnosticDescriptor(
            "RADC0003",
            "",
            $"Failed resolving the SourceText of YamlField.",
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
