using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Shared.Physics {

    public interface IBroadPhase
    {
        void UpdatePairs(BroadPhaseDelegate callback);

        bool TestOverlap(DynamicTree.Proxy proxyA, DynamicTree.Proxy proxyB);

        DynamicTree.Proxy AddProxy(ref FixtureProxy proxy);

        void MoveProxy(DynamicTree.Proxy proxyId, in Box2 aabb, Vector2 displacement);

        void RemoveProxy(DynamicTree.Proxy proxy);

        void TouchProxy(DynamicTree.Proxy proxy);

        void QueryAabb<TState>(
            ref TState state,
            DynamicTree<FixtureProxy>.QueryCallbackDelegate<TState> callback,
            Box2 aabb,
            bool approx = false);

        IEnumerable<FixtureProxy> QueryAabb(Box2 aabb, bool approx = false);

        void QueryPoint(DynamicTree<FixtureProxy>.QueryCallbackDelegate callback,
            Vector2 point,
            bool approx = false);

        void QueryPoint<TState>(
            ref TState state,
            DynamicTree<FixtureProxy>.QueryCallbackDelegate<TState> callback,
            Vector2 point,
            bool approx = false);

        IEnumerable<FixtureProxy> QueryPoint(Vector2 point, bool approx = false);

        void QueryRay(
            DynamicTree<FixtureProxy>.RayQueryCallbackDelegate callback,
            in Ray ray,
            bool approx = false);

        void QueryRay<TState>(
            ref TState state,
            DynamicTree<FixtureProxy>.RayQueryCallbackDelegate<TState> callback,
            in Ray ray,
            bool approx = false);
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

        bool Add(in T item, Box2? newAABB = null);

        bool Remove(in T item);

        bool Update(in T item, Box2? newAABB = null);

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
