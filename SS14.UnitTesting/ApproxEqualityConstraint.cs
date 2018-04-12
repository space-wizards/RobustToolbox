using NUnit.Framework.Constraints;
using SS14.Shared.Maths;

namespace SS14.UnitTesting
{
    public class ApproxEqualityConstraint : Constraint
    {
        public object Expected { get; }
        public double? Tolerance { get; }

        public ApproxEqualityConstraint(object expected, double? tolerance = null)
        {
            Expected = expected;
            Tolerance = tolerance;
        }

        public override ConstraintResult ApplyTo<TActual>(TActual actual)
        {
            if (!(Expected is IApproxEquatable<TActual> equatable))
            {
                return new ConstraintResult(this, actual, false);
            }

            if (Tolerance != null)
            {
                return new ConstraintResult(this, actual, equatable.EqualsApprox(actual, Tolerance.Value));
            }
            else
            {
                return new ConstraintResult(this, actual, equatable.EqualsApprox(actual));
            }
        }

        public override string Description => $"approximately {Expected}";
    }
}
