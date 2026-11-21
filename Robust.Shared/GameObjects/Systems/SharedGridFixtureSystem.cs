using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Events;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    public abstract partial class SharedGridFixtureSystem : EntitySystem
    {
        [Dependency] private FixtureSystem _fixtures = default!;
        [Dependency] private SharedMapSystem _map = default!;
        [Dependency] private IConfigurationManager _cfg = default!;
        [Dependency] private EntityQuery<MapComponent> _mapQuery = default!;
        [Dependency] private EntityQuery<PhysicsComponent> _bodyQuery = default!;
        [Dependency] private EntityQuery<FixturesComponent> _fixturesQuery = default!;
        [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;

        private bool _enabled;
        private float _fixtureEnlargement;
        private readonly Dictionary<string, Fixture> _changedFixtures = new();
        private readonly Dictionary<string, Fixture> _newFixtures = new();

        internal const string ShowGridNodesCommand = "showgridnodes";

        public override void Initialize()
        {
            base.Initialize();
            UpdatesBefore.Add(typeof(SharedBroadphaseSystem));

            Subs.CVar(_cfg, CVars.GenerateGridFixtures, SetEnabled, true);
            Subs.CVar(_cfg, CVars.GridFixtureEnlargement, SetEnlargement, true);
        }

        [SubscribeLocalEvent]
        private void OnGridBoundsRegenerate(ref RegenerateGridBoundsEvent ev)
        {
            RegenerateCollision(ev.Entity, ev.ChunkRectangles, ev.RemovedChunks, ev.Grid);
        }

        [SubscribeLocalEvent]
        protected virtual void OnGridInit(GridInitializeEvent ev)
        {
            if (_mapQuery.HasComponent(ev.EntityUid))
                return;

            // This will also check for grid splits if applicable.
            var grid = ev.Grid;
            _map.RegenerateCollision(ev.EntityUid, grid, _map.GetMapChunks(ev.EntityUid, grid).Values.ToHashSet());
        }

        private void SetEnabled(bool value) => _enabled = value;

        private void SetEnlargement(float value) => _fixtureEnlargement = value;

        internal void RegenerateCollision(
            EntityUid uid,
            Dictionary<MapChunk, List<Box2i>> mapChunks,
            List<MapChunk> removedChunks,
            MapGridComponent? grid = null)
        {
            if (!_enabled)
                return;

            if (!_bodyQuery.TryGetComponent(uid, out var body))
            {
                Log.Error($"Trying to regenerate collision for {uid} that doesn't have {nameof(body)}");
                return;
            }

            if (!_fixturesQuery.TryGetComponent(uid, out var manager))
            {
                Log.Error($"Trying to regenerate collision for {uid} that doesn't have {nameof(manager)}");
                return;
            }

            if (!_xformQuery.TryGetComponent(uid, out var xform))
            {
                Log.Error($"Trying to regenerate collision for {uid} that doesn't have {nameof(TransformComponent)}");
                return;
            }

            _changedFixtures.Clear();
            var anyUpdated = false;

            foreach (var (chunk, rectangles) in mapChunks)
            {
                if (!UpdateFixture(uid, chunk, rectangles, body, manager, xform))
                    continue;

                anyUpdated = true;

                foreach (var id in chunk.Fixtures)
                {
                    _changedFixtures[id] = manager.Fixtures[id];
                }
            }

            if (!anyUpdated)
            {
                CheckSplit(uid, mapChunks, removedChunks, grid);
                return;
            }

            EntityManager.EventBus.RaiseLocalEvent(uid,new GridFixtureChangeEvent {NewFixtures = _changedFixtures}, true);
            _fixtures.FixtureUpdate(uid, manager: manager, body: body);

            CheckSplit(uid, mapChunks, removedChunks, grid);
        }

        internal virtual void CheckSplit(EntityUid gridEuid, Dictionary<MapChunk, List<Box2i>> mapChunks,
            List<MapChunk> removedChunks, MapGridComponent? grid = null) {}

        internal virtual void CheckSplit(EntityUid gridEuid, MapChunk chunk, List<Box2i> rectangles, MapGridComponent? grid = null) {}

        private bool UpdateFixture(EntityUid uid, MapChunk chunk, List<Box2i> rectangles, PhysicsComponent body, FixturesComponent manager, TransformComponent xform)
        {
            var origin = chunk.Indices * chunk.ChunkSize;

            // We also ideally want to avoid re-creating the fixture every time a tile changes and pushing that data
            // to the client hence we diff it.
            // Additionally, we need to handle map deserialization where content may have stored its own data
            // on the grid (e.g. mass) which we want to preserve.
            _newFixtures.Clear();

            foreach (var rectangle in rectangles)
            {
                var tileBounds = rectangle.Translated(origin);
                var bounds = ((Box2) tileBounds).Enlarged(_fixtureEnlargement);
                var key = string.Create(CultureInfo.InvariantCulture, $"grid_chunk-{tileBounds.Left}-{tileBounds.Bottom}");
                _newFixtures.Add(key, CreateGridFixture(uid, bounds));
            }

            var updated = false;
            var toRemove = new ValueList<string>();

            // Cross-reference old fixtures by ID. If the shape hasn't changed, keep the existing fixture
            // to preserve any properties set by content (e.g. density from ShuttleSystem).
            foreach (var oldId in chunk.Fixtures)
            {
                if (_newFixtures.TryGetValue(oldId, out var newFixture) &&
                    manager.Fixtures.TryGetValue(oldId, out var oldFixture) &&
                    oldFixture.Shape is PolygonShape oldPoly &&
                    newFixture.Shape is PolygonShape newPoly &&
                    oldPoly.EqualsApprox(newPoly))
                {
                    _newFixtures.Remove(oldId);
                    continue;
                }

                toRemove.Add(oldId);
            }

            foreach (var oldId in toRemove)
            {
                chunk.Fixtures.Remove(oldId);

                if (manager.Fixtures.TryGetValue(oldId, out var fixture))
                    _fixtures.DestroyFixture(uid, oldId, fixture, false, body: body, manager: manager, xform: xform);

                updated = true;
            }

            // Anything remaining is a new fixture (or at least, may have not serialized onto the chunk yet).
            foreach (var (id, fixture) in _newFixtures)
            {
                chunk.Fixtures.Add(id);

                var existingFixture = _fixtures.GetFixtureOrNull(uid, id, manager: manager);
                if (existingFixture?.Shape is PolygonShape poly &&
                    poly.EqualsApprox((PolygonShape) fixture.Shape))
                {
                    continue;
                }

                _fixtures.CreateFixture(uid, id, fixture, false, manager, body, xform);
                updated = true;
            }

            return updated;
        }

        private static Fixture CreateGridFixture(EntityUid uid, Box2 bounds)
        {
            Span<Vector2> vertices = stackalloc Vector2[4];
            vertices[0] = bounds.BottomLeft;
            vertices[1] = bounds.BottomRight;
            vertices[2] = bounds.TopRight;
            vertices[3] = bounds.TopLeft;

            var poly = new PolygonShape();
            poly.Set(vertices, 4);

#pragma warning disable CS0618
            return new Fixture(
                poly,
                MapGridHelpers.CollisionGroup,
                MapGridHelpers.CollisionGroup,
                true)
            {
                Owner = uid
            };
#pragma warning restore CS0618
        }
    }

    /// <summary>
    /// Event raised after a grids fixtures have changed, but before <see cref="FixtureSystem.FixtureUpdate"/> is called.
    /// Allows content to modify some fixture properties, like density.
    /// </summary>
    public sealed class GridFixtureChangeEvent : EntityEventArgs
    {
        public Dictionary<string, Fixture> NewFixtures { get; init; } = default!;
    }

    [Serializable, NetSerializable]
    public sealed class ChunkSplitDebugMessage : EntityEventArgs
    {
        public NetEntity Grid;
        public Dictionary<Vector2i, List<List<Vector2i>>> Nodes = new ();
        public List<(Vector2 Start, Vector2 End)> Connections = new();
    }

    /// <summary>
    /// Raised by a client who wants to receive gridsplitnode messages.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestGridNodesMessage : EntityEventArgs {}

    [Serializable, NetSerializable]
    public sealed class StopGridNodesMessage : EntityEventArgs {}
}
