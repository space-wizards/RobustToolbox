using System;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Represents a map grid inside the ECS system.
    /// </summary>
    public interface IMapGridComponent : IComponent
    {
        GridId GridIndex { get; }
        IMapGrid Grid { get; }
        void ClearGridId();

        bool AnchorEntity(TransformComponent transform);
        void UnanchorEntity(TransformComponent transform);
        void AnchoredEntityDirty(TransformComponent transform);
    }

    /// <inheritdoc cref="IMapGridComponent"/>
    [ComponentReference(typeof(IMapGridComponent))]
    [NetworkedComponent()]
    internal class MapGridComponent : Component, IMapGridComponent
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("index")]
        private GridId _gridIndex = GridId.Invalid;

        /// <inheritdoc />
        public override string Name => "MapGrid";

        /// <inheritdoc />
        public GridId GridIndex
        {
            get => _gridIndex;
            internal set => _gridIndex = value;
        }

        /// <inheritdoc />
        [ViewVariables]
        public IMapGrid Grid => _mapManager.GetGrid(_gridIndex);

        /// <inheritdoc />
        public void ClearGridId()
        {
            _gridIndex = GridId.Invalid;
        }

        protected override void Initialize()
        {
            base.Initialize();
            var mapId = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner.Uid).MapID;

            if (_mapManager.HasMapEntity(mapId))
            {
                IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner.Uid).AttachParent(_mapManager.GetMapEntity(mapId));
            }
        }

        /// <inheritdoc />
        public bool AnchorEntity(TransformComponent transform)
        {
            var xform = (TransformComponent) transform;
            var tileIndices = Grid.TileIndicesFor(transform.Coordinates);
            var result = Grid.AddToSnapGridCell(tileIndices, transform.OwnerUid);

            if (result)
            {
                xform.Parent = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner.Uid);

                // anchor snapping
                xform.LocalPosition = Grid.GridTileToLocal(tileIndices).Position;

                xform.SetAnchored(result);

                if (IoCManager.Resolve<IEntityManager>().TryGetComponent<PhysicsComponent?>(xform.Owner.Uid, out var physicsComponent))
                {
                    physicsComponent.BodyType = BodyType.Static;
                }
            }

            return result;
        }

        /// <inheritdoc />
        public void UnanchorEntity(TransformComponent transform)
        {
            //HACK: Client grid pivot causes this.
            //TODO: make grid components the actual grid
            if(GridIndex == GridId.Invalid)
                return;

            var xform = (TransformComponent)transform;
            var tileIndices = Grid.TileIndicesFor(transform.Coordinates);
            Grid.RemoveFromSnapGridCell(tileIndices, transform.OwnerUid);
            xform.SetAnchored(false);
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent<PhysicsComponent?>(xform.Owner.Uid, out var physicsComponent))
            {
                physicsComponent.BodyType = BodyType.Dynamic;
            }
        }

        /// <inheritdoc />
        public void AnchoredEntityDirty(TransformComponent transform)
        {
            if (!transform.Anchored)
                return;

            var grid = (IMapGridInternal) _mapManager.GetGrid(transform.GridID);
            grid.AnchoredEntDirty(grid.TileIndicesFor(transform.Coordinates));
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new MapGridComponentState(_gridIndex);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not MapGridComponentState state)
                return;

            _gridIndex = state.GridIndex;
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
        {
            GridIndex = gridIndex;
        }
    }
}
