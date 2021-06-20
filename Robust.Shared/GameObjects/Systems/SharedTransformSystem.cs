using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects
{
    internal class SharedTransformSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private readonly Queue<MoveEvent> _gridMoves = new();
        private readonly Queue<MoveEvent> _otherMoves = new();

        public override void Initialize()
        {
            base.Initialize();
            _mapManager.TileChanged += MapManagerOnTileChanged;
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
            var anchoredEnts = grid.GetAnchoredEntities(tileIndices);

            foreach (var ent in anchoredEnts.ToList()) // changing anchored modifies this set
            {
                ComponentManager.GetComponent<TransformComponent>(ent).Anchored = false;
            }
        }

        public void DeferMoveEvent(MoveEvent moveEvent)
        {
            if (moveEvent.Sender.HasComponent<IMapGridComponent>())
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
                    if (ev.Sender.Deleted)
                        continue;

                    RaiseLocalEvent(ev);
                }
            }
        }
    }
}
