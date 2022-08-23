using NUnit.Framework.Constraints;
using Robust.Shared.Maths;

namespace Robust.UnitTesting
{
    public sealed class ApproxEqualityConstraint : Constraint
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
                if (Expected is float f1 && actual is float f2)
                {
                    if (Tolerance != null)
                    {
                        return new ConstraintResult(this, actual, MathHelper.CloseToPercent(f1, f2, Tolerance.Value));
                    }

                    return new ConstraintResult(this, actual, MathHelper.CloseToPercent(f1, f2));
                }

                if (Expected is double d1 && actual is float d2)
                {
                    if (Tolerance != null)
                    {
                        return new ConstraintResult(this, actual, MathHelper.CloseToPercent(d1, d2, Tolerance.Value));
                    }

                    return new ConstraintResult(this, actual, MathHelper.CloseToPercent(d1, d2));
                }

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
