using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects.Components.Map
{
    /// <summary>
    ///     Represents a map grid inside the ECS system.
    /// </summary>
    internal interface IMapGridComponent : IComponent
    {
        GridId GridIndex { get; }
        IMapGrid Grid { get; }
        void ClearGridId();
    }

    /// <inheritdoc cref="IMapGridComponent"/>
    internal class MapGridComponent : Component, IMapGridComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        [ViewVariables(VVAccess.ReadOnly)]
        private GridId _gridIndex;

        /// <inheritdoc />
        public override string Name => "MapGrid";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.MAP_GRID;

        /// <inheritdoc />
        public GridId GridIndex
        {
            get => _gridIndex;
            internal set => _gridIndex = value;
        }

        /// <inheritdoc />
        public IMapGrid Grid => _mapManager.GetGrid(_gridIndex);

        public void ClearGridId()
        {
            _gridIndex = GridId.Invalid;
        }

        public override void OnRemove()
        {
            if(GridIndex != GridId.Invalid)
            {
                if(_mapManager.GridExists(_gridIndex))
                {
                    Logger.DebugS("map", $"Entity {Owner.Uid} removed grid component, removing bound grid {_gridIndex}");
                    _mapManager.DeleteGrid(_gridIndex);
                }
            }

            base.OnRemove();
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new MapGridComponentState(_gridIndex);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is MapGridComponentState state))
                return;

            _gridIndex = state.GridIndex;
        }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _gridIndex, "index", GridId.Invalid);
        }
    }

    /// <summary>
    ///     Serialized state of a <see cref="MapGridComponentState"/>.
    /// </summary>
    [Serializable, NetSerializable]
    internal class MapGridComponentState : ComponentState
    {
        /// <summary>
        ///     Index of the grid this component is linked to.
        /// </summary>
        public GridId GridIndex { get; }

        /// <summary>
        ///     Constructs a new instance of <see cref="MapGridComponentState"/>.
        /// </summary>
        /// <param name="gridIndex">Index of the grid this component is linked to.</param>
        public MapGridComponentState(GridId gridIndex)
            : base(NetIDs.MAP_GRID)
        {
            GridIndex = gridIndex;
        }
    }
}
