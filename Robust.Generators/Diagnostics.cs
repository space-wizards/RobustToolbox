using Microsoft.CodeAnalysis;

namespace Robust.Generators
{
    public static class Diagnostics
    {
        public static SuppressionDescriptor MeansImplicitAssignment =>
            new SuppressionDescriptor("RADC1000", "CS0649", "Marked as implicitly assigned.");
    }
}
