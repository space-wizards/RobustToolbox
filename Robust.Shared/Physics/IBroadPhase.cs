using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics {

    public interface IBroadPhase : IBroadPhase<IPhysBody> {

    }

    public interface IBroadPhase<T> : ICollection<T> {

        int Capacity { get; set; }

        int Height {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        int MaxBalance {
            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
            get;
        }

        float AreaRatio {
            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
            get;
        }

        bool Add(in T item);

        bool Remove(in T item);

        bool Update(in T item);

        IEnumerable<T> Query(Box2 aabb, bool approx = false);

        IEnumerable<T> Query(Vector2 point, bool approx = false);

        bool Query(DynamicTree<T>.RayQueryCallbackDelegate callback, in Vector2 start, in Vector2 dir, bool approx = false);

        IEnumerable<(T A,T B)> GetCollisions(bool approx = false);

    }

}
