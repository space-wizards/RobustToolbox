using Microsoft.CodeAnalysis;

namespace Robust.Analyzers;

public static class Diagnostics
{
    public const string IdExplicitInterface = "RA0000";
    public const string IdSerializable = "RA0001";
    public const string IdAccess = "RA0002";
    public const string IdExplicitVirtual = "RA0003";
    public const string IdTaskResult = "RA0004";
    public const string IdUseGenericVariant = "RA0005";
    public const string IdUseGenericVariantInvalidUsage = "RA0006";
    public const string IdUseGenericVariantAttributeValueError = "RA0007";
    public const string IdNotNullableFlagNotSet = "RA0008";
    public const string IdInvalidNotNullableFlagValue = "RA0009";
    public const string IdInvalidNotNullableFlagImplementation = "RA0010";
    public const string IdInvalidNotNullableFlagType = "RA0011";
    public const string IdNotNullableFlagValueType = "RA0012";

    public static SuppressionDescriptor MeansImplicitAssignment =>
        new SuppressionDescriptor("RADC1000", "CS0649", "Marked as implicitly assigned.");
}
