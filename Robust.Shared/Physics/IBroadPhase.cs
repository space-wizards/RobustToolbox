using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Shapes;

namespace Robust.Shared.Physics
{
    internal interface IBroadPhase
    {
        // Rolled SetProxy into AddProxy
        void UpdatePairs(BroadphaseDelegate callback);

        void AddProxy(FixtureProxy proxy);

        void RemoveProxy(FixtureProxy proxy);

        void MoveProxy(FixtureProxy proxy);

        // TODO: Okay so Box2D uses TouchProxy to say "hey this proxy is moving" to know which pairs to update.
        // The problem with this is if we're driving a station and we try to run over an entity then
        // none of the entities involved in the collision are moving in their own frame of reference
        // Thus it's probably better to just always use UpdatePairs on awake bodies given most of our bodies are sleeping
        // In other games sleeping is probably not too common so it's less advantageous for them.
        //void TouchProxy(FixtureProxy proxy);

        bool Contains(FixtureProxy proxy);

        // TODO: Query and Raycast
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
