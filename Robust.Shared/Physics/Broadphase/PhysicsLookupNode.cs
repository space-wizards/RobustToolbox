using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Broadphase
{
    internal sealed class PhysicsLookupNode
    {
        internal PhysicsLookupChunk ParentChunk { get; }

        internal Vector2i Indices { get; }

        internal IEnumerable<IPhysShape> PhysicsShapes
        {
            get
            {
                for (var i = 0; i < _entities.Count; i++)
                {
                    var comp = _entities[i];
                    if (comp.Deleted)
                        continue;

                    for (var j = 0; j < comp.PhysicsShapes.Count; j++)
                    {
                        var shape = comp.PhysicsShapes[j];

                        yield return shape;
                    }
                }
            }
        }

        public IEnumerable<IPhysBody> PhysicsComponents
        {
            get
            {
                for (var i = 0; i < _entities.Count; i++)
                {
                    var comp = _entities[i];
                    if (comp.Deleted)
                        continue;

                    yield return comp;
                }
            }
        }

        private readonly List<IPhysBody> _entities = new List<IPhysBody>();

        internal PhysicsLookupNode(PhysicsLookupChunk parentChunk, Vector2i indices)
        {
            ParentChunk = parentChunk;
            Indices = indices;
        }

        internal void AddPhysics(IPhysBody comp)
        {
            DebugTools.Assert(!_entities.Contains(comp));
            _entities.Add(comp);
        }

        internal void RemovePhysics(IPhysBody comp)
        {
            DebugTools.Assert(_entities.Contains(comp));
            _entities.Remove(comp);
        }
    }
}
