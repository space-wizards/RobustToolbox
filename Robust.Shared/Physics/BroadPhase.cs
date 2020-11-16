using System.Collections;
using System.Collections.Generic;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics {

    public sealed class BroadPhase : IBroadPhase {

        private readonly DynamicTree<IPhysBody> _tree;

        public BroadPhase() =>
            _tree = new DynamicTree<IPhysBody>(
                (in IPhysBody body) => body.WorldAABB,
                capacity: 3840,
                growthFunc: x => x + 256
            );

        public IEnumerator<IPhysBody> GetEnumerator() => _tree.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) _tree).GetEnumerator();

        void ICollection<IPhysBody>.Add(IPhysBody item) => _tree.Add(item);

        public void Clear() => _tree.Clear();

        public bool Contains(IPhysBody item) => _tree.Contains(item);

        public void CopyTo(IPhysBody[] array, int arrayIndex) => _tree.CopyTo(array, arrayIndex);

        public bool Remove(IPhysBody item) => _tree.Remove(item);

        public int Capacity => _tree.Capacity;

        public int Height => _tree.Height;

        public int MaxBalance => _tree.MaxBalance;

        public float AreaRatio => _tree.AreaRatio;

        public int Count => _tree.Count;

        public bool Add(in IPhysBody item) => _tree.Add(in item);

        public bool Remove(in IPhysBody item) => _tree.Remove(in item);

        public bool Update(in IPhysBody item) => _tree.Update(in item);

        public void QueryAabb(DynamicTree<IPhysBody>.QueryCallbackDelegate callback, Box2 aabb, bool approx = false)
        {
            _tree.QueryAabb(callback, aabb, approx);
        }

        public void QueryAabb<TState>(ref TState state, DynamicTree<IPhysBody>.QueryCallbackDelegate<TState> callback, Box2 aabb, bool approx = false)
        {
            _tree.QueryAabb(ref state, callback, aabb, approx);
        }

        public IEnumerable<IPhysBody> QueryAabb(Box2 aabb, bool approx = false)
        {
            return _tree.QueryAabb(aabb, approx);
        }

        public void QueryPoint(DynamicTree<IPhysBody>.QueryCallbackDelegate callback, Vector2 point,
            bool approx = false)
        {
            _tree.QueryPoint(callback, point, approx);
        }

        public void QueryPoint<TState>(ref TState state, DynamicTree<IPhysBody>.QueryCallbackDelegate<TState> callback,
            Vector2 point, bool approx = false)
        {
            _tree.QueryPoint(ref state, callback, point, approx);
        }

        public IEnumerable<IPhysBody> QueryPoint(Vector2 point, bool approx = false)
        {
            return _tree.QueryPoint(point, approx);
        }

        public void QueryRay(DynamicTree<IPhysBody>.RayQueryCallbackDelegate callback, in Ray ray, bool approx = false) =>
            _tree.QueryRay(callback, ray, approx);

        public void QueryRay<TState>(ref TState state, DynamicTree<IPhysBody>.RayQueryCallbackDelegate<TState> callback, in Ray ray,
            bool approx = false)
        {
            _tree.QueryRay(ref state, callback, ray, approx);
        }

        public bool IsReadOnly => _tree.IsReadOnly;


    }

}
