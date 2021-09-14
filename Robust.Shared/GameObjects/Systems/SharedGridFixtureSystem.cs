using System;
using System.Collections.Generic;
using System.Linq;
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
        [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;

        public override void Initialize()
        {
            base.Initialize();
            UpdatesBefore.Add(typeof(SharedBroadphaseSystem));
            SubscribeLocalEvent<RegenerateChunkCollisionEvent>(HandleCollisionRegenerate);
        }

        /// <summary>
        /// Queue the chunk to generate (if cooldown > 0) or immediately process it.
        /// </summary>
        /// <param name="ev"></param>
        private void HandleCollisionRegenerate(RegenerateChunkCollisionEvent ev)
        {
            RegenerateCollision(ev.Chunk);
        }

        public void ProcessGrid(GridId gridId)
        {
            var grid = (IMapGridInternal) _mapManager.GetGrid(gridId);

            // Just in case there's any deleted we'll ToArray
            foreach (var (_, chunk) in grid.GetMapChunks().ToArray())
            {
                chunk.RegenerateCollision();
            }
        }

        // TODO: Move to its own thing
        /// <summary>
        /// Decompose a <see cref="MapChunk"/> into as few rectangles as possible.
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns></returns>
        private List<Box2i> GetRectangles(MapChunk chunk)
        {
            // TODO: can skip tiles based on rectangles you fucking numpty so do that.
            var rectangles = new List<Box2i>();

            // TODO: Use the existing PartitionChunk version because that one is likely faster and you can Span that shit.
            // Convert each line into boxes as long as they can be.
            for (ushort y = 0; y < chunk.ChunkSize; y++)
            {
                var origin = 0;
                var running = false;

                for (ushort x = 0; x < chunk.ChunkSize; x++)
                {
                    if (!chunk.GetTile(x, y).IsEmpty)
                    {
                        running = true;
                        continue;
                    }

                    // Still empty
                    if (running)
                    {
                        rectangles.Add(new Box2i(origin, y, x, y + 1));
                    }

                    origin = x + 1;
                    running = false;
                }

                if (running)
                    rectangles.Add(new Box2i(origin, y, chunk.ChunkSize, y + 1));
            }

            // Patch them together as available
            for (var i = rectangles.Count - 1; i >= 0; i--)
            {
                var box = rectangles[i];
                for (var j = i - 1; j >= 0; j--)
                {
                    var other = rectangles[j];

                    // Gone down as far as we can go.
                    if (other.Top < box.Bottom) break;

                    if (box.Left == other.Left && box.Right == other.Right)
                    {
                        box = new Box2i(box.Left, other.Bottom, box.Right, box.Top);
                        rectangles[i] = box;
                        rectangles.RemoveAt(j);
                        i -= 1;
                        continue;
                    }

                    // TODO: See if we can at least patch some kind of polygon out of it to reduce fixture count.
                }
            }

            // TODO: Assert ValidTiles count is the same.
            // DebugTools.Assert(rectangles.Select(b => b.Area).Equals(chunk.Va));
            return rectangles;
        }

        private void RegenerateCollision(MapChunk chunk)
        {
            if (!_mapManager.TryGetGrid(chunk.GridId, out var grid) ||
                !EntityManager.TryGetEntity(grid.GridEntityId, out var gridEnt)) return;

            DebugTools.Assert(chunk.ValidTiles > 0);

            if (!gridEnt.TryGetComponent(out PhysicsComponent? physicsComponent))
            {
                Logger.ErrorS("physics", $"Trying to regenerate collision for {gridEnt} that doesn't have {nameof(physicsComponent)}");
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

            var oldFixtures = chunk.Fixtures.ToList();

            Span<Vector2> vertices = stackalloc Vector2[4];

            foreach (var rectangle in GetRectangles(chunk))
            {
                var bounds = rectangle.Translated(origin);
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

            foreach (var oldFixture in chunk.Fixtures.ToArray())
            {
                var existing = false;

                // Handle deleted / updated fixtures
                // (TODO: Check IDs and cross-reference for updates?)
                for (var i = newFixtures.Count - 1; i >= 0; i--)
                {
                    var fixture = newFixtures[i];
                    if (!oldFixture.Equals(fixture)) continue;
                    existing = true;
                    newFixtures.RemoveAt(i);
                    break;
                }

                // Doesn't align with any new fixtures so delete
                if (existing) continue;

                chunk.Fixtures.Remove(oldFixture);
                _broadphase.DestroyFixture(physicsComponent, oldFixture);
            }

            // Anything remaining is a new fixture (or at least, may have not serialized onto the chunk yet).
            foreach (var fixture in newFixtures)
            {
                var existingFixture = physicsComponent.GetFixture(fixture.ID);
                // Check if it's the same (otherwise remove anyway).
                if (existingFixture?.Shape is PolygonShape poly &&
                    poly.EqualsApprox((PolygonShape) fixture.Shape))
                {
                    chunk.Fixtures.Add(existingFixture);
                    continue;
                }

                chunk.Fixtures.Add(fixture);
                _broadphase.CreateFixture(physicsComponent, fixture);
            }

            EntityManager.EventBus.RaiseLocalEvent(gridEnt.Uid,new GridFixtureChangeEvent {OldFixtures = oldFixtures, NewFixtures = chunk.Fixtures});
        }
    }

    public sealed class GridFixtureChangeEvent : EntityEventArgs
    {
        public List<Fixture> OldFixtures { get; init; } = default!;
        public List<Fixture> NewFixtures { get; init; } = default!;
    }
}
