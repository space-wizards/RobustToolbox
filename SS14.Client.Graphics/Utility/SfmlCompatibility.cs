using SFML.Graphics;
using SFML.System;
using SS14.Shared.Maths;
using Vector2i = SS14.Shared.Maths.Vector2i;
using Vector2u = SS14.Shared.Maths.Vector2u;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.Graphics.Utility
{
    /// <summary>
    /// Provides compatibility extensions to convert between SFML and OpenTK types.
    /// </summary>
    internal static class SfmlCompatibility
    {
        /// <summary>
        /// Converts a OpenTK Vector2 to a SFML Vector2.
        /// </summary>
        /// <param name="vec">OpenTK Vector2.</param>
        /// <returns>SFML Vector2.</returns>
        public static Vector2f Convert(this Vector2 vec)
        {
            return new Vector2f(vec.X, vec.Y);
        }

        /// <summary>
        /// Converts a SFML Vector2 to a OpenTK Vector2.
        /// </summary>
        /// <param name="vec">SFML Vector2.</param>
        /// <returns>OpenTK Vector2.</returns>
        public static Vector2 Convert(this Vector2f vec)
        {
            return new Vector2(vec.X, vec.Y);
        }

        /// <summary>
        /// Converts a OpenTK Vector3 to a SFML Vector3.
        /// </summary>
        /// <param name="vec">OpenTK Vector3.</param>
        /// <returns>SFML Vector3.</returns>
        public static Vector3f Convert(this OpenTK.Vector3 vec)
        {
            return new Vector3f(vec.X, vec.Y, vec.Z);
        }

        /// <summary>
        /// Converts a SFML Vector3 to a OpenTK Vector3.
        /// </summary>
        /// <param name="vec">SFML Vector3.</param>
        /// <returns>OpenTK Vector3.</returns>
        public static OpenTK.Vector3 Convert(this Vector3f vec)
        {
            return new OpenTK.Vector3(vec.X, vec.Y, vec.Z);
        }

        /// <summary>
        /// Converts a OpenTK Box2 to a SFML FloatRect.
        /// </summary>
        /// <param name="box">OpenTK Box2.</param>
        /// <returns>SFML FloatRect.</returns>
        public static FloatRect Convert(this OpenTK.Box2 box)
        {
            return new FloatRect(box.Left, box.Top, box.Width, box.Height);
        }

        /// <summary>
        /// Converts a SS14 Box2 to a SFML FloatRect.
        /// </summary>
        /// <param name="box">OpenTK Box2.</param>
        /// <returns>SFML FloatRect.</returns>
        public static FloatRect Rect(this Box2 box)
        {
            return new FloatRect(box.Left, box.Top, box.Width, box.Height);
        }

        /// <summary>
        /// Converts a SFML FloatRect to a OpenTK Box2.
        /// </summary>
        /// <param name="rect">SFML FloatRect.</param>
        /// <returns>OpenTK Box2.</returns>
        public static OpenTK.Box2 Convert(this FloatRect rect)
        {
            return new OpenTK.Box2(rect.Left, rect.Top, rect.Right(), rect.Bottom());
        }

        /// <summary>
        ///     Converts a SFML FloatRect to a SS14 Box2.
        /// </summary>
        public static Shared.Maths.Box2 ToBox(this FloatRect rect)
        {
            return new Shared.Maths.Box2(rect.Left, rect.Top, rect.Right(), rect.Bottom());
        }

        public static OpenTK.Box2 ToBox(this Shared.Maths.Box2 rect)
        {
            return new OpenTK.Box2(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        public static FloatRect Convert(this Shared.Maths.Box2 box)
        {
            return new FloatRect(box.Left, box.Top, box.Width, box.Height);
        }

        public static IntRect Convert(this Box2i box)
        {
            return new IntRect(box.Left, box.Top, box.Width, box.Height);
        }

        public static Box2i Convert(this IntRect rect)
        {
            return new Box2i(rect.Left, rect.Top, rect.Right(), rect.Bottom());
        }

        public static Vector2i Convert(this SFML.System.Vector2i vector)
        {
            return new Vector2i(vector.X, vector.Y);
        }

        public static SFML.System.Vector2i Convert(this Vector2i vector)
        {
            return new SFML.System.Vector2i(vector.X, vector.Y);
        }

        public static Vector2u Convert(this SFML.System.Vector2u vector)
        {
            return new Vector2u(vector.X, vector.Y);
        }

        public static Vector2 Convertf(this SFML.System.Vector2u vector)
        {
            return new Vector2(vector.X, vector.Y);
        }

        public static SFML.Graphics.Color Convert(this Shared.Maths.Color color)
        {
            return new SFML.Graphics.Color(color.RByte, color.GByte, color.BByte, color.AByte);
        }

        public static Shared.Maths.Color Convert(this SFML.Graphics.Color color)
        {
            return new Shared.Maths.Color(color.R, color.G, color.B, color.A);
        }
    }
}
