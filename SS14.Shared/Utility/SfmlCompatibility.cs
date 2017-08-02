using OpenTK;
using SFML.System;

namespace SS14.Shared.Utility
{
    /// <summary>
    /// Provides compatibility extensions to convert between SFML and OpenTK types.
    /// </summary>
    public static class SfmlCompatibility
    {
        /// <summary>
        /// Converts a OpenTK Vector2 to a SFML Vector2f.
        /// </summary>
        /// <param name="vec">OpenTK Vector2.</param>
        /// <returns>SFML Vector2.</returns>
        public static Vector2f Convert(this Vector2 vec)
        {
            return new Vector2f(vec.X, vec.Y);
        }

        /// <summary>
        /// Converts a SFML Vector2f to a OpenTK Vector2.
        /// </summary>
        /// <param name="vec">SFML Vector2f.</param>
        /// <returns>OpenTK Vector2.</returns>
        public static Vector2 Convert(this Vector2f vec)
        {
            return new Vector2(vec.X, vec.Y);
        }
    }
}