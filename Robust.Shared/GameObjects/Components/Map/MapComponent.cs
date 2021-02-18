using System;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Represents a world map inside the ECS system.
    /// </summary>
    public interface IMapComponent : IComponent
    {
        MapId WorldMap { get; }
        void ClearMapId();
    }

    /// <inheritdoc cref="IMapComponent"/>
    public class MapComponent : Component, IMapComponent
    {
        [ViewVariables(VVAccess.ReadOnly)]
        private MapId _mapIndex;

        /// <inheritdoc />
        public override string Name => "Map";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.MAP_MAP;

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

        /// <param name="player"></param>
        /// <inheritdoc />
        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new MapComponentState(_mapIndex);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is MapComponentState state))
                return;

            _mapIndex = state.MapId;

            ((TransformComponent) Owner.Transform).ChangeMapId(_mapIndex);
        }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _mapIndex, "index", MapId.Nullspace);
        }
    }

    /// <summary>
    ///     Serialized state of a <see cref="MapGridComponentState"/>.
    /// </summary>
    [Serializable, NetSerializable]
    internal class MapComponentState : ComponentState
    {
        public MapId MapId { get; }

        public MapComponentState(MapId mapId)
            : base(NetIDs.MAP_MAP)
        {
            MapId = mapId;
        }
    }
}
