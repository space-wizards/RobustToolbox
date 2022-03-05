using System;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
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
        ///     Constructs a new instance of <see cref="MetaDataComponentState"/>.
        /// </summary>
        /// <param name="name">The in-game name of this entity.</param>
        /// <param name="description">The in-game description of this entity.</param>
        /// <param name="prototypeId">The prototype this entity was created from, if any.</param>
        public MetaDataComponentState(string? name, string? description, string? prototypeId)
        {
            Name = name;
            Description = description;
            PrototypeId = prototypeId;
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
        private bool _entityPaused;

        // Every entity starts at tick 1, because they are conceptually created in the time between 0->1
        [ViewVariables]
        public GameTick EntityLastModifiedTick { get; internal set; } = new(1);

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

        [ViewVariables]
        public MetaDataFlags Flags { get; internal set; }

        /// <summary>
        ///     The sum of our visibility layer and our parent's visibility layers.
        ///     Server-only.
        /// </summary>
        [ViewVariables]
        public int VisibilityMask { get; internal set; }

        [ViewVariables]
        public bool EntityPaused
        {
            get => _entityPaused;
            set
            {
                if (_entityPaused == value)
                    return;

                var entMan = IoCManager.Resolve<IEntityManager>();
                if (value && entMan.HasComponent<IgnorePauseComponent>(Owner))
                    return;

                _entityPaused = value;
                entMan.EventBus.RaiseLocalEvent(Owner, new EntityPausedEvent(Owner, value));
            }
        }

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
        /// Whether the entity has states specific to a particular player.
        /// </summary>
        EntitySpecific = 1 << 0,
        /// <summary>
        /// Whether the entity is currently inside of a container.
        /// </summary>
        InContainer = 1 << 1,
    }
}
