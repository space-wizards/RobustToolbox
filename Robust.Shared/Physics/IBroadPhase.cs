using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Shapes;

namespace Robust.Shared.Physics
{
    public interface IBroadPhase
    {
        // Rolled SetProxy into AddProxy
        void UpdatePairs(BroadphaseDelegate callback);

        bool TestOverlap(DynamicTree.Proxy proxyIdA, DynamicTree.Proxy proxyIdB);

        DynamicTree.Proxy AddProxy(FixtureProxy proxy);

        void RemoveProxy(DynamicTree.Proxy proxy);

        void MoveProxy(DynamicTree.Proxy proxy, ref Box2 aabb, Vector2 displacement);

        FixtureProxy GetProxy(DynamicTree.Proxy proxy);

        // TODO: Okay so Box2D uses TouchProxy to say "hey this proxy is moving" to know which pairs to update.
        // The problem with this is if we're driving a station and we try to run over an entity then
        // none of the entities involved in the collision are moving in their own frame of reference
        // Thus it's probably better to just always use UpdatePairs on awake bodies given most of our bodies are sleeping
        // In other games sleeping is probably not too common so it's less advantageous for them.
        //void TouchProxy(FixtureProxy proxy);

        void QueryAabb(
            DynamicTree<FixtureProxy>.QueryCallbackDelegate callback,
            Box2 aabb,
            bool approx = false);

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

        void ShiftOrigin(Vector2 newOrigin);
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
