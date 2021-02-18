using System;
using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     Internal structure to store 2 indices of a chunk or tile.
    ///     <remarks>Despite the name, this can refer to a specific tile on a grid.</remarks>
    /// </summary>
    [PublicAPI]
    [Serializable, NetSerializable]
    [Obsolete("Use Vector2i instead")]
    public readonly struct MapIndices : IEquatable<MapIndices>
    {
        /// <summary>
        ///     The <see cref="X" /> index.
        /// </summary>
        public readonly int X;

        /// <summary>
        ///     The <see cref="Y" /> index.
        /// </summary>
        public readonly int Y;

        /// <summary>
        ///     Public constructor.
        /// </summary>
        /// <param name="x">The <see cref="X" /> index.</param>
        /// <param name="y">The <see cref="Y" /> index.</param>
        public MapIndices(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        ///     Translates the indices by a given offset.
        /// </summary>
        public static MapIndices operator +(MapIndices left, MapIndices right)
        {
            return new(left.X + right.X, left.Y + right.Y);
        }

        /// <summary>
        ///     Translates the indices by a given offset.
        /// </summary>
        public static MapIndices operator -(MapIndices left, MapIndices right)
        {
            return new(left.X - right.X, left.Y - right.Y);
        }

        /// <summary>
        ///     Returns the opposite indices.
        /// </summary>
        public static MapIndices operator -(MapIndices indices)
        {
            return new(-indices.X, -indices.Y);
        }

        /// <summary>
        ///     Scales the <paramref name="indices" /> by a scalar amount.
        /// </summary>
        public static MapIndices operator *(MapIndices indices, int multiplier)
        {
            return new(indices.X * multiplier, indices.Y * multiplier);
        }

        /// <summary>
        ///     Tests for value equality between two LocalCoordinates.
        /// </summary>
        public static bool operator ==(MapIndices a, MapIndices b)
        {
            return a.Equals(b);
        }

        /// <summary>
        ///     Tests for value inequality between two LocalCoordinates.
        /// </summary>
        public static bool operator !=(MapIndices a, MapIndices b)
        {
            return !a.Equals(b);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is MapIndices idx && Equals(idx);
        }

        /// <inheritdoc />
        public bool Equals(MapIndices other)
        {
            return other.X == X && other.Y == Y;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{{{X},{Y}}}";
        }

        public EntityCoordinates ToEntityCoordinates(IMapManager mapManager, GridId gridId)
        {
            var grid = mapManager.GetGrid(gridId);
            var tile = grid.TileSize;

            return new EntityCoordinates(grid.GridEntityId, (X * tile, Y * tile));
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return X ^ (Y * 23011);
        }

        public static implicit operator Vector2i(in MapIndices indices)
        {
            return new(indices.X, indices.Y);
        }

        public static implicit operator MapIndices(in Vector2i indices)
        {
            return new(indices.X, indices.Y);
        }

        public static implicit operator Vector2(in MapIndices indices)
        {
            return new(indices.X, indices.Y);
        }
    }
}
