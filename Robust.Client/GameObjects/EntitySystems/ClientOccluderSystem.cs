using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Maths;

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

            SubscribeLocalEvent<OccluderDirtyEvent>(OnOccluderDirty);

            SubscribeLocalEvent<ClientOccluderComponent, AnchorStateChangedEvent>(OnAnchorChanged);
            SubscribeLocalEvent<ClientOccluderComponent, ReAnchorEvent>(OnReAnchor);
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
                    && EntityManager.TryGetComponent(entity, out ClientOccluderComponent? occluder)
                    && occluder.UpdateGeneration != _updateGeneration)
                {
                    occluder.Update();

                    occluder.UpdateGeneration = _updateGeneration;
                }
            }
        }

        private static void OnAnchorChanged(EntityUid uid, ClientOccluderComponent component, ref AnchorStateChangedEvent args)
        {
            component.AnchorStateChanged();
        }

        private void OnReAnchor(EntityUid uid, ClientOccluderComponent component, ref ReAnchorEvent args)
        {
            component.AnchorStateChanged();
        }

        private void OnOccluderDirty(OccluderDirtyEvent ev)
        {
            var sender = ev.Sender;
            MapGridComponent? grid;
            var occluderQuery = GetEntityQuery<ClientOccluderComponent>();

            if (EntityManager.EntityExists(sender) &&
                occluderQuery.HasComponent(sender))
            {
                var xform = EntityManager.GetComponent<TransformComponent>(sender);
                if (!_mapManager.TryGetGrid(xform.GridUid, out grid))
                    return;

                var coords = xform.Coordinates;
                var localGrid = grid.TileIndicesFor(coords);

                _dirtyEntities.Enqueue(sender);
                AddValidEntities(grid.GetAnchoredEntitiesEnumerator(localGrid + new Vector2i(0, 1)), occluderQuery);
                AddValidEntities(grid.GetAnchoredEntitiesEnumerator(localGrid + new Vector2i(0, -1)), occluderQuery);
                AddValidEntities(grid.GetAnchoredEntitiesEnumerator(localGrid + new Vector2i(1, 0)), occluderQuery);
                AddValidEntities(grid.GetAnchoredEntitiesEnumerator(localGrid + new Vector2i(-1, 0)), occluderQuery);
            }

            // Entity is no longer valid, update around the last position it was at.
            else if (ev.LastPosition.HasValue && _mapManager.TryGetGrid(ev.LastPosition.Value.grid, out grid))
            {
                var pos = ev.LastPosition.Value.pos;

                AddValidEntities(grid.GetAnchoredEntitiesEnumerator(pos + new Vector2i(0, 1)), occluderQuery);
                AddValidEntities(grid.GetAnchoredEntitiesEnumerator(pos + new Vector2i(0, -1)), occluderQuery);
                AddValidEntities(grid.GetAnchoredEntitiesEnumerator(pos + new Vector2i(1, 0)), occluderQuery);
                AddValidEntities(grid.GetAnchoredEntitiesEnumerator(pos + new Vector2i(-1, 0)), occluderQuery);
            }
        }

        private void AddValidEntities(AnchoredEntitiesEnumerator enumerator, EntityQuery<ClientOccluderComponent> occluderQuery)
        {
            while (enumerator.MoveNext(out var entity))
            {
                if (!occluderQuery.HasComponent(entity.Value)) continue;

                _dirtyEntities.Enqueue(entity.Value);
            }
        }
    }

    /// <summary>
    ///     Event raised by a <see cref="ClientOccluderComponent"/> when it needs to be recalculated.
    /// </summary>
    internal sealed class OccluderDirtyEvent : EntityEventArgs
    {
        public OccluderDirtyEvent(EntityUid sender, (EntityUid grid, Vector2i pos)? lastPosition)
        {
            LastPosition = lastPosition;
            Sender = sender;
        }

        public (EntityUid grid, Vector2i pos)? LastPosition { get; }
        public EntityUid Sender { get; }
    }
}
