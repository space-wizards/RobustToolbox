using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics {

    public interface IBroadPhase : IBroadPhase<IPhysBody> {

    }

    public interface IBroadPhase<T> : ICollection<T> where T : notnull {

        int Capacity { get; }

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

        void QueryAabb(
            DynamicTree<T>.QueryCallbackDelegate callback,
            Box2 aabb,
            bool approx = false);

        void QueryAabb<TState>(
            ref TState state,
            DynamicTree<T>.QueryCallbackDelegate<TState> callback,
            Box2 aabb,
            bool approx = false);

        IEnumerable<T> QueryAabb(Box2 aabb, bool approx = false);

        void QueryPoint(DynamicTree<T>.QueryCallbackDelegate callback,
            Vector2 point,
            bool approx = false);

        void QueryPoint<TState>(
            ref TState state,
            DynamicTree<T>.QueryCallbackDelegate<TState> callback,
            Vector2 point,
            bool approx = false);

        IEnumerable<T> QueryPoint(Vector2 point, bool approx = false);

        void QueryRay(
            DynamicTree<T>.RayQueryCallbackDelegate callback,
            in Ray ray,
            bool approx = false);

        void QueryRay<TState>(
            ref TState state,
            DynamicTree<T>.RayQueryCallbackDelegate<TState> callback,
            in Ray ray,
            bool approx = false);
    }
}
