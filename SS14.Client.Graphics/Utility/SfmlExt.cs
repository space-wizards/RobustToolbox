using SFML.Graphics;
using SFML.System;
using System;

namespace SS14.Client.Graphics.Utility
{
    internal static class SfmlExt
    {
        // Vector2i
        public static int LengthSquared(this SFML.System.Vector2i vec) => vec.X * vec.X + vec.Y * vec.Y;
        public static float Length(this SFML.System.Vector2i vec) => (float)Math.Sqrt(LengthSquared(vec));
        public static Vector2f ToFloat(this SFML.System.Vector2i vec) => new Vector2f(vec.X, vec.Y);

        // Vector2
        public static float LengthSquared(this Vector2f vec) => vec.X * vec.X + vec.Y * vec.Y;
        public static float Length(this Vector2f vec) => (float)Math.Sqrt(LengthSquared(vec));
        public static SFML.System.Vector2i Round(this Vector2f vec) => new SFML.System.Vector2i((int)Math.Round(vec.X), (int)Math.Round(vec.Y));

        // IntRect
        public static int Bottom(this IntRect rect) => rect.Top + rect.Height;
        public static int Right(this IntRect rect) => rect.Left + rect.Width;
        public static bool IsEmpty(this IntRect rect) => rect.Left == 0 && rect.Top == 0 && rect.Width == 0 && rect.Height == 0;
        public static FloatRect ToFloat(this IntRect rect) => new FloatRect(rect.Left, rect.Top, rect.Width, rect.Height);

        // FloatRect
        public static float Bottom(this FloatRect rect) => rect.Top + rect.Height;
        public static float Right(this FloatRect rect) => rect.Left + rect.Width;
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
        /// Returns the dot product of two vectors
        /// </summary>
        /// <returns>The dot product of the two vectors.</returns>
        public static float DotProduct(this Vector2f v1, Vector2f v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y;
        }

        /// <summary>
        /// Return the magnitude (aka. length or absolute value) of a vector.
        /// </summary>
        public static float Magnitude(this Vector2f vector)
        {
            return (float)Math.Sqrt(vector.X * vector.X + vector.Y + vector.Y);
        }

        /// <summary>
        /// Get the normalized vector (scale it so its magnitude is 1).
        /// </summary>
        public static Vector2f Normalize(this Vector2f vector)
        {
            try
            {
                var inverse = 1 / vector.Magnitude();
                return new Vector2f(vector.X * inverse, vector.Y * inverse);
            }
            catch (DivideByZeroException e)
            {
                throw new DivideByZeroException("Attempted to normalize a zero vector.", e);
            }
        }
    }
}
