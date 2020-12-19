using System;
using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Shapes;

namespace Robust.Shared.Physics.Broadphase
{
    public class ChunkBroadphase : IBroadPhase
    {
        private const byte ChunkSize = PhysicsLookupChunk.ChunkSize;

        private Dictionary<Vector2i, PhysicsLookupChunk> _graph = new Dictionary<Vector2i, PhysicsLookupChunk>();

        private Dictionary<int, List<PhysicsLookupChunk>> _proxyChunks = new Dictionary<int, List<PhysicsLookupChunk>>();

        private int _proxyCount;

        private List<FixtureProxy> _data = new List<FixtureProxy>(16);

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

        public void UpdatePairs(PhysicsMapCallback.BroadphaseDelegate callback)
        {
            throw new NotImplementedException();
        }

        public bool TestOverlap(int proxyIdA, int proxyIdB)
        {
            throw new NotImplementedException();
        }

        public int AddProxy(ref Box2 aabb)
        {
            var chunks = GetChunks(aabb);

            // Allocating ids less important for chunks (and our B2DynamicTree is somewhat diff to aether's DynamicTree
            // which is why I just did a new broadphase, coz it seemed easier somehow).
            var proxyId = _proxyCount;
            _proxyChunks[proxyId] = chunks;
            // TODO: Get each chunk in range and add it to that
            _proxyCount++;
            if (_data.Capacity <= proxyId)
            {
                _data.Capacity *= 2;
            }

            // TODO: Is the id diff?
            return proxyId;
        }

        private List<PhysicsLookupChunk> GetChunks(Box2 aabb)
        {
            throw new NotImplementedException();
        }

        public void RemoveProxy(int proxyId)
        {
            if (!_proxyChunks.Remove(proxyId))
                throw new InvalidOperationException($"Removing proxyId {proxyId} when it isn't on the graph");

            _proxyCount--;
        }

        public void MoveProxy(int proxyId, ref Box2 aabb, Vector2 displacement)
        {
            // TODO: Use existing Box2
            //var chunks = GetChunks(aabb.)

            _pro
        }

        public void SetProxy(int proxyId, ref FixtureProxy proxy)
        {
            _data[proxyId] = proxy;
        }

        public FixtureProxy GetProxy(int proxyId)
        {
            throw new System.NotImplementedException();
        }

        public void TouchProxy(int proxyId)
        {
            throw new System.NotImplementedException();
        }

        public void Query(BroadPhaseQueryCallback callback, ref Box2 aabb)
        {
            throw new System.NotImplementedException();
        }

        public void RayCast(BroadPhaseRayCastCallback callback, ref RayCastInput input)
        {
            throw new System.NotImplementedException();
        }
    }
}
