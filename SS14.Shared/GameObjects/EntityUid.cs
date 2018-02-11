using System;

namespace SS14.Shared.GameObjects
{
    /// <summary>
    ///     This type contains a network identification number of an entity.
    ///     This can be used by the EntityManager to reference an IEntity.
    /// </summary>
    [Serializable]
    public struct EntityUid : IEquatable<EntityUid>
    {
        private readonly int _uid;

        public static readonly EntityUid Invalid = new EntityUid(0);

        /// <summary>
        ///     Creates an instance of this structure, with the given network ID.
        /// </summary>
        public EntityUid(int uid)
        {
            _uid = uid;
        }

        /// <summary>
        ///     Checks if the ID value is valid. Does not check if it identifies
        ///     a valid Entity.
        /// </summary>
        public bool IsValid()
        {
            return _uid > 0;
        }

        /// <inheritdoc />
        public bool Equals(EntityUid other)
        {
            return _uid == other._uid;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is EntityUid id && Equals(id);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _uid;
        }

        /// <summary>
        ///     Check for equality by value between two objects.
        /// </summary>
        public static bool operator ==(EntityUid a, EntityUid b)
        {
            return a._uid == b._uid;
        }

        /// <summary>
        ///     Check for inequality by value between two objects.
        /// </summary>
        public static bool operator !=(EntityUid a, EntityUid b)
        {
            return !(a == b);
        }

        /// <summary>
        ///     Explicit conversion of EntityId to int. This should only be used in special
        ///     cases like serialization. Do NOT use the in content.
        /// </summary>
        public static explicit operator int(EntityUid self)
        {
            return self._uid;
        }
        
        /// <inheritdoc />
        public override string ToString()
        {
            return _uid.ToString();
        }
    }
}
