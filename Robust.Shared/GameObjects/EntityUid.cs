using System;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions.Dangerous;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     This type contains a local identification number of an entity.
    ///     This can be used by the EntityManager to access an entity
    /// </summary>
    [CopyByRef]
    public readonly struct EntityUid : IEquatable<EntityUid>, IComparable<EntityUid>, ISpanFormattable
    {
        internal readonly int _uid;

        internal readonly int Version;

        /// <summary>
        ///     An Invalid entity UID you can compare against.
        /// </summary>
        public static readonly EntityUid Invalid = new(-1 + ArchUidOffset, -1 + ArchVersionOffset);

        /// <summary>
        ///     The first entity UID the entityManager should use when the manager is initialized.
        /// </summary>
        public static readonly EntityUid FirstUid = new(0 + ArchUidOffset, 0 + ArchVersionOffset);

        internal const int ArchUidOffset = 1;
        internal const int ArchVersionOffset = 1;

        public EntityUid()
        {
            _uid = Invalid._uid;
            Version = -Invalid.Version;
        }

        internal EntityUid(EntityReference reference)
        {
            _uid = reference.Entity.Id + ArchUidOffset;
            Version = reference.Version + ArchVersionOffset;
        }

        /// <summary>
        ///     Creates an instance of this structure, with the given network ID.
        /// </summary>
        public EntityUid(int uid, int version)
        {
            _uid = uid;
            Version = version;
        }

        public bool Valid => IsValid();

        /// <summary>
        ///     Creates an entity UID by parsing a string number.
        /// </summary>
        public static EntityUid Parse(ReadOnlySpan<char> uid, ReadOnlySpan<char> version)
        {
            return new EntityUid(int.Parse(uid), int.Parse(version));
        }

        public static bool TryParse(ReadOnlySpan<char> uid, ReadOnlySpan<char> version, out EntityUid entityUid)
        {
            try
            {
                entityUid = Parse(uid, version);
                return true;
            }
            catch (FormatException)
            {
                entityUid = Invalid;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EntityUid FromArch(in World world, in Entity entity)
        {
            return new EntityUid(entity.Id + ArchUidOffset, world.Reference(entity).Version + ArchVersionOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetArchId() => _uid - 1;

        /// <summary>
        ///     Checks if the ID value is valid. Does not check if it identifies
        ///     a valid Entity.
        /// </summary>
        [Pure]
        public bool IsValid()
        {
            return _uid > Invalid._uid;
        }

        /// <inheritdoc />
        public bool Equals(EntityUid other)
        {
            return _uid == other._uid && Version == other.Version;
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
            return a._uid == b._uid && a.Version == b.Version;
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

        public static implicit operator Entity(EntityUid self)
        {
            return DangerousEntityExtensions.CreateEntityStruct(self._uid - ArchUidOffset, 0);
        }

        public static implicit operator EntityUid(EntityReference other)
        {
            return new EntityUid(other);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return _uid.ToString();
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return ToString();
        }

        public bool TryFormat(
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format,
            IFormatProvider? provider)
        {
            return _uid.TryFormat(destination, out charsWritten);
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
