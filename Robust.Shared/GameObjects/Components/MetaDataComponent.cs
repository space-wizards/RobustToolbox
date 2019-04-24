using System;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class MetaDataComponentState : ComponentState
    {
        public string Name { get; }
        public string Description { get; }
        public string PrototypeId { get; }

        public MetaDataComponentState(uint netID, string name, string description, string prototypeId) : base(netID)
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
        EntityPrototype EntityPrototype { get; set; }
    }

    class MetaDataComponent : Component, IMetaDataComponent
    {
        [Dependency] private readonly IPrototypeManager _prototypes;

        private string _entityName;
        private string _entityDescription;
        private EntityPrototype _entityPrototype;

        /// <inheritdoc />
        public override string Name => "MetaData";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.META_DATA;

        /// <inheritdoc />
        public override Type StateType => typeof(MetaDataComponentState);

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public string EntityName
        {
            get => _entityName;
            set
            {
                if(_entityName == value)
                    return;

                _entityName = value;
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
                    return EntityPrototype.Description;
                return _entityDescription;
            }
            set
            {
                if(_entityDescription == value)
                    return;

                _entityDescription = value;
                Dirty();
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public EntityPrototype EntityPrototype
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
            return new MetaDataComponentState(NetID.Value, _entityName, _entityDescription, EntityPrototype.ID);
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);
            
            if (!(curState is MetaDataComponentState state))
                return;

            _entityName = state.Name;
            _entityDescription = state.Description;
            _entityPrototype = _prototypes.Index<EntityPrototype>(state.PrototypeId);
        }
    }
}
