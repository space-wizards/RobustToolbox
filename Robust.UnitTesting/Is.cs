namespace Robust.UnitTesting
{
    sealed class Is : NUnit.Framework.Is
    {
        public static ApproxEqualityConstraint Approximately(object expected, double? tolerance = null)
        {
            return new(expected, tolerance);
        }
    }
}
