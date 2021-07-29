using System.Collections.Generic;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Server.Physics
{
    /// <summary>
    /// Handles generating fixtures for MapGrids.
    /// </summary>
    internal sealed class GridFixtureSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;

        // Is delaying fixture updates a good idea? IDEK. We definitely can't do them on every tile changed
        // because if someone changes 50 tiles that will kill perf. We could probably just run it every Update
        // (and at specific times due to race condition stuff).
        // At any rate, cooldown given here if someone wants it. CD of 0 just runs it every tick.
        private float _cooldown;
        private float _accumulator;

        private HashSet<MapChunk> _queuedChunks = new();

        public override void Initialize()
        {
            base.Initialize();
            UpdatesBefore.Add(typeof(PhysicsSystem));
            SubscribeLocalEvent<RegenerateChunkCollisionEvent>(HandleCollisionRegenerate);

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CVars.GridFixtureUpdateRate, value => _cooldown = value, true);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _accumulator += frameTime;
            if (_accumulator < _cooldown) return;

            _accumulator -= _cooldown;
            Process();
        }

        /// <summary>
        /// Go through every dirty chunk and re-generate their fixtures.
        /// </summary>
        public void Process()
        {
            foreach (var chunk in _queuedChunks)
            {
                RegenerateCollision(chunk);
            }

            _queuedChunks.Clear();
        }

        /// <summary>
        /// Queue the chunk to generate (if cooldown > 0) or immediately process it.
        /// </summary>
        /// <param name="ev"></param>
        private void HandleCollisionRegenerate(RegenerateChunkCollisionEvent ev)
        {
            if (_cooldown <= 0f)
            {
                RegenerateCollision(ev.Chunk);
                return;
            }

            _queuedChunks.Add(ev.Chunk);
        }

        private void RegenerateCollision(MapChunk chunk)
        {
            // Currently this is gonna be hella simple.
            if (!_mapManager.TryGetGrid(chunk.GridId, out var grid) ||
                !EntityManager.TryGetEntity(grid.GridEntityId, out var gridEnt) ||
                !gridEnt.TryGetComponent(out PhysicsComponent? physicsComponent)) return;

            // TODO: Lots of stuff here etc etc, make changes to mapgridchunk.
            var bounds = chunk.CalcLocalBounds();

            // So something goes on with the chunk's internal bounds caching where if there's no data the bound is 0 or something?
            if (bounds.IsEmpty()) return;

            var origin = chunk.Indices * chunk.ChunkSize;
            bounds = bounds.Translated(origin);

            // So we store a reference to the fixture on the chunk because it's easier to cross-reference it.
            // This is because when we get multiple fixtures per chunk there's no easy way to tell which the old one
            // corresponds with.
            // We also ideally want to avoid re-creating the fixture every time a tile changes and pushing that data
            // to the client hence we diff it.

            // Additionally, we need to handle map deserialization where content may have stored its own data
            // on the grid (e.g. mass) which we want to preserve.
            var oldFixture = chunk.Fixture;

            var newFixture = new Fixture(
                new PolygonShape
                {
                    Vertices = new List<Vector2>
                    {
                        bounds.BottomRight,
                        bounds.TopRight,
                        bounds.TopLeft,
                        bounds.BottomLeft,
                    }
                },
                MapGridHelpers.CollisionGroup,
                MapGridHelpers.CollisionGroup,
                true) {ID = $"grid_chunk-{chunk.Indices.X}-{chunk.Indices.Y}",
                Body = physicsComponent};

            // Check if we have an existing fixture on MapGrid
            var existingFixture = physicsComponent.GetFixture(newFixture.ID);
            var same = true;

            // Some fucky shit but we gotta handle map deserialization.
            if (existingFixture is {Shape: PolygonShape poly})
            {
                var newPoly = (PolygonShape) newFixture.Shape;

                if (newPoly.Vertices.Count == poly.Vertices.Count)
                {
                    for (var i = 0; i < poly.Vertices.Count; i++)
                    {
                        if (!poly.Vertices[i].EqualsApprox(newPoly.Vertices[i]))
                        {
                            same = false;
                            break;
                        }
                    }
                }
                else
                {
                    same = false;
                }
            }
            else
            {
                same = false;
            }

            // TODO: Chunk will likely need multiple fixtures but future sloth problem lmao idiot
            if (same)
            {
                // If we're deserializing map this can occur so just update it.
                if (oldFixture == null && existingFixture != null)
                {
                    chunk.Fixture = existingFixture;
                    existingFixture.CollisionMask = MapGridHelpers.CollisionGroup;
                    existingFixture.CollisionLayer = MapGridHelpers.CollisionGroup;
                }

                return;
            }

            if (oldFixture != null)
                _broadphase.DestroyFixture(physicsComponent, oldFixture);

            _broadphase.CreateFixture(physicsComponent, newFixture);
            chunk.Fixture = newFixture;

            EntityManager.EventBus.RaiseLocalEvent(gridEnt.Uid,new GridFixtureChangeEvent {OldFixture = oldFixture, NewFixture = newFixture});
        }
    }

    public sealed class GridFixtureChangeEvent : EntityEventArgs
    {
        public Fixture? OldFixture { get; init; }
        public Fixture? NewFixture { get; init; }
    }
}
