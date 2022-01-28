using System;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Represents a world map inside the ECS system.
    /// </summary>
    public interface IMapComponent : IComponent
    {
        bool LightingEnabled { get; set; }
        MapId WorldMap { get; }
        void ClearMapId();
    }

    /// <inheritdoc cref="IMapComponent"/>
    [ComponentReference(typeof(IMapComponent))]
    [NetworkedComponent()]
    public class MapComponent : Component, IMapComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("index")]
        private MapId _mapIndex = MapId.Nullspace;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField(("lightingEnabled"))]
        public bool LightingEnabled { get; set; } = true;

        /// <inheritdoc />
        public MapId WorldMap
        {
            get => _mapIndex;
            internal set => _mapIndex = value;
        }

        /// <inheritdoc />
        public void ClearMapId()
        {
            _mapIndex = MapId.Nullspace;
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new MapComponentState(_mapIndex, LightingEnabled);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not MapComponentState state)
                return;

            _mapIndex = state.MapId;
            LightingEnabled = state.LightingEnabled;

            _entMan.GetComponent<TransformComponent>(Owner).ChangeMapId(_mapIndex);
        }
    }

    /// <summary>
    ///     Serialized state of a <see cref="MapGridComponentState"/>.
    /// </summary>
    [Serializable, NetSerializable]
    internal class MapComponentState : ComponentState
    {
        public MapId MapId { get; }
        public bool LightingEnabled { get; }

        public MapComponentState(MapId mapId, bool lightingEnabled)
        {
            MapId = mapId;
            LightingEnabled = lightingEnabled;
        }
    }
}
