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
    public abstract class SharedGridFixtureSystem : EntitySystem
    {
        [Dependency] private readonly FixtureSystem _fixtures = default!;
        [Dependency] private readonly SharedMapSystem _map = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private bool _enabled;
        private float _fixtureEnlargement;

        internal const string ShowGridNodesCommand = "showgridnodes";

        public override void Initialize()
        {
            base.Initialize();
            UpdatesBefore.Add(typeof(SharedBroadphaseSystem));

            Subs.CVar(_cfg, CVars.GenerateGridFixtures, SetEnabled, true);
            Subs.CVar(_cfg, CVars.GridFixtureEnlargement, SetEnlargement, true);

            SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
            SubscribeLocalEvent<RegenerateGridBoundsEvent>(OnGridBoundsRegenerate);
        }

        private void OnGridBoundsRegenerate(ref RegenerateGridBoundsEvent ev)
        {
            RegenerateCollision(ev.Entity, ev.ChunkRectangles, ev.RemovedChunks);
        }

        protected virtual void OnGridInit(GridInitializeEvent ev)
        {
            if (HasComp<MapComponent>(ev.EntityUid))
                return;

            // This will also check for grid splits if applicable.
            var grid = Comp<MapGridComponent>(ev.EntityUid);
            _map.RegenerateCollision(ev.EntityUid, grid, _map.GetMapChunks(ev.EntityUid, grid).Values.ToHashSet());
        }

        private void SetEnabled(bool value) => _enabled = value;

        private void SetEnlargement(float value) => _fixtureEnlargement = value;

        internal void RegenerateCollision(
            EntityUid uid,
            Dictionary<MapChunk, List<Box2i>> mapChunks,
            List<MapChunk> removedChunks)
        {
            if (!_enabled)
                return;

            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? body))
            {
                Log.Error($"Trying to regenerate collision for {uid} that doesn't have {nameof(body)}");
                return;
            }

            if (!EntityManager.TryGetComponent(uid, out FixturesComponent? manager))
            {
                Log.Error($"Trying to regenerate collision for {uid} that doesn't have {nameof(manager)}");
                return;
            }

            if (!EntityManager.TryGetComponent(uid, out TransformComponent? xform))
            {
                Log.Error($"Trying to regenerate collision for {uid} that doesn't have {nameof(TransformComponent)}");
                return;
            }

            var fixtures = new Dictionary<string, Fixture>(mapChunks.Count);

            foreach (var (chunk, rectangles) in mapChunks)
            {
                UpdateFixture(uid, chunk, rectangles, body, manager, xform);

                foreach (var id in chunk.Fixtures)
                {
                    fixtures[id] = manager.Fixtures[id];
                }
            }

            EntityManager.EventBus.RaiseLocalEvent(uid,new GridFixtureChangeEvent {NewFixtures = fixtures}, true);
            _fixtures.FixtureUpdate(uid, manager: manager, body: body);

            CheckSplit(uid, mapChunks, removedChunks);
        }

        internal virtual void CheckSplit(EntityUid gridEuid, Dictionary<MapChunk, List<Box2i>> mapChunks,
            List<MapChunk> removedChunks) {}

        internal virtual void CheckSplit(EntityUid gridEuid, MapChunk chunk, List<Box2i> rectangles) {}

        private bool UpdateFixture(EntityUid uid, MapChunk chunk, List<Box2i> rectangles, PhysicsComponent body, FixturesComponent manager, TransformComponent xform)
        {
            var origin = chunk.Indices * chunk.ChunkSize;

            // So we store a reference to the fixture on the chunk because it's easier to cross-reference it.
            // This is because when we get multiple fixtures per chunk there's no easy way to tell which the old one
            // corresponds with.
            // We also ideally want to avoid re-creating the fixture every time a tile changes and pushing that data
            // to the client hence we diff it.

            // Additionally, we need to handle map deserialization where content may have stored its own data
            // on the grid (e.g. mass) which we want to preserve.
            var newFixtures = new ValueList<(string Id, Fixture Fixture)>();

            Span<Vector2> vertices = stackalloc Vector2[4];

            foreach (var rectangle in rectangles)
            {
                var bounds = ((Box2) rectangle.Translated(origin)).Enlarged(_fixtureEnlargement);
                var poly = new PolygonShape();

                vertices[0] = bounds.BottomLeft;
                vertices[1] = bounds.BottomRight;
                vertices[2] = bounds.TopRight;
                vertices[3] = bounds.TopLeft;

                poly.Set(vertices, 4);

#pragma warning disable CS0618
                var newFixture = new Fixture(
                    poly,
                    MapGridHelpers.CollisionGroup,
                    MapGridHelpers.CollisionGroup,
                    true)
                {
                    Owner = uid
                };
#pragma warning restore CS0618

                var key = string.Create(CultureInfo.InvariantCulture, $"grid_chunk-{bounds.Left}-{bounds.Bottom}");
                newFixtures.Add((key, newFixture));
            }

            // Check if we even need to issue an eventbus event
            var updated = false;

            foreach (var oldId in chunk.Fixtures)
            {
                var oldFixture = manager.Fixtures[oldId];
                var existing = false;

                // Handle deleted / updated fixtures
                // (TODO: Check IDs and cross-reference for updates?)
                for (var i = newFixtures.Count - 1; i >= 0; i--)
                {
                    var fixture = newFixtures[i].Fixture;

                    // TODO GRIDS
                    // Fix this
                    // This **only** works if we assume the density is always the default (PhysicsConstants.DefaultDensity).
                    // Hence, this always fails in SS14 because ShuttleSystem.OnGridFixtureChange changes the density.
                    // So it constantly creats & destroys fixtures unnecessarily
                    // AAAAA
                    if (!oldFixture.Equals(fixture))
                        continue;

                    existing = true;
                    newFixtures.RemoveSwap(i);
                    break;
                }

                if (existing)
                    continue;

                // Doesn't align with any new fixtures so delete
                chunk.Fixtures.Remove(oldId);
                _fixtures.DestroyFixture(uid, oldId, oldFixture, false, body: body, manager: manager, xform: xform);
                updated = true;
            }

            if (newFixtures.Count > 0)
            {
                updated = true;
            }

            // Anything remaining is a new fixture (or at least, may have not serialized onto the chunk yet).
            foreach (var (id, fixture) in newFixtures.Span)
            {
                chunk.Fixtures.Add(id);
                var existingFixture = _fixtures.GetFixtureOrNull(uid, id, manager: manager);
                // Check if it's the same (otherwise remove anyway).
                // TODO GRIDS
                // wasn't this already checked?
                if (existingFixture?.Shape is PolygonShape poly &&
                    poly.EqualsApprox((PolygonShape) fixture.Shape))
                {
                    continue;
                }

                _fixtures.CreateFixture(uid, id, fixture, false, manager, body, xform);
            }

            return updated;
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
