using System;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Serialized state of a <see cref="MetaDataComponent"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public class MetaDataComponentState : ComponentState
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
            : base(NetIDs.META_DATA)
        {
            Name = name;
            Description = description;
            PrototypeId = prototypeId;
        }
    }

    /// <summary>
    ///     Contains meta data about this entity that isn't component specific.
    /// </summary>
    public interface IMetaDataComponent
    {
        /// <summary>
        ///     The in-game name of this entity.
        /// </summary>
        string EntityName { get; set; }

        /// <summary>
        ///     The in-game description of this entity.
        /// </summary>
        string EntityDescription { get; set; }

        /// <summary>
        ///     The prototype this entity was created from, if any.
        /// </summary>
        EntityPrototype? EntityPrototype { get; set; }
    }

    /// <inheritdoc cref="IMetaDataComponent"/>
    internal class MetaDataComponent : Component, IMetaDataComponent
    {
        [Dependency] private readonly IPrototypeManager _prototypes = default!;

        [YamlField("name")]
        private string? _entityName;
        [YamlField("desc")]
        private string? _entityDescription;
        private EntityPrototype? _entityPrototype;

        /// <inheritdoc />
        public override string Name => "MetaData";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.META_DATA;

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new MetaDataComponentState(_entityName, _entityDescription, EntityPrototype?.ID);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is MetaDataComponentState state))
                return;

            _entityName = state.Name;
            _entityDescription = state.Description;

            if(state.PrototypeId != null)
                _entityPrototype = _prototypes.Index<EntityPrototype>(state.PrototypeId);
        }

        internal override void ClearTicks()
        {
            // Do not clear modified ticks.
            // MetaDataComponent is used in the game state system to carry initial data like prototype ID.
            // So it ALWAYS has to be sent.
            // (Creation can still be cleared though)
            ClearCreationTick();
        }
    }
}
