using Microsoft.CodeAnalysis;

namespace Robust.Analyzers;

public static class Diagnostics
{
    public const string IdExplicitInterface = "RA0000";
    public const string IdSerializable = "RA0001";
    public const string IdAccess = "RA0002";
    public const string IdExplicitVirtual = "RA0003";
    public const string IdTaskResult = "RA0004";
    public const string IdServ4NotNullableFlagNotPresent = "RA0005";
    public const string IdServ4NotNullableFlag = "RA0006";
    public const string IdServ4InvalidNotNullableFlag = "RA0007";

    public static SuppressionDescriptor MeansImplicitAssignment =>
        new SuppressionDescriptor("RADC1000", "CS0649", "Marked as implicitly assigned.");
}
