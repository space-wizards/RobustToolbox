using System;
using System.Numerics;
using System.Runtime.CompilerServices;
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
            dir = dir.Normalized();
            Theta = Math.Atan2(dir.Y, dir.X);
        }

        public static Angle FromWorldVec(Vector2 dir)
        {
            return new Angle(dir) + Math.PI / 2;
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
        public readonly Vector2 ToVec()
        {
            var x = Math.Cos(Theta);
            var y = Math.Sin(Theta);
            return new Vector2((float) x, (float) y);
        }

        public readonly Vector2 ToWorldVec()
        {
            return (this - MathHelper.PiOver2).ToVec();
        }

        private const double Segment = 2 * Math.PI / 8.0; // Cut the circle into 8 pieces
        private const double Offset = Segment / 2.0; // offset the pieces by 1/2 their size

        public readonly Direction GetDir()
        {
            var ang = Theta % (2 * Math.PI);

            if (ang < 0) // convert -PI > PI to 0 > 2PI
                ang += 2 * Math.PI;

            return (Direction) (Math.Floor((ang + Offset) / Segment) % 8);
        }

        public Direction RotateDir(Direction dir)
        {
            var ang = (Theta + Segment * (int)dir) % (2 * Math.PI);
            if (ang < 0)
                ang += 2 * Math.PI;

            return (Direction)(Math.Floor((ang + Offset) / Segment) % 8);
        }

        private const double CardinalSegment = 2 * Math.PI / 4.0; // Cut the circle into 4 pieces
        private const double CardinalOffset = CardinalSegment / 2.0; // offset the pieces by 1/2 their size

        public readonly Direction GetCardinalDir()
        {
            var ang = Theta % (2 * Math.PI);

            if (ang < 0.0f) // convert -PI > PI to 0 > 2PI
                ang += 2 * Math.PI;

            return (Direction) (Math.Floor((ang + CardinalOffset) / CardinalSegment) * 2 % 8);
        }

        /// <summary>
        ///     Rotates the vector counter-clockwise around its origin by the value of Theta.
        /// </summary>
        /// <param name="vec">Vector to rotate.</param>
        /// <returns>New rotated vector.</returns>
        [Pure]
        public readonly Vector2 RotateVec(in Vector2 vec)
        {
            // No calculation necessery when theta is zero
            if (Theta == 0) return vec;

            var cos = Math.Cos(Theta);
            var sin = Math.Sin(Theta);
            var dx = cos * vec.X - sin * vec.Y;
            var dy = sin * vec.X + cos * vec.Y;

            return new Vector2((float) dx, (float) dy);
        }

        public bool EqualsApprox(Angle other, double tolerance)
        {
            return EqualsApprox(this, other, tolerance);
        }

        public bool EqualsApprox(Angle other)
        {
            return EqualsApprox(this, other);
        }

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
                || MathHelper.CloseToPercent(aPositive + MathHelper.TwoPi, bPositive)
                || MathHelper.CloseToPercent(aPositive, bPositive + MathHelper.TwoPi);
        }

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
                || MathHelper.CloseToPercent(aPositive + MathHelper.TwoPi, bPositive, tolerance)
                || MathHelper.CloseToPercent(aPositive, bPositive + MathHelper.TwoPi, tolerance);
        }

        /// <summary>
        ///     Removes revolutions from a positive or negative angle to make it as small as possible.
        /// </summary>
        public readonly Angle Reduced()
        {
            return new(Reduce(Theta));
        }

        /// <summary>
        ///     Removes revolutions from a positive or negative angle to make it as small as possible.
        /// </summary>
        private static double Reduce(double theta)
        {
            // int truncates value (round to 0)
            var aTurns = (int) (theta / (2 * Math.PI));
            return theta - aTurns * (2 * Math.PI);
        }

        /// <inheritdoc />
        public readonly bool Equals(Angle other)
        {
            return Theta.Equals(other.Theta);
        }

        /// <inheritdoc />
        public readonly override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Angle angle && Equals(angle);
        }

        /// <inheritdoc />
        public readonly override int GetHashCode()
        {

            return Theta.GetHashCode();

        }

        public static bool operator ==(Angle a, Angle b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Angle a, Angle b)
        {
            return !(a == b);
        }

        public readonly Angle Opposite()
        {
            return new Angle(FlipPositive(Theta-Math.PI));
        }

        public readonly Angle FlipPositive()
        {
            return new(FlipPositive(Theta));
        }

        /// <summary>
        ///     Calculates the congruent positive angle of a negative angle. Does nothing to a positive angle.
        /// </summary>
        private static double FlipPositive(double theta)
        {
            if (theta >= 0)
                return theta;

            return theta + 2 * Math.PI;
        }

        /// <summary>
        ///     Similar to Lerp but, but defaults to making sure that lerping from 1 to 359 degrees doesn't wrap around
        ///     the whole circle.
        /// </summary>
        public static Angle Lerp(in Angle a, in Angle b, float factor)
        {
            return a + ShortestDistance(a, b) * factor;
        }

        /// <summary>
        ///     Returns the shortest distance between two angles.
        /// </summary>
        public static Angle ShortestDistance(in Angle a, in Angle b)
        {
            var delta = (b - a) % Math.Tau;
            return 2 * delta % Math.Tau - delta;
        }

        /// <summary>
        ///     Constructs a new angle, from degrees instead of radians.
        /// </summary>
        /// <param name="degrees">The angle in degrees.</param>
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
        public static implicit operator double(Angle angle)
        {
            return angle.Theta;
        }

        /// <summary>
        ///     Implicit conversion from double to Angle.
        /// </summary>
        /// <param name="theta"></param>
        public static implicit operator Angle(double theta)
        {
            return new(theta);
        }

        /// <summary>
        ///     Implicit conversion from float to Angle.
        /// </summary>
        /// <param name="theta"></param>
        public static implicit operator Angle(float theta)
        {
            return new(theta);
        }

        public static Angle operator +(Angle a, Angle b)
            => new(a.Theta + b.Theta);

        public static Angle operator -(Angle a, Angle b)
            => new(a.Theta - b.Theta);

        public static Angle operator -(Angle orig)
            => new(-orig.Theta);

        public override string ToString()
        {
            return $"{Theta} rad";
        }

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
