using System;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Robust.Shared.Maths
{
    /// <summary>
    ///     A representation of an angle, in radians.
    /// </summary>
    [Serializable]
    public readonly struct Angle : IApproxEquatable<Angle>, IEquatable<Angle>, ISpanFormattable
    {
        public static Angle Zero { get; } = new();

        /// <summary>
        ///     Angle in radians.
        /// </summary>
        public readonly double Theta;

        private const double PiOver2 = Math.PI / 2.0;

        private const double Segment = 2 * Math.PI / 8.0; // Cut the circle into 8 pieces
        private const double Offset = Segment / 2.0; // offset the pieces by 1/2 their size
        private const double InvSegment = 1.0 / Segment;

        private const double CardinalSegment = 2 * Math.PI / 4.0; // Cut the circle into 4 pieces
        private const double CardinalOffset = CardinalSegment / 2.0; // offset the pieces by 1/2 their size
        private const double InvCardinalSegment = 1.0 / CardinalSegment;

        /// <summary>
        ///     Angle in degrees.
        /// </summary>
        public double Degrees => MathHelper.RadiansToDegrees(Theta);

        /// <summary>
        ///     Constructs an instance of an Angle.
        /// </summary>
        /// <param name="theta">The angle in radians.</param>
        public Angle(double theta)
        {
            Theta = theta;
        }

        /// <summary>
        ///     Constructs an instance of an angle from an (un)normalized direction vector.
        /// </summary>
        /// <param name="dir"></param>
        public Angle(Vector2 dir)
        {
            Theta = Math.Atan2(dir.Y, dir.X);
        }

        /// <summary>
        /// Converts a world-vector (where south is 0,-1) into an angle.
        /// </summary>
        [Pure]
        public static Angle FromWorldVec(Vector2 dir)
        {
            return new Angle(Math.Atan2(dir.Y, dir.X) + PiOver2);
        }

        /// <summary>
        ///     Converts this angle to a unit direction vector.
        /// </summary>
        /// <remarks>
        ///     This is in "normal" cartesian trigonometry, with an angle of zero being (1, 0).
        ///     Use <see cref="ToWorldVec"/> for in-world calculations
        ///     where an angle of zero is usually considered "south" (0, -1).
        /// </remarks>
        /// <returns>Unit Direction Vector</returns>
        [Pure]
        public Vector2 ToVec()
        {
            var (y, x) = Math.SinCos(Theta);
            return new Vector2((float)x, (float)y);
        }

        /// <summary>
        /// Converts this angle to a unit direction vector in world-terms i.e. south is 0.
        /// </summary>
        /// <returns></returns>
        [Pure]
        public Vector2 ToWorldVec()
        {
            var theta = Theta - PiOver2;
            var (y, x) = Math.SinCos(theta);
            return new Vector2((float)x, (float)y);
        }

        /// <summary>
        /// Gets the angle in a cardinal direction.
        /// </summary>
        [Pure]
        public Direction GetDir()
        {
            return GetDir(Theta);
        }

        /// <summary>
        /// Rotates a direction by the specified direction.
        /// </summary>
        /// <param name="dir"></param>
        [Pure]
        public Direction RotateDir(Direction dir)
        {
            return (Direction)(((int)GetDir() + (int)dir) & 7);
        }

        /// <summary>
        /// Gets the angle in a cardinal direction.
        /// </summary>
        [Pure]
        public Direction GetCardinalDir()
        {
            var ang = Theta % Math.Tau;
            if (ang < 0) // convert -PI > PI to 0 > 2PI
                ang += Math.Tau;

            return (Direction) (((int) ((ang + CardinalOffset) * InvCardinalSegment) * 2) & 7);
        }

        /// <summary>
        /// Rounds the angle to the nearest cardinal direction. This behaves similarly to a combination of
        /// <see cref="GetCardinalDir"/> and Direction.ToAngle(), however this may return an angle outside of the range
        /// returned by those methods (-pi to pi).
        /// </summary>
        [Pure]
        public Angle RoundToCardinalAngle()
        {
            return new Angle(CardinalSegment * Math.Floor((Theta + CardinalOffset) / CardinalSegment));
        }

        /// <summary>
        ///     Rotates the vector counter-clockwise around its origin by the value of Theta.
        /// </summary>
        /// <param name="vec">Vector to rotate.</param>
        /// <returns>New rotated vector.</returns>
        [Pure]
        public Vector2 RotateVec(in Vector2 vec)
        {
            // No calculation necessery when theta is zero
            if (Theta == 0) return vec;

            var cos = Math.Cos(Theta);
            var sin = Math.Sin(Theta);
            var dx = cos * vec.X - sin * vec.Y;
            var dy = sin * vec.X + cos * vec.Y;

            return new Vector2((float) dx, (float) dy);
        }

        [Pure]
        public bool EqualsApprox(Angle other, double tolerance)
        {
            return EqualsApprox(this, other, tolerance);
        }

        [Pure]
        public bool EqualsApprox(Angle other)
        {
            return EqualsApprox(this, other);
        }

        [Pure]
        private static bool EqualsApprox(Angle a, Angle b)
        {
            // reduce both angles
            var aReduced = Reduce(a.Theta);
            var bReduced = Reduce(b.Theta);

            var aPositive = FlipPositive(aReduced);
            var bPositive = FlipPositive(bReduced);

            // The second two expressions cover an edge case where one number is barely non-negative while the other number is negative.
            // In this case, the negative number will get FlipPositived to ~2pi and the comparison will give a false negative.
            return MathHelper.CloseToPercent(aPositive, bPositive)
                || MathHelper.CloseToPercent(aPositive + Math.Tau, bPositive)
                || MathHelper.CloseToPercent(aPositive, bPositive + Math.Tau);
        }

        [Pure]
        private static bool EqualsApprox(Angle a, Angle b, double tolerance)
        {
            // reduce both angles
            var aReduced = Reduce(a.Theta);
            var bReduced = Reduce(b.Theta);

            var aPositive = FlipPositive(aReduced);
            var bPositive = FlipPositive(bReduced);

            // The second two expressions cover an edge case where one number is barely non-negative while the other number is negative.
            // In this case, the negative number will get FlipPositived to ~2pi and the comparison will give a false negative.
            return MathHelper.CloseToPercent(aPositive, bPositive, tolerance)
                || MathHelper.CloseToPercent(aPositive + Math.Tau, bPositive, tolerance)
                || MathHelper.CloseToPercent(aPositive, bPositive + Math.Tau, tolerance);
        }

        /// <summary>
        ///     Removes revolutions from a positive or negative angle to make it as small as possible.
        /// </summary>
        [Pure]
        public Angle Reduced()
        {
            return new(Reduce(Theta));
        }

        /// <summary>
        ///     Removes revolutions from a positive or negative angle to make it as small as possible.
        /// </summary>
        [Pure]
        private static double Reduce(double theta)
        {
            // int truncates value (round to 0)
            var aTurns = (int) (theta / Math.Tau);
            return theta - aTurns * Math.Tau;
        }

        /// <inheritdoc />
        [Pure]
        public bool Equals(Angle other)
        {
            return Theta.Equals(other.Theta);
        }

        /// <inheritdoc />
        [Pure]
        public override bool Equals(object? obj)
        {
            return obj is Angle angle && Equals(angle);
        }

        /// <inheritdoc />
        [Pure]
        public override int GetHashCode()
        {
            return Theta.GetHashCode();
        }

        [Pure]
        public static bool operator ==(Angle a, Angle b)
        {
            return a.Equals(b);
        }

        [Pure]
        public static bool operator !=(Angle a, Angle b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Gets the angle flipped around 180 degrees.
        /// </summary>
        [Pure]
        public Angle Opposite()
        {
            return new Angle(FlipPositive(Theta - Math.PI));
        }

        [Pure]
        public Angle FlipPositive()
        {
            return new(FlipPositive(Theta));
        }

        /// <summary>
        ///     Calculates the congruent positive angle of a negative angle. Does nothing to a positive angle.
        /// </summary>
        [Pure]
        private static double FlipPositive(double theta)
        {
            if (theta >= 0)
                return theta;

            return theta + Math.Tau;
        }

        [Pure]
        private static Direction GetDir(double theta)
        {
            var ang = theta % Math.Tau;
            if (ang < 0) // convert -PI > PI to 0 > 2PI
                ang += Math.Tau;

            return (Direction)((int) ((ang + Offset) * InvSegment) & 7);
        }

        /// <summary>
        ///     Similar to Lerp but, but defaults to making sure that lerping from 1 to 359 degrees doesn't wrap around
        ///     the whole circle.
        /// </summary>
        [Pure]
        public static Angle Lerp(in Angle a, in Angle b, float factor)
        {
            return a + ShortestDistance(a, b) * factor;
        }

        /// <summary>
        ///     Returns the shortest distance between two angles.
        /// </summary>
        [Pure]
        public static Angle ShortestDistance(in Angle a, in Angle b)
        {
            var delta = (b - a) % Math.Tau;
            return 2 * delta % Math.Tau - delta;
        }

        /// <summary>
        ///     Constructs a new angle, from degrees instead of radians.
        /// </summary>
        /// <param name="degrees">The angle in degrees.</param>
        [Pure]
        public static Angle FromDegrees(double degrees)
        {
            // Avoid rounding issues with common use cases.
            switch (degrees)
            {
                case -270:
                    return new Angle(Math.PI * -1.5);
                case 90:
                    return new Angle(Math.PI / 2);
                case -180:
                    return new Angle(-Math.PI);
                case 180:
                    return new Angle(Math.PI);
                case 270.0:
                    return new Angle(Math.PI * 1.5);
                case -90:
                    return new Angle(Math.PI / -2);
                default:
                    return new(MathHelper.DegreesToRadians(degrees));
            }
        }

        /// <summary>
        ///     Implicit conversion from Angle to double.
        /// </summary>
        /// <param name="angle"></param>
        [Pure]
        public static implicit operator double(Angle angle)
        {
            return angle.Theta;
        }

        /// <summary>
        ///     Implicit conversion from double to Angle.
        /// </summary>
        /// <param name="theta"></param>
        [Pure]
        public static implicit operator Angle(double theta)
        {
            return new(theta);
        }

        /// <summary>
        ///     Implicit conversion from float to Angle.
        /// </summary>
        /// <param name="theta"></param>
        [Pure]
        public static implicit operator Angle(float theta)
        {
            return new(theta);
        }

        [Pure]
        public static Angle operator +(Angle a, Angle b)
            => new(a.Theta + b.Theta);

        [Pure]
        public static Angle operator -(Angle a, Angle b)
            => new(a.Theta - b.Theta);

        [Pure]
        public static Angle operator -(Angle orig)
            => new(-orig.Theta);

        [Pure]
        public override string ToString()
        {
            return $"{Theta} rad";
        }

        [Pure]
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return ToString();
        }

        public bool TryFormat(
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format,
            IFormatProvider? provider)
        {
            return FormatHelpers.TryFormatInto(
                destination,
                out charsWritten,
                $"{Theta} rad");
        }
    }
}
