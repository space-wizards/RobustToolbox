using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    public abstract class SharedTransformSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEntityLookup _entityLookup = default!;

        private readonly Queue<MoveEvent> _gridMoves = new();
        private readonly Queue<MoveEvent> _otherMoves = new();

        public override void Initialize()
        {
            base.Initialize();

            UpdatesOutsidePrediction = true;

            _mapManager.TileChanged += MapManagerOnTileChanged;
            SubscribeLocalEvent<TransformComponent, EntityDirtyEvent>(OnTransformDirty);
        }

        private void OnTransformDirty(EntityUid uid, TransformComponent component, ref EntityDirtyEvent args)
        {
            if (!component.Anchored ||
                !component.ParentUid.IsValid() ||
                MetaData(uid).EntityLifeStage < EntityLifeStage.Initialized)
                return;

            // Anchor dirty
            // May not even need this in future depending what happens with chunk anchoring (paulVS plz).
            var gridComp = EntityManager.GetComponent<IMapGridComponent>(component.ParentUid);

            var grid = (IMapGridInternal) gridComp.Grid;
            DebugTools.Assert(component.GridID == gridComp.GridIndex);
            grid.AnchoredEntDirty(grid.TileIndicesFor(component.Coordinates));
        }

        public override void Shutdown()
        {
            _mapManager.TileChanged -= MapManagerOnTileChanged;
            base.Shutdown();
        }

        private void MapManagerOnTileChanged(object? sender, TileChangedEventArgs e)
        {
            if(e.NewTile.Tile != Tile.Empty)
                return;

            DeparentAllEntsOnTile(e.NewTile.GridIndex, e.NewTile.GridIndices);
        }

        /// <summary>
        ///     De-parents and unanchors all entities on a grid-tile.
        /// </summary>
        /// <remarks>
        ///     Used when a tile on a grid is removed (becomes space). Only de-parents entities if they are actually
        ///     parented to that grid. No more disemboweling mobs. 
        /// </remarks>
        private void DeparentAllEntsOnTile(GridId gridId, Vector2i tileIndices)
        {
            var grid = _mapManager.GetGrid(gridId);
            var gridUid = grid.GridEntityId;
            var mapTransform = Transform(_mapManager.GetMapEntityId(grid.ParentMapId));
         
            foreach (var uid in _entityLookup.GetEntitiesIntersecting(gridId, tileIndices).ToList())
            {
                // AttachParent() will automatically set anchored=false;
                var transform = Transform(uid);
                if (transform.ParentUid == gridUid)
                    transform.AttachParent(mapTransform);
            }
        }

        public void DeferMoveEvent(ref MoveEvent moveEvent)
        {
            if (EntityManager.HasComponent<IMapGridComponent>(moveEvent.Sender))
                _gridMoves.Enqueue(moveEvent);
            else
                _otherMoves.Enqueue(moveEvent);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // Process grid moves first.
            Process(_gridMoves);
            Process(_otherMoves);

            void Process(Queue<MoveEvent> queue)
            {
                while (queue.TryDequeue(out var ev))
                {
                    if (EntityManager.Deleted(ev.Sender))
                    {
                        continue;
                    }

                    // Hopefully we can remove this when PVS gets updated to not use NaNs
                    if (!ev.NewPosition.IsValid(EntityManager))
                    {
                        continue;
                    }

                    RaiseLocalEvent(ev.Sender, ref ev);
                }
            }
        }
    }
}
