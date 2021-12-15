using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    internal abstract class SharedTransformSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private readonly Queue<MoveEvent> _gridMoves = new();
        private readonly Queue<MoveEvent> _otherMoves = new();

        public override void Initialize()
        {
            base.Initialize();
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

            var grid = _mapManager.GetGrid(e.NewTile.GridIndex);
            var tileIndices = e.NewTile.GridIndices;
            UnanchorAllEntsOnTile(grid, tileIndices);
        }

        private void UnanchorAllEntsOnTile(IMapGrid grid, Vector2i tileIndices)
        {
            var anchoredEnts = grid.GetAnchoredEntities(tileIndices).Where(e => EntityManager.EntityExists(e)).ToList();

            if (anchoredEnts.Count == 0) return;

            var mapEnt = _mapManager.GetMapEntityIdOrThrow(grid.ParentMapId);

            foreach (var ent in anchoredEnts) // changing anchored modifies this set
            {
                var transform = EntityManager.GetComponent<TransformComponent>(ent);
                transform.Anchored = false;
                // If the tile was nuked than that means no longer intersecting the grid hence parent to the map
                transform.AttachParent(mapEnt);
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
