using System;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     This type contains a network identification number of an entity.
    ///     This can be used by the EntityManager to access an entity
    /// </summary>
    [Serializable, NetSerializable]
    public readonly struct EntityUid : IEquatable<EntityUid>, IComparable<EntityUid>
    {
        /// <summary>
        ///     If this bit is set on a UID, it's client sided.
        ///     Use <see cref="IsClientSide" /> to check this.
        /// </summary>
        internal const int ClientUid = 2 << 29;
        readonly int _uid;

        /// <summary>
        ///     An Invalid entity UID you can compare against.
        /// </summary>
        public static readonly EntityUid Invalid = new(0);

        /// <summary>
        ///     The first entity UID the entityManager should use when the manager is initialized.
        /// </summary>
        public static readonly EntityUid FirstUid = new(1);

        /// <summary>
        ///     Creates an instance of this structure, with the given network ID.
        /// </summary>
        public EntityUid(int uid)
        {
            _uid = uid;
        }

        public bool Valid => IsValid();

        /// <summary>
        ///     Creates an entity UID by parsing a string number.
        /// </summary>
        public static EntityUid Parse(ReadOnlySpan<char> uid)
        {
            if (uid.StartsWith("c"))
            {
                return new EntityUid(int.Parse(uid[1..]) | ClientUid);
            }
            else
            {
                return new EntityUid(int.Parse(uid));
            }
        }

        public static bool TryParse(ReadOnlySpan<char> uid, out EntityUid entityUid)
        {
            try
            {
                entityUid = Parse(uid);
                return true;
            }
            catch (FormatException)
            {
                entityUid = Invalid;
                return false;
            }
        }

        /// <summary>
        ///     Checks if the ID value is valid. Does not check if it identifies
        ///     a valid Entity.
        /// </summary>
        [Pure]
        public bool IsValid()
        {
            return _uid > 0;
        }

        [Pure]
        public bool IsClientSide()
        {
            return (_uid & (2 << 29)) != 0;
        }

        /// <inheritdoc />
        public bool Equals(EntityUid other)
        {
            return _uid == other._uid;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
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
        ///     cases like serialization. Do NOT use this in content.
        /// </summary>
        public static explicit operator int(EntityUid self)
        {
            return self._uid;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (IsClientSide())
            {
                return $"c{_uid & ~ClientUid}";
            }
            return _uid.ToString();
        }

        /// <inheritdoc />
        public int CompareTo(EntityUid other)
        {
            return _uid.CompareTo(other._uid);
        }

        #region ViewVariables


        [ViewVariables]
        private string Representation => IoCManager.Resolve<IEntityManager>().ToPrettyString(this);

        [ViewVariables(VVAccess.ReadWrite)]
        private string Name
        {
            get => MetaData?.EntityName ?? string.Empty;
            set
            {
                if (MetaData is {} metaData)
                    metaData.EntityName = value;
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        private string Description
        {
            get => MetaData?.EntityDescription ?? string.Empty;
            set
            {
                if (MetaData is {} metaData)
                    metaData.EntityDescription = value;
            }
        }

        [ViewVariables]
        private EntityPrototype? Prototype => MetaData?.EntityPrototype;

        [ViewVariables]
        private GameTick LastModifiedTick => MetaData?.EntityLastModifiedTick ?? GameTick.Zero;

        [ViewVariables]
        private bool Paused => MetaData?.EntityPaused ?? false;

        [ViewVariables]
        private EntityLifeStage LifeStage => MetaData?.EntityLifeStage ?? EntityLifeStage.Deleted;

        [ViewVariables]
        private MetaDataComponent? MetaData =>
            IoCManager.Resolve<IEntityManager>().GetComponentOrNull<MetaDataComponent>(this);

        [ViewVariables]
        private TransformComponent? Transform =>
            IoCManager.Resolve<IEntityManager>().GetComponentOrNull<TransformComponent>(this);

        // This might seem useless, but it allows you to retrieve remote entities that don't exist on the client.
        [ViewVariables]
        private EntityUid Uid => this;

        #endregion
    }
}
