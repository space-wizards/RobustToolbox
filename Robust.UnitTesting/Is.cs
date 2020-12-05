namespace Robust.UnitTesting
{
    class Is : NUnit.Framework.Is
    {
        public static ApproxEqualityConstraint Approximately(object expected, double? tolerance = null)
        {
            return new(expected, tolerance);
        }
    }
}
