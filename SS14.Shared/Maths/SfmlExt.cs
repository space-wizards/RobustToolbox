using SFML.Graphics;
using SFML.System;
using System;

namespace SS14.Shared.Maths
{
    public static class SfmlExt
    {
        // Vector2i
        public static int LengthSquared(this Vector2i vec) => vec.X * vec.X + vec.Y * vec.Y;
        public static float Length(this Vector2i vec)      => (float)Math.Sqrt(LengthSquared(vec));
        public static Vector2f ToFloat(this Vector2i vec)  => new Vector2f(vec.X, vec.Y);


        // Vector2f
        public static float LengthSquared(this Vector2f vec) => vec.X * vec.X + vec.Y * vec.Y;
        public static float Length(this Vector2f vec)        => (float)Math.Sqrt(LengthSquared(vec));
        public static Vector2i Round(this Vector2f vec)      => new Vector2i((int)Math.Round(vec.X), (int)Math.Round(vec.Y));


        // IntRect
        public static int Bottom(this IntRect rect) => rect.Top + rect.Height;
        public static int Right(this IntRect rect)  => rect.Left + rect.Width;
        public static bool IsEmpty(this IntRect rect) => rect.Left == 0 && rect.Top == 0 && rect.Width == 0 && rect.Height == 0;
        public static FloatRect ToFloat(this IntRect rect) => new FloatRect(rect.Left, rect.Top, rect.Width, rect.Height);


        // FloatRect
        public static float Bottom(this FloatRect rect) => rect.Top + rect.Height;
        public static float Right(this FloatRect rect)  => rect.Left + rect.Width;
        public static bool IsEmpty(this FloatRect rect) => rect.Left == 0 && rect.Top == 0 && rect.Width == 0 && rect.Height == 0;
        public static IntRect Round(this FloatRect rect)
        {
            var top = (int)Math.Round(rect.Top);
            var left = (int)Math.Round(rect.Left);
            var right = (int)Math.Round(rect.Right());
            var bottom = (int)Math.Round(rect.Bottom());
            return new IntRect(left, top, right - left, bottom - top);
        }
        public static bool Encloses(this FloatRect outer, FloatRect inner)
            => (outer.Left <= inner.Left)
            && (inner.Right() <= outer.Right())
            && (outer.Top <= inner.Top)
            && (inner.Bottom() <= outer.Bottom());


        // Color
        public static uint ToInt(this Color color)
            => unchecked((uint)(
                (color.R << 16)
                | (color.G << 8)
                | (color.B << 0)
                | (color.A << 24)));

        public static Color IntToColor(uint color)
            => unchecked(new Color(
                (byte)(color >> 16),
                (byte)(color >> 8),
                (byte)(color >> 0),
                (byte)(color >> 24)));

        /// <summary>
        /// Values used internally by <see cref="DirectionTo" />
        /// </summary>
        private static Direction[] AngleDirections = new Direction[]
        {
            Direction.East,
            Direction.NorthEast,
            Direction.North,
            Direction.NorthWest,
            Direction.West,
            Direction.SouthWest,
            Direction.South,
            Direction.SouthEast,
            Direction.East
        };

        /// <summary>
        /// Find the direction that <paramref name="target" /> is from <paramref name="origin" />.
        /// </summary>
        /// <param name="origin">The origin vector.</param>
        /// <param name="target">The target vector.</param>
        /// <param name="fallback">The direction used if no direction could be calculated (difference between vectors too small).</param>
        public static Direction DirectionTo(this Vector2f origin, Vector2f target, Direction fallback=Direction.South)
        {
            var mag1 = origin.Magnitude();
            var mag2 = target.Magnitude();
            // Check whether the vectors are almost zero.
            // Don't use == because equality checking on floats is dangerous and unreliable.
            // If the range is too wide go ahead and make it smaller.
            if (mag1 < 0.0001 && mag1 > -0.0001 && mag2 < 0.0001 && mag2 > -0.0001)
            {
                return fallback;
            }

            // Angle in degrees.
            // Keep in mind: Cartesian plane so 0° is to the right.
            var angle = FloatMath.ToDegrees((float)Math.Atan2(target.Y - origin.Y, target.X - origin.X));

            // The directions are assumed to be perfect 45° surfaces.
            // So 0° is between the east one, and 22.5° is the edge between east and north east.

            // Wrap negative angles around so we're always dealing with positives.
            if (angle < 0)
            {
                angle += 360;
            }

            // Add 22.5° to offset the angles since 0° is inside one.
            angle += 22.5f;

            int dirindex = FloatMath.Clamp((int)Math.Floor(angle / 45f), 0, 8);
            return AngleDirections[dirindex];
        }

        /// <summary>
        /// Returns the dot product of two vectors
        /// </summary>
        /// <returns>The dot product of the two vectors.</returns>
        public static float DotProduct(this Vector2f v1, Vector2f v2)
        {
            return v1.X*v2.X + v1.Y*v2.Y;
        }

        /// <summary>
        /// Return the magnitude (aka. length or absolute value) of a vector.
        /// </summary>
        public static float Magnitude(this Vector2f vector)
        {
            return (float)Math.Sqrt(vector.X*vector.X + vector.Y+vector.Y);
        }

        /// <summary>
        /// Get the normalized vector (scale it so its magnitude is 1).
        /// </summary>
        public static Vector2f Normalize(this Vector2f vector)
        {
            try
            {
                var inverse = 1 / vector.Magnitude();
                return new Vector2f(vector.X*inverse, vector.Y*inverse);
            }
            catch (DivideByZeroException e)
            {
                throw new DivideByZeroException("Attempted to normalize a zero vector.", e);
            }
        }
    }
}
