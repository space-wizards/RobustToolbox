using System;

namespace SS14.Shared.Maths
{
    /// <summary>
    ///     A representation of an angle, in radians.
    /// </summary>
    [Serializable]
    public struct Angle
    {
        public static Angle Zero { get; set; } = new Angle();

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
            dir = dir.Normalized;
            Theta = Math.Atan2(-dir.Y, dir.X);
        }

        /// <summary>
        ///     Converts this angle to a unit direction vector.
        /// </summary>
        /// <returns>Unit Direction Vector</returns>
        public Vector2 ToVec()
        {
            var x = Math.Cos(Theta);
            var y = -Math.Sin(Theta);
            return new Vector2((float)x, (float)y);
        }

        private const double Segment = 2 * Math.PI / 8.0; // Cut the circle into 8 pieces
        private const double Offset = Segment / 2.0; // offset the pieces by 1/2 their size

        public Direction GetDir()
        {
            var ang = Theta % (2 * Math.PI);

            if (ang < 0.0f) // convert -PI > PI to 0 > 2PI
                ang += 2 * (float)Math.PI;

            return (Direction)(Math.Floor((ang + Offset) / Segment) % 8);
        }

        private const double CardinalSegment = 2 * Math.PI / 4.0; // Cut the circle into 4 pieces
        private const double CardinalOffset = CardinalSegment / 2.0; // offset the pieces by 1/2 their size

        public Direction GetCardinalDir()
        {
            var ang = Theta % (2 * Math.PI);

            if (ang < 0.0f) // convert -PI > PI to 0 > 2PI
                ang += 2 * (float)Math.PI;

            return (Direction)((Math.Floor((ang + CardinalOffset) / CardinalSegment) * 2) % 8);
        }

        public bool EqualsApprox(Angle angle)
        {
            return EqualsApprox(this, angle);
        }

        private static bool EqualsApprox(Angle a, Angle b)
        {
            // reduce both angles
            var aReduced = Reduce(a.Theta);
            var bReduced = Reduce(b.Theta);

            var aPositive = FlipPositive(aReduced);
            var bPositive = FlipPositive(bReduced);

            return FloatMath.CloseTo(aPositive, bPositive);
        }

        /// <summary>
        ///     Removes revolutions from a positive or negative angle to make it as small as possible.
        /// </summary>
        private static double Reduce(double theta)
        {
            // int truncates value (round to 0)
            var aTurns = (int)(theta / (2 * Math.PI));
            return theta - aTurns * (2 * Math.PI);
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
        ///     Constructs a new angle, from degrees instead of radians.
        /// </summary>
        /// <param name="degrees">The angle in degrees.</param>
        public static Angle FromDegrees(double degrees)
        {
            return new Angle(MathHelper.DegreesToRadians(degrees));
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
            return new Angle(theta);
        }

        /// <summary>
        ///     Implicit conversion from float to Angle.
        /// </summary>
        /// <param name="theta"></param>
        public static implicit operator Angle(float theta)
        {
            return new Angle(theta);
        }
    }
}
