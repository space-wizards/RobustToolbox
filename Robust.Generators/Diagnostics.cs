using Microsoft.CodeAnalysis;

namespace Robust.Generators
{
    public static class Diagnostics
    {
        public static DiagnosticDescriptor InvalidYamlAttrTarget(string member, string symbol) => new DiagnosticDescriptor(
            "RADC0000",
            "",
            $"YamlFieldAttribute assigned for Member {member} of type {symbol} which is neither Field or Property! It will be ignored.",
            "Usage",
            DiagnosticSeverity.Warning,
            true);

        public static DiagnosticDescriptor FailedCustomDataClassAttributeResolve(string text) => new DiagnosticDescriptor(
            "RADC0001",
            "",
            $"Could not resolve CustomDataClassAttribute for class {text}",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public static DiagnosticDescriptor InvalidDeepCloneImpl(string invalidAssignment) => new DiagnosticDescriptor(
            "RADC0002",
            "",
            $"Invalid assignment found in DeepClone implementation: {invalidAssignment}",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public static DiagnosticDescriptor InvalidYamlTag(string member, string symbol, string fieldName) => new DiagnosticDescriptor(
            "RADC0003",
            "",
            $"YamlFieldAttribute for Member {member} of type {symbol} has an invalid tag {fieldName}.",
            "Usage",
            DiagnosticSeverity.Error,
            true);

        public static DiagnosticDescriptor NoDeepCloneImpl(string symbol) => new DiagnosticDescriptor(
            "RADC0004",
            "",
            $"{symbol} should implement IDeepClone.DeepClone",
            "Usage",
            DiagnosticSeverity.Error,
            true);
    }
}
