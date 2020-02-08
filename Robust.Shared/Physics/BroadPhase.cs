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

        public int Capacity {
            get => _tree.Capacity;
            set => _tree.Capacity = value;
        }

        public int Height => _tree.Height;

        public int MaxBalance => _tree.MaxBalance;

        public float AreaRatio => _tree.AreaRatio;

        public int Count => _tree.Count;

        public bool Add(in IPhysBody item) => _tree.Add(in item);

        public bool Remove(in IPhysBody item) => _tree.Remove(in item);

        public bool Update(in IPhysBody item) => _tree.Update(in item);

        public IEnumerable<IPhysBody> Query(Box2 aabb, bool approx = false) => _tree.Query(aabb, approx);

        public IEnumerable<IPhysBody> Query(Vector2 point, bool approx = false) => _tree.Query(point, approx);

        public bool Query(DynamicTree<IPhysBody>.RayQueryCallbackDelegate callback, in Vector2 start, in Vector2 dir, bool approx = false) =>
            _tree.Query(callback, in start, in dir, approx);

        public IEnumerable<(IPhysBody A, IPhysBody B)> GetCollisions(bool approx = false) =>
            _tree.GetCollisions(approx);

        public bool IsReadOnly => _tree.IsReadOnly;


    }

}
