/*
 * Initially based on Box2D by Erin Catto, license follows;
 *
 * Copyright (c) 2009 Erin Catto http://www.box2d.org
 *
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 1. The origin of this software must not be misrepresented; you must not
 * claim that you wrote the original software. If you use this software
 * in a product, an acknowledgment in the product documentation would be
 * appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 * misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.Maths;
using Proxy = Robust.Shared.Physics.DynamicTree.Proxy;

namespace Robust.Shared.Physics
{

    [PublicAPI]
    public abstract partial class DynamicTree
    {
        public const int MinimumCapacity = 16;

        protected const float AabbMultiplier = 2f;

        protected readonly float AabbExtendSize;

        protected readonly Func<int, int> GrowthFunc;

        protected DynamicTree(float aabbExtendSize, Func<int, int>? growthFunc)
        {
            AabbExtendSize = aabbExtendSize;
            GrowthFunc = growthFunc ?? DefaultGrowthFunc;
        }

        // box2d grows by *2, here we're being somewhat more linear
        private static int DefaultGrowthFunc(int x)
            => x + 256;

    }

    [PublicAPI]
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public sealed class DynamicTree<T>
        : IBroadPhase<T> where T : notnull {

        public delegate Box2 ExtractAabbDelegate(in T value);

        public delegate bool QueryCallbackDelegate(in T value);
        public delegate bool QueryCallbackDelegate<TState>(ref TState state, in T value);

        public delegate bool RayQueryCallbackDelegate(in T value, in Vector2 point, float distFromOrigin);
        public delegate bool RayQueryCallbackDelegate<TState>(ref TState state, in T value, in Vector2 point, float distFromOrigin);

        private readonly IEqualityComparer<T> _equalityComparer;

        private readonly ExtractAabbDelegate _extractAabb;

        // avoids "Collection was modified; enumeration operation may not execute."
        private Dictionary<T, Proxy> _nodeLookup;
        private readonly B2DynamicTree<T> _b2Tree;

        public DynamicTree(ExtractAabbDelegate extractAabbFunc, IEqualityComparer<T>? comparer = null, float aabbExtendSize = 1f / 32, int capacity = 256, Func<int, int>? growthFunc = null)
        {
            capacity = Math.Max(DynamicTree.MinimumCapacity, capacity);

            _extractAabb = extractAabbFunc;
            _equalityComparer = comparer ?? EqualityComparer<T>.Default;
            _nodeLookup = new Dictionary<T, Proxy>(_equalityComparer);
            _b2Tree = new B2DynamicTree<T>(aabbExtendSize, capacity, growthFunc);
        }

        public int Capacity => _b2Tree.Capacity;
        public int Height => _b2Tree.Height;
        public int MaxBalance => _b2Tree.MaxBalance;
        public float AreaRatio => _b2Tree.AreaRatio;

        public string DebuggerDisplay
            => $"Count = {Count}, Capacity = {Capacity}, Height = {Height}, NodeCount = {NodeCount}";

        public IEnumerator<T> GetEnumerator()
        {
            return _nodeLookup.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public void Clear()
        {
            foreach (var proxy in _nodeLookup.Values)
            {
                _b2Tree.DestroyProxy(proxy);
            }
        }

        public bool Contains(T item)
            => _nodeLookup.ContainsKey(item);

        public void CopyTo(T[] array, int arrayIndex)
            => _nodeLookup.Keys.CopyTo(array, arrayIndex);

        public int NodeCount { get; private set; }

        public int Count => _nodeLookup.Count;

        public bool IsReadOnly
            => false;

        void ICollection<T>.Add(T item)
            => Add(item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(in T item, Box2? aabb = null)
        {
            if (TryGetProxy(item, out var proxy))
            {
                return false;
            }

            var box = _extractAabb(item);

            if (CheckNaNs(box))
            {
                _nodeLookup[item] = Proxy.Free;
                return true;
            }

            proxy = _b2Tree.CreateProxy(box, item);
            _nodeLookup[item] = proxy;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetProxy(in T item, out Proxy proxy)
            => _nodeLookup.TryGetValue(item, out proxy);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Box2? GetNodeBounds(T item)
            => TryGetProxy(item, out var proxy) ? _b2Tree.GetFatAabb(proxy) : (Box2?) null;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Box2? GetNodeBounds(in T item)
            => TryGetProxy(item, out var proxy) ? _b2Tree.GetFatAabb(proxy) : (Box2?) null;


        public bool Remove(in T item)
        {
            if (!_nodeLookup.Remove(item, out var proxy))
            {
                return false;
            }

            if (proxy != Proxy.Free)
            {
                _b2Tree.DestroyProxy(proxy);
            }

            return true;
        }

        bool ICollection<T>.Remove(T item)
            => Remove(item);

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public bool Update(in T item, Box2? newBox = null)
        {
            if (!TryGetProxy(item, out var proxy))
            {
                return false;
            }

            newBox ??= _extractAabb(item);

            if (CheckNaNs(newBox.Value))
            {
                if (proxy == Proxy.Free)
                {
                    return false;
                }

                _b2Tree.DestroyProxy(proxy);
                _nodeLookup[item] = Proxy.Free;
                return true;
            }

            if (proxy == Proxy.Free)
            {
                _nodeLookup[item] = _b2Tree.CreateProxy(newBox.Value, item);
                return true;
            }

            return _b2Tree.MoveProxy(proxy, newBox.Value, Vector2.Zero);
        }

        public void QueryAabb(QueryCallbackDelegate callback, Box2 aabb, bool approx = false)
        {
            QueryAabb(ref callback, EasyQueryCallback, aabb, approx);
        }

        public void QueryAabb<TState>(ref TState state, QueryCallbackDelegate<TState> callback, Box2 aabb, bool approx = false)
        {
            var tuple = (state, _b2Tree, callback, aabb, approx, _extractAabb);
            _b2Tree.Query(ref tuple, DelegateCache<TState>.AabbQueryState, aabb);
            state = tuple.state;
        }

        public IEnumerable<T> QueryAabb(Box2 aabb, bool approx = false)
        {
            var list = new List<T>();

            QueryAabb(ref list, (ref List<T> lst, in T i) =>
            {
                lst.Add(i);
                return true;
            }, aabb, approx);

            return list;
        }

        public void QueryPoint(QueryCallbackDelegate callback, Vector2 point, bool approx = false)
        {
            QueryPoint(ref callback, EasyQueryCallback, point, approx);
        }

        public void QueryPoint<TState>(ref TState state, QueryCallbackDelegate<TState> callback, Vector2 point, bool approx = false)
        {
            var tuple = (state, _b2Tree, callback, point, approx, _extractAabb);
            _b2Tree.Query(ref tuple,
                (ref (TState state, B2DynamicTree<T> tree, QueryCallbackDelegate<TState> callback, Vector2 point, bool approx, ExtractAabbDelegate extract) tuple,
                    Proxy proxy) =>
                {
                    var item = tuple.tree.GetUserData(proxy)!;

                    if (!tuple.approx)
                    {
                        var precise = tuple.extract(item);
                        if (!precise.Contains(tuple.point))
                        {
                            return true;
                        }
                    }

                    return tuple.callback(ref tuple.state, item);
                }, Box2.CenteredAround(point, new Vector2(0.1f, 0.1f)));
            state = tuple.state;
        }

        public IEnumerable<T> QueryPoint(Vector2 point, bool approx = false)
        {
            var list = new List<T>();

            QueryPoint(ref list, (ref List<T> list, in T i) =>
            {
                list.Add(i);
                return true;
            }, point, approx);

            return list;
        }

        private static readonly QueryCallbackDelegate<QueryCallbackDelegate> EasyQueryCallback =
            (ref QueryCallbackDelegate s, in T v) => s(v);

        public void QueryRay<TState>(ref TState state, RayQueryCallbackDelegate<TState> callback, in Ray ray, bool approx = false)
        {
            var tuple = (state, callback, _b2Tree, approx ? null : _extractAabb, ray);
            _b2Tree.RayCast(ref tuple, DelegateCache<TState>.RayQueryState, ray);
            state = tuple.state;
        }

        private static bool AabbQueryStateCallback<TState>(ref (TState state, B2DynamicTree<T> tree, QueryCallbackDelegate<TState> callback, Box2 aabb, bool approx, ExtractAabbDelegate extract) tuple, Proxy proxy)
        {
            var item = tuple.tree.GetUserData(proxy)!;
            if (!tuple.approx)
            {
                var precise = tuple.extract(item);
                if (!precise.Intersects(tuple.aabb))
                {
                    return true;
                }
            }

            return tuple.callback(ref tuple.state, item);
        }

        private static bool RayQueryStateCallback<TState>(ref (TState state, RayQueryCallbackDelegate<TState> callback, B2DynamicTree<T> tree, ExtractAabbDelegate? extract, Ray srcRay) tuple, Proxy proxy, in Vector2 hitPos, float distance)
        {
            var item = tuple.tree.GetUserData(proxy)!;
            var hit = hitPos;

            if (tuple.extract != null)
            {
                var precise = tuple.extract(item);
                if (!tuple.srcRay.Intersects(precise, out distance, out hit))
                {
                    return true;
                }
            }

            return tuple.callback(ref tuple.state, item, hit, distance);
        }

        public void QueryRay(RayQueryCallbackDelegate callback, in Ray ray, bool approx = false)
        {
            QueryRay(ref callback, RayQueryDelegateCallbackInst, ray, approx);
        }

        private static readonly RayQueryCallbackDelegate<RayQueryCallbackDelegate> RayQueryDelegateCallbackInst = RayQueryDelegateCallback;

        private static bool RayQueryDelegateCallback(ref RayQueryCallbackDelegate state, in T value, in Vector2 point, float distFromOrigin)
        {
            return state(value, point, distFromOrigin);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AddOrUpdate(T item, Box2? newAABB = null) => Update(item, newAABB) || Add(item, newAABB);

        private static bool CheckNaNs(in Box2 box)
        {
            return float.IsNaN(box.Left)
                   || float.IsNaN(box.Top)
                   || float.IsNaN(box.Bottom)
                   || float.IsNaN(box.Right);
        }

        [Conditional("DEBUG_DYNAMIC_TREE")]
        [Conditional("DEBUG_DYNAMIC_TREE_ASSERTS")]
        [DebuggerNonUserCode]
        [DebuggerHidden]
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool assertion, [CallerMemberName] string? member = default,
            [CallerFilePath] string? file = default, [CallerLineNumber] int line = default)
        {
            if (assertion) return;

            var msg = $"Assertion failure in {member} ({file}:{line})";
            Debug.Print(msg);
            Debugger.Break();
            throw new InvalidOperationException(msg);
        }

        private static class DelegateCache<TState>
        {
            public static readonly
                B2DynamicTree<T>.QueryCallback<(TState state, B2DynamicTree<T> tree, QueryCallbackDelegate<TState> callback, Box2 aabb, bool approx, ExtractAabbDelegate extract)> AabbQueryState =
                    AabbQueryStateCallback;

            public static readonly
                B2DynamicTree<T>.RayQueryCallback<(TState state, RayQueryCallbackDelegate<TState> callback,
                    B2DynamicTree<T> tree, ExtractAabbDelegate? extract, Ray srcRay)> RayQueryState =
                    RayQueryStateCallback;
        }
    }
}
