using System;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
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

        bool AnchorEntity(TransformComponent transform);
        void UnanchorEntity(TransformComponent transform);
    }

    /// <inheritdoc cref="IMapGridComponent"/>
    [ComponentReference(typeof(IMapGridComponent))]
    [NetworkedComponent]
    internal sealed class MapGridComponent : Component, IMapGridComponent
    {
        [Dependency] private readonly IMapManagerInternal _mapManager = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        // This field is used for deserialization internally in the map loader.
        // If you want to remove this, you would have to restructure the map save file.
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("index")]
        private GridId _gridIndex = GridId.Invalid;

        private IMapGrid? _mapGrid;

        /// <inheritdoc />
        public GridId GridIndex
        {
            get => _gridIndex;
            internal set => _gridIndex = value;
        }

        [DataField("chunkSize")]
        private ushort _chunkSize = 16;

        /// <inheritdoc />
        [ViewVariables]
        public IMapGrid Grid
        {
            get => _mapGrid ?? throw new InvalidOperationException();
            private set => _mapGrid = value;
        }

        protected override void Initialize()
        {
            base.Initialize();
            var mapId = _entMan.GetComponent<TransformComponent>(Owner).MapID;

            if (_mapManager.HasMapEntity(mapId))
            {
                _entMan.GetComponent<TransformComponent>(Owner).AttachParent(_mapManager.GetMapEntityIdOrThrow(mapId));
            }
        }

        protected override void OnRemove()
        {
            _mapManager.TrueGridDelete((MapGrid)_mapGrid!);

            base.OnRemove();
        }

        /// <inheritdoc />
        public bool AnchorEntity(TransformComponent transform)
        {
            var xform = transform;
            var tileIndices = Grid.TileIndicesFor(transform.Coordinates);
            var result = Grid.AddToSnapGridCell(tileIndices, transform.Owner);

            if (result)
            {
                xform.ParentUid = Owner;

                // anchor snapping
                xform.LocalPosition = Grid.GridTileToLocal(tileIndices).Position;

                xform.SetAnchored(result);

                if (_entMan.TryGetComponent<PhysicsComponent?>(xform.Owner, out var physicsComponent))
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

            var xform = transform;
            var tileIndices = Grid.TileIndicesFor(transform.Coordinates);
            Grid.RemoveFromSnapGridCell(tileIndices, transform.Owner);
            xform.SetAnchored(false);
            if (_entMan.TryGetComponent<PhysicsComponent?>(xform.Owner, out var physicsComponent))
            {
                physicsComponent.BodyType = BodyType.Dynamic;
            }
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new MapGridComponentState(_gridIndex, _chunkSize);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not MapGridComponentState state)
                return;

            _gridIndex = state.GridIndex;
            _chunkSize = state.ChunkSize;
        }

        public MapGrid AllocMapGrid(ushort chunkSize, ushort tileSize)
        {
            DebugTools.Assert(LifeStage == ComponentLifeStage.Added);

            var grid = new MapGrid(_mapManager, _entMan, GridIndex, chunkSize);
            grid.TileSize = tileSize;

            Grid = grid;
            grid.GridEntityId = Owner;

            _mapManager.OnGridAllocated(this, grid);
            return grid;
        }
    }

    /// <summary>
    ///     Serialized state of a <see cref="MapGridComponentState"/>.
    /// </summary>
    [Serializable, NetSerializable]
    internal sealed class MapGridComponentState : ComponentState
    {
        /// <summary>
        ///     Index of the grid this component is linked to.
        /// </summary>
        public GridId GridIndex { get; }

        /// <summary>
        ///     The size of the chunks in the map grid.
        /// </summary>
        public ushort ChunkSize { get; }

        /// <summary>
        ///     Constructs a new instance of <see cref="MapGridComponentState"/>.
        /// </summary>
        /// <param name="gridIndex">Index of the grid this component is linked to.</param>
        /// <param name="chunkSize">The size of the chunks in the map grid.</param>
        public MapGridComponentState(GridId gridIndex, ushort chunkSize)
        {
            GridIndex = gridIndex;
            ChunkSize = chunkSize;
        }
    }
}
