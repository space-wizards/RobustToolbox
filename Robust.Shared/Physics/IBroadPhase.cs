using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Shapes;

namespace Robust.Shared.Physics
{
    internal interface IBroadPhase
    {
        void UpdatePairs(PhysicsMapCallback.BroadphaseDelegate callback);

        bool TestOverlap(int proxyIdA, int proxyIdB);

        int AddFixture(ref AABB aabb);

        void RemoveProxy(int proxyId);

        void MoveProxy(int proxyId, ref AABB aabb, Vector2 displacement);

        void SetProxy(int proxyId, ref FixtureProxy proxy);

        FixtureProxy GetProxy(int proxyId);

        void TouchProxy(int proxyId);

        void Query(BroadPhaseQueryCallback callback, ref AABB aabb);

        void RayCast(BroadPhaseRayCastCallback callback, ref RayCastInput input);
    }

    public delegate bool BroadPhaseQueryCallback(int proxyId);

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
