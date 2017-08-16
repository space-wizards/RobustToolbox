using OpenTK;
using SFML.Graphics;
using SFML.System;
using SS14.Shared.Maths;

namespace SS14.Shared.Utility
{
    /// <summary>
    /// Provides compatibility extensions to convert between SFML and OpenTK types.
    /// </summary>
    public static class SfmlCompatibility
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
        /// Converts a OpenTK Box2 to a SFML FloatRect.
        /// </summary>
        /// <param name="box">OpenTK Box2.</param>
        /// <returns>SFML FloatRect.</returns>
        public static FloatRect Convert(this Box2 box)
        {
            return new FloatRect(box.Left, box.Top, box.Width, box.Height);
        }

        /// <summary>
        /// Converts a SFML FloatRect to a OpenTK Box2.
        /// </summary>
        /// <param name="rect">SFML FloatRect.</param>
        /// <returns>OpenTK Box2.</returns>
        public static Box2 Convert(this FloatRect rect)
        {
            return new Box2(rect.Left, rect.Top, rect.Right(), rect.Bottom());
        }
    }
}
