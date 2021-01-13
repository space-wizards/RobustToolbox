using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Client.Physics;
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
    // NOTE: this class handles both snap grid updates of occluders, as well as occluder tree updates (via its parent).
    // This seems like it's doing somewhat double work because it already has an update queue for occluders but...
    // See the thing is the snap grid stuff was coded earlier
    // and technically it only cares about changes in the entity's SNAP GRID position.
    // Whereas the tree stuff is precise.
    // Also I just realized this and I cba to refactor this again.
    [UsedImplicitly]
    internal sealed class ClientOccluderSystem : OccluderSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private readonly Queue<IEntity> _dirtyEntities = new();

        private uint _updateGeneration;

        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(TransformSystem));
            UpdatesAfter.Add(typeof(PhysicsSystem));

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
                    && entity.TryGetComponent(out ClientOccluderComponent? occluder)
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
                sender.TryGetComponent(out ClientOccluderComponent? iconSmooth)
                && iconSmooth.Running)
            {
                var snapGrid = sender.GetComponent<SnapGridComponent>();

                _dirtyEntities.Enqueue(sender);
                AddValidEntities(snapGrid.GetInDir(Direction.North));
                AddValidEntities(snapGrid.GetInDir(Direction.South));
                AddValidEntities(snapGrid.GetInDir(Direction.East));
                AddValidEntities(snapGrid.GetInDir(Direction.West));
            }

            // Entity is no longer valid, update around the last position it was at.
            if (ev.LastPosition.HasValue && _mapManager.TryGetGrid(ev.LastPosition.Value.grid, out var grid))
            {
                var pos = ev.LastPosition.Value.pos;

                AddValidEntities(grid.GetSnapGridCell(pos + new Vector2i(1, 0), ev.Offset));
                AddValidEntities(grid.GetSnapGridCell(pos + new Vector2i(-1, 0), ev.Offset));
                AddValidEntities(grid.GetSnapGridCell(pos + new Vector2i(0, 1), ev.Offset));
                AddValidEntities(grid.GetSnapGridCell(pos + new Vector2i(0, -1), ev.Offset));
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
        public OccluderDirtyEvent(IEntity sender, (GridId grid, Vector2i pos)? lastPosition, SnapGridOffset offset)
        {
            LastPosition = lastPosition;
            Offset = offset;
            Sender = sender;
        }

        public (GridId grid, Vector2i pos)? LastPosition { get; }
        public SnapGridOffset Offset { get; }
        public IEntity Sender { get; }
    }
}
