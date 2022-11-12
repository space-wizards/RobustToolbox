using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System;

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
    [NetworkedComponent]
    public sealed class MetaDataComponent : Component
    {
        [DataField("name")] internal string? _entityName;
        [DataField("desc")] internal string? _entityDescription;
        internal EntityPrototype? _entityPrototype;

        /// <summary>
        /// When this entity was paused, if applicable
        /// </summary>
        internal TimeSpan? PauseTime;

        // Every entity starts at tick 1, because they are conceptually created in the time between 0->1
        [ViewVariables]
        public GameTick EntityLastModifiedTick { get; internal set; } = new(1);

        /// <summary>
        ///     This is the tick at which the client last applied state data received from the server.
        /// </summary>
        [ViewVariables]
        public GameTick LastStateApplied { get; internal set; } = GameTick.Zero;

        /// <summary>
        ///     This is the most recent tick at which some component was removed from this entity.
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
            set
            {
                string? newValue = value;
                if (_entityPrototype != null && _entityPrototype.Name == newValue)
                    newValue = null;

                if (_entityName == newValue)
                    return;

                _entityName = newValue;
                Dirty();
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
            set
            {
                string? newValue = value;
                if (_entityPrototype != null && _entityPrototype.Description == newValue)
                    newValue = null;

                if(_entityDescription == newValue)
                    return;

                _entityDescription = newValue;
                Dirty();
            }
        }

        /// <summary>
        ///     The prototype this entity was created from, if any.
        /// </summary>
        [ViewVariables]
        public EntityPrototype? EntityPrototype
        {
            get => _entityPrototype;
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
        [ViewVariables]
        public EntityLifeStage EntityLifeStage { get; internal set; }

        [DataField("flags")]
        public MetaDataFlags Flags
        {
            get => _flags;
            internal set
            {
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
        [Access(typeof(MetaDataSystem))]
        public int VisibilityMask = 1;

        [UsedImplicitly, ViewVariables(VVAccess.ReadWrite)]
        private int VVVisibilityMask
        {
            get => VisibilityMask;
            set => IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<MetaDataSystem>().SetVisibilityMask(Owner, value, this);
        }

        [ViewVariables]
        public bool EntityPaused => PauseTime != null;

        public bool EntityInitialized => EntityLifeStage >= EntityLifeStage.Initialized;
        public bool EntityInitializing => EntityLifeStage == EntityLifeStage.Initializing;
        public bool EntityDeleted => EntityLifeStage >= EntityLifeStage.Deleted;

        internal override void ClearTicks()
        {
            // Do not clear modified ticks.
            // MetaDataComponent is used in the game state system to carry initial data like prototype ID.
            // So it ALWAYS has to be sent.
            // (Creation can still be cleared though)
            ClearCreationTick();
        }
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
    }
}
