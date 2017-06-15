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
    }
}
