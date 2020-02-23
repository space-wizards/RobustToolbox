using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects.EntitySystems
{
    internal sealed class OccluderSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        private readonly Queue<IEntity> _dirtyEntities = new Queue<IEntity>();

        private uint _updateGeneration;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<OccluderDirtyEvent>(HandleDirtyEvent);
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            if (_dirtyEntities.Count == 0)
            {
                return;
            }

            _updateGeneration += 1;

            while (_dirtyEntities.TryDequeue(out var entity))
            {
                if (!entity.Deleted
                    && entity.TryGetComponent(out ClientOccluderComponent occluder)
                    && occluder.UpdateGeneration != _updateGeneration)
                {
                    occluder.Update();

                    occluder.UpdateGeneration = _updateGeneration;
                }
            }
        }

        private void HandleDirtyEvent(OccluderDirtyEvent ev)
        {
            var sender = ev.Sender;
            if (sender.IsValid() &&
                sender.TryGetComponent(out ClientOccluderComponent iconSmooth)
                && iconSmooth.Running)
            {
                var snapGrid = sender.GetComponent<SnapGridComponent>();

                _dirtyEntities.Enqueue(sender);
                AddValidEntities(snapGrid.GetInDir(Direction.North));
                AddValidEntities(snapGrid.GetInDir(Direction.South));
                AddValidEntities(snapGrid.GetInDir(Direction.East));
                AddValidEntities(snapGrid.GetInDir(Direction.West));
            }

            if (ev.LastPosition.HasValue)
            {
                // Entity is no longer valid, update around the last position it was at.
                var grid = _mapManager.GetGrid(ev.LastPosition.Value.grid);
                var pos = ev.LastPosition.Value.pos;

                AddValidEntities(grid.GetSnapGridCell(pos + new MapIndices(1, 0), ev.Offset));
                AddValidEntities(grid.GetSnapGridCell(pos + new MapIndices(-1, 0), ev.Offset));
                AddValidEntities(grid.GetSnapGridCell(pos + new MapIndices(0, 1), ev.Offset));
                AddValidEntities(grid.GetSnapGridCell(pos + new MapIndices(0, -1), ev.Offset));
            }
        }

        private void AddValidEntities(IEnumerable<IEntity> candidates)
        {
            foreach (var entity in candidates)
            {
                if (entity.HasComponent<ClientOccluderComponent>())
                {
                    _dirtyEntities.Enqueue(entity);
                }
            }
        }

        private void AddValidEntities(IEnumerable<IComponent> candidates)
        {
            AddValidEntities(candidates.Select(c => c.Owner));
        }
    }

    /// <summary>
    ///     Event raised by a <see cref="ClientOccluderComponent"/> when it needs to be recalculated.
    /// </summary>
    internal sealed class OccluderDirtyEvent : EntitySystemMessage
    {
        public OccluderDirtyEvent(IEntity sender, (GridId grid, MapIndices pos)? lastPosition, SnapGridOffset offset)
        {
            LastPosition = lastPosition;
            Offset = offset;
            Sender = sender;
        }

        public (GridId grid, MapIndices pos)? LastPosition { get; }
        public SnapGridOffset Offset { get; }
        public IEntity Sender { get; }
    }
}
