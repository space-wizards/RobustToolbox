using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Shapes;

namespace Robust.Shared.Physics.Broadphase
{
    public class ChunkBroadphase : IBroadPhase
    {
        public MapId MapId { get; set; }

        public GridId GridId { get; set; }

        private const byte ChunkSize = PhysicsLookupChunk.ChunkSize;

        private Dictionary<Vector2i, PhysicsLookupChunk> _graph = new Dictionary<Vector2i, PhysicsLookupChunk>();

        private Dictionary<FixtureProxy, List<PhysicsLookupChunk>> _proxyChunks = new Dictionary<FixtureProxy, List<PhysicsLookupChunk>>();

        private PhysicsLookupChunk GetOrCreateChunk(Vector2i origin)
        {
            var chunkIndices = GetChunkIndices(origin);

            if (!_graph.TryGetValue(chunkIndices, out var chunk))
            {
                chunk = new PhysicsLookupChunk(chunkIndices);
                _graph[chunkIndices] = chunk;
            }

            return chunk;
        }

        private static Vector2i GetChunkIndices(Vector2i indices)
        {
            return new Vector2i(
                (int) (Math.Floor((float) indices.X / ChunkSize) * ChunkSize),
                (int) (Math.Floor((float) indices.Y / ChunkSize) * ChunkSize));
        }

        public void UpdatePairs(BroadphaseDelegate callback)
        {
            // TODO: Go through each awake body on our grid and get our neighbors

            foreach (var body in EntitySystem.Get<SharedBroadPhaseSystem>().GetAwakeBodies(MapId, GridId))
            {

            }
        }

        public void AddProxy(FixtureProxy proxy)
        {
            var chunks = GetChunks(proxy.AABB);
            _proxyChunks[proxy] = chunks;
        }

        private List<PhysicsLookupChunk> GetChunks(Box2 aabb)
        {
            var inRange = new List<PhysicsLookupChunk>();
            var range = (aabb.BottomLeft - aabb.Center).Length;

            // This is the max in any direction that we can get a chunk (e.g. max 2 chunks away of data).
            var (maxXDiff, maxYDiff) = ((int) (range / ChunkSize) + 1, (int) (range / ChunkSize) + 1);

            var entityTile = new Vector2i((int) Math.Floor(aabb.Center.X), (int) Math.Floor(aabb.Center.Y));

            for (var x = -maxXDiff; x <= maxXDiff; x++)
            {
                for (var y = -maxYDiff; y <= maxYDiff; y++)
                {
                    var chunkIndices = GetChunkIndices(new Vector2i(entityTile.X + x * ChunkSize, entityTile.Y + y * ChunkSize));

                    if (!_graph.TryGetValue(chunkIndices, out var chunk)) continue;

                    // Now we'll check if it's in range and relevant for us
                    // (e.g. if we're on the very edge of a chunk we may need more chunks).

                    var (xDiff, yDiff) = (chunkIndices.X - entityTile.X, chunkIndices.Y - entityTile.Y);
                    if (xDiff > 0 && xDiff > range ||
                        yDiff > 0 && yDiff > range ||
                        xDiff < 0 && Math.Abs(xDiff + ChunkSize) > range ||
                        yDiff < 0 && Math.Abs(yDiff + ChunkSize) > range) continue;

                    inRange.Add(chunk);
                }
            }


            return inRange;
        }

        public void RemoveProxy(FixtureProxy proxy)
        {
            if (!_proxyChunks.Remove(proxy))
                throw new InvalidOperationException();
        }

        public void MoveProxy(FixtureProxy proxy)
        {
            throw new NotImplementedException();
        }

        public void MoveProxy(FixtureProxy proxy, Vector2 displacement)
        {
            var newAABB = proxy.AABB.Translated(displacement);
            var chunks = GetChunks(newAABB);
            var oldChunks = _proxyChunks[proxy].Where(o => !chunks.Contains(o));

            // Remove from old chunks
            foreach (var chunk in oldChunks)
            {
                chunk.RemoveProxy(proxy);

                if (chunk.CanDeleteChunk())
                    _graph.Remove(chunk.Origin);
            }

            foreach (var chunk in chunks)
            {
                // Update existing
                if (_proxyChunks[proxy].Contains(chunk))
                {
                    chunk.RemoveProxy(proxy);
                    chunk.AddProxy(proxy);
                }
                // Add new chunk
                else
                {
                    chunk.AddProxy(proxy);
                }
            }
        }

        public void TouchProxy(FixtureProxy proxy)
        {
            throw new NotImplementedException();
        }

        public bool Contains(FixtureProxy proxy)
        {
            throw new NotImplementedException();
        }

        public void Query(BroadPhaseQueryCallback callback, ref Box2 aabb)
        {
            // TODO: Query an aabb for overlapping proxies

            throw new NotImplementedException();
        }

        public void RayCast(BroadPhaseRayCastCallback callback, ref RayCastInput input)
        {
            throw new NotImplementedException();
        }
    }
}
