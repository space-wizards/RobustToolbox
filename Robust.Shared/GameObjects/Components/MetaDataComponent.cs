using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Serialized state of a <see cref="MetaDataComponent"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MetaDataComponentState : ComponentState
    {
        /// <summary>
        ///     The in-game name of this entity.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        ///     The in-game description of this entity.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        ///     The prototype this entity was created from, if any.
        /// </summary>
        public string? PrototypeId { get; }

        /// <summary>
        ///     When this entity was paused.
        /// </summary>
        public TimeSpan? PauseTime;

        /// <summary>
        ///     Constructs a new instance of <see cref="MetaDataComponentState"/>.
        /// </summary>
        /// <param name="name">The in-game name of this entity.</param>
        /// <param name="description">The in-game description of this entity.</param>
        /// <param name="prototypeId">The prototype this entity was created from, if any.</param>
        /// <param name="pauseTime">When this entity was paused.</param>
        public MetaDataComponentState(string? name, string? description, string? prototypeId, TimeSpan? pauseTime)
        {
            Name = name;
            Description = description;
            PrototypeId = prototypeId;
            PauseTime = pauseTime;
        }
    }

    /// <summary>
    ///     Contains meta data about this entity that isn't component specific.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class MetaDataComponent : Component
    {
        [DataField("name")] internal string? _entityName;
        [DataField("desc")] internal string? _entityDescription;
        internal EntityPrototype? _entityPrototype;

        /// <summary>
        /// The components attached to the entity that are currently networked.
        /// </summary>
        [ViewVariables]
        internal readonly Dictionary<ushort, IComponent> NetComponents = new();

        /// <summary>
        /// Network identifier for this entity.
        /// </summary>
        [ViewVariables]
        [Access(typeof(EntityManager), Other = AccessPermissions.ReadExecute)]
        public NetEntity NetEntity { get; internal set; } = NetEntity.Invalid;

        /// <summary>
        /// When this entity was paused, if applicable. Note that this is the actual time, not the duration which gets
        /// returned by <see cref="MetaDataSystem.GetPauseTime"/>.
        /// </summary>
        internal TimeSpan? PauseTime;

        // Every entity starts at tick 1, because they are conceptually created in the time between 0->1
        [ViewVariables]
        public GameTick EntityLastModifiedTick { get; internal set; } = GameTick.First;

        /// <summary>
        ///     This is the tick at which the client last applied state data received from the server.
        /// </summary>
        [ViewVariables]
        public GameTick LastStateApplied { get; internal set; } = GameTick.Zero;

        /// <summary>
        ///     This is the most recent tick at which a networked component was removed from this entity.
        ///     Currently only reliable server-side, client side prediction may cause the value to be wrong.
        /// </summary>
        [ViewVariables]
        public GameTick LastComponentRemoved { get; internal set; } = GameTick.Zero;

        /// <summary>
        ///     The in-game name of this entity.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public string EntityName
        {
            get
            {
                if (_entityName == null)
                    return _entityPrototype != null ? _entityPrototype.Name : string.Empty;
                return _entityName;
            }
        }

        /// <summary>
        ///     The in-game description of this entity.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public string EntityDescription
        {
            get
            {
                if (_entityDescription == null)
                    return _entityPrototype != null ? _entityPrototype.Description : string.Empty;
                return _entityDescription;
            }
        }

        /// <summary>
        ///     The prototype this entity was created from, if any.
        /// </summary>
        [ViewVariables]
        public EntityPrototype? EntityPrototype
        {
            get => _entityPrototype;
            [Obsolete("Use MetaDataSystem.SetEntityPrototype")]
            set
            {
                _entityPrototype = value;
                Dirty();
            }
        }

        /// <summary>
        ///     The current lifetime stage of this entity. You can use this to check
        ///     if the entity is initialized or being deleted.
        /// </summary>
        [ViewVariables, Access(typeof(EntityManager), Other = AccessPermissions.ReadExecute)]
        public EntityLifeStage EntityLifeStage { get; internal set; }

        public MetaDataFlags Flags
        {
            get => _flags;
            internal set
            {
                if (_flags == value)
                    return;

                // In container and detached to null are mutually exclusive flags.
                DebugTools.Assert((value & (MetaDataFlags.InContainer | MetaDataFlags.Detached)) != (MetaDataFlags.InContainer | MetaDataFlags.Detached));
                _flags = value;
            }
        }

        internal MetaDataFlags _flags;

        /// <summary>
        ///     The sum of our visibility layer and our parent's visibility layers.
        /// </summary>
        /// <remarks>
        ///     Every entity will always have the first bit set to true.
        /// </remarks>
        [ViewVariables] // TODO ACCESS RRestrict writing to server-side visibility system
        public ushort VisibilityMask { get; internal set; }= 1;

        [ViewVariables]
        public bool EntityPaused => PauseTime != null;

        public bool EntityInitialized => EntityLifeStage >= EntityLifeStage.Initialized;
        public bool EntityInitializing => EntityLifeStage == EntityLifeStage.Initializing;
        public bool EntityDeleted => EntityLifeStage >= EntityLifeStage.Deleted;

        /// <summary>
        /// The PVS chunk that this entity is currently stored on.
        /// This should always be set properly if the entity is directly attached to a grid or map.
        /// If it is null, it implies that either:
        /// - The entity nested is somewhere in some chunk that has already been marked as dirty
        /// - The entity is in nullspace
        /// </summary>
        [ViewVariables]
        internal PvsChunkLocation? LastPvsLocation;

        private protected override void ClearTicks()
        {
            // Do not clear modified ticks.
            // MetaDataComponent is used in the game state system to carry initial data like prototype ID.
            // So it ALWAYS has to be sent.
            // (Creation can still be cleared though)
            ClearCreationTick();
        }

        /// <summary>
        /// Offset into internal PVS data.
        /// </summary>
        internal PvsIndex PvsData = PvsIndex.Invalid;
    }

    [Flags]
    public enum MetaDataFlags : byte
    {
        None = 0,

        /// <summary>
        /// Whether the entity has any component that has state information specific to particular players.
        /// </summary>
        SessionSpecific = 1 << 0,

        /// <summary>
        /// Whether the entity is currently inside of a container.
        /// </summary>
        InContainer = 1 << 1,

        /// <summary>
        /// Used by clients to indicate that an entity has left their visible set.
        /// </summary>
        Detached = 1 << 2,

        /// <summary>
        /// Indicates this entity can never be handled by the client as PVS detached.
        /// </summary>
        Undetachable = 1 << 3,

        /// <summary>
        /// If true, then this entity is considered a "high priority" entity and will be sent to players from further
        /// away. Useful for things like light sources and occluders. Only works if the entity is directly parented to
        /// a grid or map.
        /// </summary>
        PvsPriority = 1 << 4,

        /// <summary>
        /// If set, transform system will raise events directed at this entity whenever the GridUid or MapUid are modified.
        /// </summary>
        ExtraTransformEvents = 1 << 5,
    }

    /// <summary>
    /// Key struct for uniquely identifying a PVS chunk.
    /// </summary>
    internal readonly record struct PvsChunkLocation(EntityUid Uid, Vector2i Indices);

    /// <summary>
    /// An opaque index into the PVS data arrays on the server.
    /// </summary>
    internal readonly record struct PvsIndex(int Index)
    {
        /// <summary>
        /// An invalid index. This is also used as a marker value in the free list.
        /// </summary>
        public static readonly PvsIndex Invalid = new PvsIndex(-1);
        // TODO PVS
        // Consider making 0 an invalid value.
        // it prevents default structs from accidentally being used.
    }
}
