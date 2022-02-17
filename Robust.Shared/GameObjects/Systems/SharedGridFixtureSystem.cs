using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    public abstract class SharedGridFixtureSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly FixtureSystem _fixtures = default!;

        private bool _enabled;

        private float _fixtureEnlargement;

        public override void Initialize()
        {
            base.Initialize();
            UpdatesBefore.Add(typeof(SharedBroadphaseSystem));

            var configManager = IoCManager.Resolve<IConfigurationManager>();

            configManager.OnValueChanged(CVars.GenerateGridFixtures, SetEnabled, true);
            configManager.OnValueChanged(CVars.GridFixtureEnlargement, SetEnlargement, true);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            var configManager = IoCManager.Resolve<IConfigurationManager>();

            configManager.UnsubValueChanged(CVars.GenerateGridFixtures, SetEnabled);
            configManager.UnsubValueChanged(CVars.GridFixtureEnlargement, SetEnlargement);
        }

        private void SetEnabled(bool value) => _enabled = value;

        private void SetEnlargement(float value) => _fixtureEnlargement = value;

        internal void ProcessGrid(IMapGridInternal gridInternal)
        {
            // Just in case there's any deleted we'll ToArray
            foreach (var (_, chunk) in gridInternal.GetMapChunks().ToArray())
            {
                gridInternal.RegenerateCollision(chunk);
            }
        }

        internal void RegenerateCollision(EntityUid gridEuid, MapChunk chunk, List<Box2i> rectangles)
        {
            if (!_enabled) return;

            DebugTools.Assert(chunk.FilledTiles > 0);

            if (!EntityManager.TryGetComponent(gridEuid, out PhysicsComponent? physicsComponent))
            {
                Logger.ErrorS("physics", $"Trying to regenerate collision for {gridEuid} that doesn't have {nameof(physicsComponent)}");
                return;
            }

            if (!EntityManager.TryGetComponent(gridEuid, out FixturesComponent? fixturesComponent))
            {
                Logger.ErrorS("physics", $"Trying to regenerate collision for {gridEuid} that doesn't have {nameof(fixturesComponent)}");
                return;
            }

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

                poly.SetVertices(vertices);

                var newFixture = new Fixture(
                    poly,
                    MapGridHelpers.CollisionGroup,
                    MapGridHelpers.CollisionGroup,
                    true) {ID = $"grid_chunk-{bounds.Left}-{bounds.Bottom}",
                    Body = physicsComponent};

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
                chunk.Fixtures.Remove(fixture);
                _fixtures.DestroyFixture(fixture, false, fixturesComponent);
            }

            if (newFixtures.Count > 0 || toRemove.List?.Count > 0)
            {
                updated = true;
            }

            // Anything remaining is a new fixture (or at least, may have not serialized onto the chunk yet).
            foreach (var fixture in newFixtures)
            {
                var existingFixture = _fixtures.GetFixtureOrNull(physicsComponent, fixture.ID);
                // Check if it's the same (otherwise remove anyway).
                if (existingFixture?.Shape is PolygonShape poly &&
                    poly.EqualsApprox((PolygonShape) fixture.Shape))
                {
                    chunk.Fixtures.Add(existingFixture);
                    continue;
                }

                chunk.Fixtures.Add(fixture);
                _fixtures.CreateFixture(physicsComponent, fixture, false, fixturesComponent);
            }

            if (updated)
            {
                _fixtures.FixtureUpdate(fixturesComponent, physicsComponent);
                EntityManager.EventBus.RaiseLocalEvent(gridEuid,new GridFixtureChangeEvent {NewFixtures = chunk.Fixtures});
            }
        }
    }

    public sealed class GridFixtureChangeEvent : EntityEventArgs
    {
        public List<Fixture> NewFixtures { get; init; } = default!;
    }
}
