using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Client.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
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

        private readonly Queue<EntityUid> _dirtyEntities = new();

        private uint _updateGeneration;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(TransformSystem));
            UpdatesAfter.Add(typeof(PhysicsSystem));

            SubscribeLocalEvent<OccluderDirtyEvent>(HandleDirtyEvent);

            SubscribeLocalEvent<ClientOccluderComponent, AnchorStateChangedEvent>(HandleAnchorChanged);
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
                if (EntityManager.EntityExists(entity)
                    && ComponentManager.TryGetComponent(entity, out ClientOccluderComponent? occluder)
                    && occluder.UpdateGeneration != _updateGeneration)
                {
                    occluder.Update();

                    occluder.UpdateGeneration = _updateGeneration;
                }
            }
        }

        private static void HandleAnchorChanged(EntityUid uid, ClientOccluderComponent component, ref AnchorStateChangedEvent args)
        {
            component.AnchorStateChanged();
        }

        private void HandleDirtyEvent(OccluderDirtyEvent ev)
        {
            var sender = ev.Sender;
            if (sender.IsValid() &&
                sender.TryGetComponent(out ClientOccluderComponent? iconSmooth)
                && iconSmooth.Running)
            {
                var grid1 = _mapManager.GetGrid(sender.Transform.GridID);
                var coords = sender.Transform.Coordinates;

                _dirtyEntities.Enqueue(sender.Uid);
                AddValidEntities(grid1.GetInDir(coords, Direction.North));
                AddValidEntities(grid1.GetInDir(coords, Direction.South));
                AddValidEntities(grid1.GetInDir(coords, Direction.East));
                AddValidEntities(grid1.GetInDir(coords, Direction.West));
            }

            // Entity is no longer valid, update around the last position it was at.
            if (ev.LastPosition.HasValue && _mapManager.TryGetGrid(ev.LastPosition.Value.grid, out var grid))
            {
                var pos = ev.LastPosition.Value.pos;

                AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(1, 0)));
                AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(-1, 0)));
                AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(0, 1)));
                AddValidEntities(grid.GetAnchoredEntities(pos + new Vector2i(0, -1)));
            }
        }

        private void AddValidEntities(IEnumerable<EntityUid> candidates)
        {
            foreach (var entity in candidates)
            {
                if (ComponentManager.HasComponent<ClientOccluderComponent>(entity))
                {
                    _dirtyEntities.Enqueue(entity);
                }
            }
        }
    }

    /// <summary>
    ///     Event raised by a <see cref="ClientOccluderComponent"/> when it needs to be recalculated.
    /// </summary>
    internal sealed class OccluderDirtyEvent : EntityEventArgs
    {
        public OccluderDirtyEvent(IEntity sender, (GridId grid, Vector2i pos)? lastPosition)
        {
            LastPosition = lastPosition;
            Sender = sender;
        }

        public (GridId grid, Vector2i pos)? LastPosition { get; }
        public IEntity Sender { get; }
    }
}
