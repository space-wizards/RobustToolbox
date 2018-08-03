using System;
using SS14.Shared.Serialization;

namespace SS14.Shared.Map
{
    /// <summary>
    /// Internal structure to store 2 indices of a chunk or tile.
    /// </summary>
    [Serializable, NetSerializable]
    public struct MapIndices
    {
        /// <summary>
        /// The X index.
        /// </summary>
        public readonly int X;

        /// <summary>
        /// The Y index.
        /// </summary>
        public readonly int Y;

        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="x">The X index.</param>
        /// <param name="y">The Y index.</param>
        public MapIndices(int x, int y)
        {
            X = x;
            Y = y;
        }

        //TODO: Fill out the rest of these.
        public static MapIndices operator +(MapIndices left, MapIndices right)
        {
            return new MapIndices(left.X + right.X, left.Y + right.Y);
        }

        public static MapIndices operator *(MapIndices indices, int multiplier)
        {
            return new MapIndices(indices.X * multiplier, indices.Y * multiplier);
        }

        public override string ToString()
        {
            return $"{{{X},{Y}}}";
        }
    }
}
