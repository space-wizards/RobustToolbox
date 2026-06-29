using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     Uniquely identifies a map.
    /// </summary>
    /// <remarks>
    ///     All maps, aside from <see cref="Nullspace"/>, are also entities. When writing generic code it's usually
    ///     preferable to use <see cref="EntityUid"/> or <see cref="Entity{T}"/> instead.
    /// </remarks>
    /// <seealso cref="IMapManager"/>
    /// <seealso cref="SharedMapSystem"/>
    [Serializable, NetSerializable]
    public readonly struct MapId : IEquatable<MapId>
    {
        /// <summary>
        ///     The equivalent of <c>null</c> for maps. There is no map entity assigned to this and anything here is
        ///     a root (has no parent) for the <see cref="TransformComponent">transform hierarchy</see>.<br/>
        ///     All map entities live in nullspace and function as roots, for example.
        /// </summary>
        public static readonly MapId Nullspace = new(0);

        internal readonly int Value;

        public MapId(int value)
        {
            Value = value;
        }

        /// <inheritdoc />
        public bool Equals(MapId other)
        {
            return Value == other.Value;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is MapId id && Equals(id);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(MapId a, MapId b)
        {
            return a.Value == b.Value;
        }

        public static bool operator !=(MapId a, MapId b)
        {
            return !(a == b);
        }

        public static explicit operator int(MapId self)
        {
            return self.Value;
        }

        public override string ToString()
        {
            return IsClientSide ? $"c{-Value}" : Value.ToString();
        }

        public bool IsClientSide => Value < 0;
    }
}
