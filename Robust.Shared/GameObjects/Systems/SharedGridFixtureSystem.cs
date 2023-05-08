using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    public abstract class SharedGridFixtureSystem : EntitySystem
    {
        [Dependency] private readonly FixtureSystem _fixtures = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        protected ISawmill Sawmill = default!;
        private bool _enabled;
        private float _fixtureEnlargement;

        internal const string ShowGridNodesCommand = "showgridnodes";

        public override void Initialize()
        {
            base.Initialize();
            UpdatesBefore.Add(typeof(SharedBroadphaseSystem));
            Sawmill = Logger.GetSawmill("physics");

            _cfg.OnValueChanged(CVars.GenerateGridFixtures, SetEnabled, true);
            _cfg.OnValueChanged(CVars.GridFixtureEnlargement, SetEnlargement, true);

            SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
        }

        protected virtual void OnGridInit(GridInitializeEvent ev)
        {
            if (HasComp<MapComponent>(ev.EntityUid))
                return;

            // This will also check for grid splits if applicable.
            var grid = Comp<MapGridComponent>(ev.EntityUid);
            grid.RegenerateCollision(grid.GetMapChunks().Values.ToHashSet());
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _cfg.UnsubValueChanged(CVars.GenerateGridFixtures, SetEnabled);
            _cfg.UnsubValueChanged(CVars.GridFixtureEnlargement, SetEnlargement);
        }

        private void SetEnabled(bool value) => _enabled = value;

        private void SetEnlargement(float value) => _fixtureEnlargement = value;

        internal void RegenerateCollision(
            EntityUid uid,
            Dictionary<MapChunk, List<Box2i>> mapChunks,
            List<MapChunk> removedChunks)
        {
            if (!_enabled) return;

            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? body))
            {
                Sawmill.Error($"Trying to regenerate collision for {uid} that doesn't have {nameof(body)}");
                return;
            }

            if (!EntityManager.TryGetComponent(uid, out FixturesComponent? manager))
            {
                Sawmill.Error($"Trying to regenerate collision for {uid} that doesn't have {nameof(manager)}");
                return;
            }

            if (!EntityManager.TryGetComponent(uid, out TransformComponent? xform))
            {
                Sawmill.Error($"Trying to regenerate collision for {uid} that doesn't have {nameof(TransformComponent)}");
                return;
            }

            var fixtures = new List<Fixture>(mapChunks.Count);

            foreach (var (chunk, rectangles) in mapChunks)
            {
                UpdateFixture(uid, chunk, rectangles, body, manager, xform);
                fixtures.AddRange(chunk.Fixtures);
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
            var newFixtures = new List<Fixture>();

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

                var newFixture = new Fixture(
                    $"grid_chunk-{bounds.Left}-{bounds.Bottom}",
                    poly,
                    MapGridHelpers.CollisionGroup,
                    MapGridHelpers.CollisionGroup,
                    true) { Body = body};

                newFixtures.Add(newFixture);
            }

            var toRemove = new RemQueue<Fixture>();
            // Check if we even need to issue an eventbus event
            var updated = false;

            foreach (var oldFixture in chunk.Fixtures)
            {
                var existing = false;

                // Handle deleted / updated fixtures
                // (TODO: Check IDs and cross-reference for updates?)
                for (var i = newFixtures.Count - 1; i >= 0; i--)
                {
                    var fixture = newFixtures[i];
                    if (!oldFixture.Equals(fixture)) continue;
                    existing = true;
                    newFixtures.RemoveSwap(i);
                    break;
                }

                // Doesn't align with any new fixtures so delete
                if (existing) continue;

                toRemove.Add(oldFixture);
            }

            foreach (var fixture in toRemove)
            {
                // TODO add a DestroyFixture() override that takes in a list.
                // reduced broadphase lookups
                chunk.Fixtures.Remove(fixture);
                _fixtures.DestroyFixture(uid, fixture, false, body: body, manager: manager, xform: xform);
            }

            if (newFixtures.Count > 0 || toRemove.List?.Count > 0)
            {
                updated = true;
            }

            // Anything remaining is a new fixture (or at least, may have not serialized onto the chunk yet).
            foreach (var fixture in newFixtures)
            {
                var existingFixture = _fixtures.GetFixtureOrNull(uid, fixture.ID, manager: manager);
                // Check if it's the same (otherwise remove anyway).
                if (existingFixture?.Shape is PolygonShape poly &&
                    poly.EqualsApprox((PolygonShape) fixture.Shape))
                {
                    chunk.Fixtures.Add(existingFixture);
                    continue;
                }

                chunk.Fixtures.Add(fixture);
                _fixtures.CreateFixture(uid, fixture, false, manager, body, xform);
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
        public List<Fixture> NewFixtures { get; init; } = default!;
    }

    [Serializable, NetSerializable]
    public sealed class ChunkSplitDebugMessage : EntityEventArgs
    {
        public EntityUid Grid;
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
