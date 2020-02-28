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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{

    [PublicAPI]
    public abstract partial class DynamicTree
    {

        public const int MinimumCapacity = 16;

        protected const float AabbMultiplier = 2f;

        protected readonly float AabbExtendSize;

        protected readonly Func<int, int> GrowthFunc;

        protected DynamicTree(float aabbExtendSize, Func<int, int> growthFunc)
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
    public sealed partial class DynamicTree<T>
        : DynamicTree, IBroadPhase<T> {

        public delegate Box2 ExtractAabbDelegate(in T value);

        public delegate bool QueryCallbackDelegate(ref T value);

        public delegate bool RayQueryCallbackDelegate(ref T value, in Vector2 point, float distFromOrigin);

        private readonly IEqualityComparer<T> _equalityComparer;

        private readonly ExtractAabbDelegate _extractAabb;

        private Proxy _freeNodes;

        // avoids "Collection was modified; enumeration operation may not execute."
        private IDictionary<T, Proxy> _nodeLookup;

        private Node[] _nodes;

        private Proxy _root;

        public DynamicTree(ExtractAabbDelegate extractAabbFunc, IEqualityComparer<T> comparer = null, float aabbExtendSize = 1f / 32, int capacity = 256, Func<int, int> growthFunc = null)
            : base(aabbExtendSize, growthFunc)
        {
            _extractAabb = extractAabbFunc;
            _equalityComparer = comparer ?? EqualityComparer<T>.Default;
            capacity = Math.Max(MinimumCapacity, capacity);

            _root = Proxy.Free;

            _nodeLookup = new Dictionary<T, Proxy>();
            _nodes = new Node[capacity];

            var l = Capacity - 1;
            for (var i = 0; i < l; ++i)
            {
                ref var node = ref _nodes[i];
                node.Parent = (Proxy) (i + 1);
                node.Height = -1;
            }

            ref var lastNode = ref _nodes[l];

            lastNode.Parent = Proxy.Free;
            lastNode.Height = -1;
        }

        public int Capacity
        {
            get => _nodes.Length;
            set => EnsureCapacity(value);
        }

        public int Height
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _root == Proxy.Free ? 0 : _nodes[_root].Height;
        }

        public int MaxBalance
        {
            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
            get
            {
                var maxBal = 0;

                for (var i = 0; i < Capacity; ++i)
                {
                    ref var node = ref _nodes[i];
                    if (node.Height <= 1)
                    {
                        continue;
                    }

                    ref var child1Node = ref _nodes[node.Child1];
                    ref var child2Node = ref _nodes[node.Child2];

                    var bal = Math.Abs(child2Node.Height - child1Node.Height);
                    maxBal = Math.Max(maxBal, bal);
                }

                return maxBal;
            }
        }

        public float AreaRatio
        {
            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
            get
            {
                if (_root == Proxy.Free)
                {
                    return 0;
                }

                ref var rootNode = ref _nodes[_root];
                var rootPeri = Box2.Perimeter(rootNode.Aabb);

                var totalPeri = 0f;

                for (var i = 0; i < Capacity; ++i)
                {
                    ref var node = ref _nodes[i];
                    if (node.Height < 0)
                    {
                        continue;
                    }

                    totalPeri += Box2.Perimeter(node.Aabb);
                }

                return totalPeri / rootPeri;
            }
        }

        public string DebuggerDisplay
            => $"Count = {Count}, Capacity = {Capacity}, Height = {Height}, NodeCount = {NodeCount}";

        private IEnumerable<(Proxy, Node)> DebugAllocatedNodesEnumerable
        {
            get
            {
                for (var i = 0; i < _nodes.Length; i++)
                {
                    var node = _nodes[i];
                    if (!node.IsFree)
                    {
                        yield return ((Proxy) i, node);
                    }
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private (Proxy, Node)[] DebugAllocatedNodes
        {
            get
            {
                var data = new (Proxy, Node)[NodeCount];
                var i = 0;
                foreach (var x in DebugAllocatedNodesEnumerable)
                {
                    data[i++] = x;
                }

                return data;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            var stack = new Stack<Proxy>(256);

            stack.Push(_root);

            while (stack.Count > 0)
            {
                var proxy = stack.Pop();

                if (proxy == Proxy.Free)
                {
                    continue;
                }

                // note: non-ref stack local copy here
                var node = _nodes[proxy];

                if (!node.IsLeaf)
                {
                    if (node.Child1 != Proxy.Free)
                    {
                        stack.Push(node.Child1);
                    }

                    if (node.Child2 != Proxy.Free)
                    {
                        stack.Push(node.Child2);
                    }

                    continue;
                }

                var item = node.Item;

                yield return node.Item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public void Clear()
        {
            var capacity = Capacity;

            NodeCount = 0;
            _nodes = new Node[capacity];
            _root = Proxy.Free;

            _nodeLookup = new Dictionary<T, Proxy>();
            _nodes = new Node[capacity];

            var l = Capacity - 1;
            for (var i = 0; i < l; ++i)
            {
                ref var node = ref _nodes[i];
                node.Parent = (Proxy) (i + 1);
                node.Height = -1;
            }

            ref var lastNode = ref _nodes[l];

            lastNode.Parent = Proxy.Free;
            lastNode.Height = -1;
        }

        public bool Contains(T item)
            => item != null && _nodeLookup.ContainsKey(item);

        public void CopyTo(T[] array, int arrayIndex)
            => _nodeLookup.Keys.CopyTo(array, arrayIndex);

        public int NodeCount { get; private set; }

        public int Count => _nodeLookup.Count;

        public bool IsReadOnly
            => false;

        void ICollection<T>.Add(T item)
            => Add(item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(in T item)
        {
            if (TryGetProxy(item, out var proxy))
            {
                return false;
            }

            var box = _extractAabb(item);

            proxy = AllocateNode();

            ref var node = ref _nodes[proxy];
            node.Aabb = new Box2(
                box.Left - AabbExtendSize,
                box.Bottom - AabbExtendSize,
                box.Right + AabbExtendSize,
                box.Top + AabbExtendSize
            );
            node.Item = item;

            InsertLeaf(proxy);

            _nodeLookup[item] = proxy;

            Assert(Contains(item));

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetProxy(in T item, out Proxy proxy)
            => _nodeLookup.TryGetValue(item, out proxy);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Box2? GetNodeBounds(T item)
            => TryGetProxy(item, out var proxy) ? _nodes[proxy].Aabb : (Box2?) null;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Box2? GetNodeBounds(in T item)
            => TryGetProxy(item, out var proxy) ? _nodes[proxy].Aabb : (Box2?) null;


        public bool Remove(in T item)
        {
            if (!_nodeLookup.Remove(item, out var proxy))
            {
                return false;
            }

            DestroyProxy(proxy);
            return true;

        }

        bool ICollection<T>.Remove(T item)
            => Remove(item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DestroyProxy(Proxy proxy)
        {
            RemoveLeaf(proxy);
            FreeNode(proxy);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public bool Update(in T item)
        {
            if (!TryGetProxy(item, out var leaf))
            {
                return false;
            }

            Assert(Contains(item));

            ref var leafNode = ref _nodes[leaf];

            Assert(leafNode.IsLeaf);

            Assert(Equals(leafNode.Item,item));

            var oldBox = leafNode.Aabb;

            var newBox = _extractAabb(item);

            if (leafNode.Aabb.Contains(newBox))
            {
                return false;
            }

            var movedDist = newBox.Center - oldBox.Center;

            var fattenedNewBox = newBox.Enlarged(AabbExtendSize);

            fattenedNewBox = newBox.Union(fattenedNewBox.Translated(movedDist));

            Assert(fattenedNewBox.Contains(newBox));

            RemoveLeaf(leaf);

            leafNode.Aabb = fattenedNewBox;

            InsertLeaf(leaf);

            Assert(Contains(item));

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref T GetValue(Proxy proxy)
            => ref _nodes[proxy].Item;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref Box2 GetAabb(Proxy proxy)
            => ref _nodes[proxy].Aabb;

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public IEnumerable<T> Query(Box2 aabb, bool approx = false)
        {
            var stack = new Stack<Proxy>(256);

            stack.Push(_root);

            while (stack.Count > 0)
            {
                var proxy = stack.Pop();

                if (proxy == Proxy.Free)
                {
                    continue;
                }

                // note: non-ref stack local copy here
                var node = _nodes[proxy];

                if (!node.Aabb.Intersects(aabb))
                {
                    continue;
                }

                if (!node.IsLeaf)
                {
                    if (node.Child1 != Proxy.Free)
                    {
                        stack.Push(node.Child1);
                    }

                    if (node.Child2 != Proxy.Free)
                    {
                        stack.Push(node.Child2);
                    }

                    continue;
                }

                var item = node.Item;

                if (!approx)
                {
                    var preciseAabb = _extractAabb(item);

                    if (!preciseAabb.Intersects(aabb))
                    {
                        continue;
                    }
                }

                yield return node.Item;
            }
        }


        public IEnumerable<(T A,T B)> GetCollisions(bool approx = false)
        {
            var stack = new Stack<Proxy>(256);

            ISet<(Proxy, Proxy)> collisions = new HashSet<(Proxy, Proxy)>(_nodeLookup.Count);

            foreach (var (_, leaf) in _nodeLookup)
            {
                foreach (var pair in GetCollisions(stack, collisions, leaf, approx))
                {
                    yield return (_nodes[pair.A].Item, _nodes[pair.B].Item);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        private IEnumerable<(Proxy A,Proxy B)> GetCollisions(Stack<Proxy> stack, ISet<(Proxy,Proxy)> pairs, Proxy leaf, bool approx = false)
        {
            stack.Clear();

            var leafNode = _nodes[leaf];

            var aabb = approx ? leafNode.Aabb : _extractAabb(leafNode.Item);

            var parent = leafNode.Parent;

            if (parent == Proxy.Free)
            {
                yield break;
            }

            stack.Push(parent);

            while (stack.Count > 0)
            {
                var proxy = stack.Pop();

                if (proxy == Proxy.Free || proxy == leaf)
                {
                    continue;
                }

                // note: non-ref stack local copy here
                var node = _nodes[proxy];

                if (!node.Aabb.Intersects(aabb))
                {
                    continue;
                }

                if (!node.IsLeaf)
                {
                    if (node.Child1 != Proxy.Free)
                    {
                        stack.Push(node.Child1);
                    }

                    if (node.Child2 != Proxy.Free)
                    {
                        stack.Push(node.Child2);
                    }

                    continue;
                }

                var item = node.Item;

                if (!approx)
                {
                    var preciseAabb = _extractAabb(item);

                    if (!preciseAabb.Intersects(aabb))
                    {
                        continue;
                    }
                }

                var pair = leaf > proxy ? (proxy, leaf) : (leaf, proxy);

                if (!pairs.Add(pair))
                {
                    continue;
                }

                yield return pair;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public IEnumerable<T> Query(Vector2 point, bool approx = false)
        {
            var stack = new Stack<Proxy>(256);

            stack.Push(_root);

            while (stack.Count > 0)
            {
                var proxy = stack.Pop();

                if (proxy == Proxy.Free)
                {
                    continue;
                }

                // note: non-ref stack local copy here
                var node = _nodes[proxy];

                if (!node.Aabb.Contains(point))
                {
                    continue;
                }

                if (!node.IsLeaf)
                {
                    if (node.Child1 != Proxy.Free)
                    {
                        stack.Push(node.Child1);
                    }

                    if (node.Child2 != Proxy.Free)
                    {
                        stack.Push(node.Child2);
                    }

                    continue;
                }

                var item = node.Item;

                if (!approx)
                {
                    var preciseAabb = _extractAabb(item);

                    if (!preciseAabb.Contains(point))
                    {
                        continue;
                    }
                }

                yield return node.Item;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Dot(in Vector2 a, in Vector2 b)
            => a.X * b.X + a.Y * b.Y;

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public bool Query(RayQueryCallbackDelegate callback, in Vector2 start, in Vector2 dir, bool approx = false)
        {
            var stack = new Stack<Proxy>(256);

            stack.Push(_root);

            var any = false;

            var ray = new Ray(start, dir);

            while (stack.Count > 0)
            {
                var proxy = stack.Pop();

                if (proxy == Proxy.Free)
                {
                    continue;
                }

                ref var node = ref _nodes[proxy];

                if (!ray.Intersects(node.Aabb, out var dist, out var hit))
                {
                    continue;
                }

                var item = node.Item;

                if (node.IsLeaf)
                {

                    if (!approx)
                    {
                        var preciseAabb = _extractAabb(item);

                        if (!ray.Intersects(preciseAabb, out dist, out hit))
                        {
                            continue;
                        }
                    }

                    any = true;

                    var carryOn = callback(ref node.Item, hit, dist);

                    if (!carryOn)
                    {
                        return true;
                    }
                }
                else
                {
                    if (node.Child1 != Proxy.Free)
                    {
                        stack.Push(node.Child1);
                    }

                    if (node.Child2 != Proxy.Free)
                    {
                        stack.Push(node.Child2);
                    }
                }
            }

            return any;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public void RebuildOptimal(int free = 0)
        {
            var proxies = new Proxy[NodeCount + free];
            var count = 0;

            for (var i = 0; i < Capacity; ++i)
            {
                ref var node = ref _nodes[i];
                if (node.Height < 0)
                {
                    continue;
                }

                var proxy = (Proxy) i;
                if (node.IsLeaf)
                {
                    node.Parent = Proxy.Free;
                    proxies[count++] = proxy;
                }
                else
                {
                    FreeNode(proxy);
                }
            }

            while (count > 1)
            {
                var minCost = float.MaxValue;

                var iMin = -1;
                var jMin = -1;

                for (var i = 0; i < count; ++i)
                {
                    ref var aabbI = ref _nodes[proxies[i]].Aabb;

                    for (var j = i + 1; j < count; ++j)
                    {
                        ref var aabbJ = ref _nodes[proxies[j]].Aabb;

                        var cost = Box2.Perimeter(aabbI.Union(aabbJ));

                        if (cost >= minCost)
                        {
                            continue;
                        }

                        iMin = i;
                        jMin = j;
                        minCost = cost;
                    }
                }

                var child1 = proxies[iMin];
                var child2 = proxies[jMin];

                var parent = AllocateNode();

                ref var child1Node = ref _nodes[child1];
                ref var child2Node = ref _nodes[child2];
                ref var parentNode = ref _nodes[parent];

                parentNode.Child1 = child1;
                parentNode.Child2 = child2;
                parentNode.Height = Math.Max(child1Node.Height, child2Node.Height) + 1;
                parentNode.Aabb = child1Node.Aabb.Union(child2Node.Aabb);
                parentNode.Parent = Proxy.Free;

                child1Node.Parent = parent;
                child2Node.Parent = parent;

                proxies[jMin] = proxies[count - 1];
                proxies[iMin] = parent;
                --count;
            }

            _root = proxies[0];

            Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public void ShiftOrigin(in Vector2 newOrigin)
        {
            for (var i = 0; i < Capacity; ++i)
            {
                ref var node = ref _nodes[i];
                node.Aabb = new Box2(
                    node.Aabb.BottomLeft - newOrigin,
                    node.Aabb.TopRight - newOrigin
                );
            }
        }

        /// <remarks>
        ///     If allocation occurs, references to <see cref="Node" />s will be invalid.
        /// </remarks>
        private Proxy AllocateNode()
        {
            if (_freeNodes == Proxy.Free)
            {
                var newNodeCap = GrowthFunc(Capacity);

                if (newNodeCap <= Capacity)
                {
                    throw new InvalidOperationException("Growth function returned invalid new capacity, must be greater than current capacity.");
                }

                EnsureCapacity(newNodeCap);
            }

            var alloc = _freeNodes;
            ref var allocNode = ref _nodes[alloc];
            Assert(allocNode.IsFree);
            _freeNodes = allocNode.Parent;
            Assert(_freeNodes == -1 || _nodes[_freeNodes].IsFree);
            allocNode.Parent = Proxy.Free;
            allocNode.Child1 = Proxy.Free;
            allocNode.Child2 = Proxy.Free;
            allocNode.Height = 0;
            ++NodeCount;
            return alloc;
        }

        public void EnsureCapacity(int newCapacity)
        {
            if (newCapacity <= Capacity)
            {
                return;
            }

            var oldNodes = _nodes;

            _nodes = new Node[newCapacity];

            Array.Copy(oldNodes, _nodes, NodeCount);

            var l = Capacity - 1;
            for (var i = NodeCount; i < l; ++i)
            {
                ref var node = ref _nodes[i];
                node.Parent = (Proxy) (i + 1);
                node.Height = -1;
            }

            ref var lastNode = ref _nodes[l];
            lastNode.Parent = Proxy.Free;
            lastNode.Height = -1;
            _freeNodes = (Proxy) NodeCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FreeNode(Proxy proxy)
        {
            ref var node = ref _nodes[proxy];
            node.Parent = _freeNodes;
            node.Height = -1;
            node.Child1 = Proxy.Free;
            node.Child2 = Proxy.Free;
            node.Item = default;
            _freeNodes = proxy;
            --NodeCount;
        }

        [Conditional("DEBUG_DYNAMIC_TREE")]
        private void Validate()
        {
            Validate(_root);

            var freeCount = 0;
            var freeIndex = _freeNodes;
            while (freeIndex != Proxy.Free)
            {
                Assert(0 <= freeIndex);
                Assert(freeIndex < Capacity);
                freeIndex = _nodes[freeIndex].Parent;
                ++freeCount;
            }

            Assert(Height == ComputeHeight());

            Assert(NodeCount + freeCount == Capacity);
        }

        [Conditional("DEBUG_DYNAMIC_TREE")]
        private void Validate(Proxy proxy)
        {
            if (proxy == Proxy.Free) return;

            ref var node = ref _nodes[proxy];

            if (proxy == _root)
            {
                Assert(node.Parent == Proxy.Free);
            }

            var child1 = node.Child1;
            var child2 = node.Child2;

            if (node.IsLeaf)
            {
                Assert(child1 == Proxy.Free);
                Assert(child2 == Proxy.Free);
                Assert(node.Height == 0);
                return;
            }

            Assert(0 <= child1);
            Assert(child1 < Capacity);
            Assert(0 <= child2);
            Assert(child2 < Capacity);

            ref var child1Node = ref _nodes[child1];
            ref var child2Node = ref _nodes[child2];

            Assert(child1Node.Parent == proxy);
            Assert(child2Node.Parent == proxy);

            var height1 = child1Node.Height;
            var height2 = child2Node.Height;

            var height = 1 + Math.Max(height1, height2);

            Assert(node.Height == height);

            ref var aabb = ref node.Aabb;
            Assert(aabb.Contains(child1Node.Aabb));
            Assert(aabb.Contains(child2Node.Aabb));

            Validate(child1);
            Validate(child2);
        }

        [Conditional("DEBUG_DYNAMIC_TREE")]
        private void ValidateHeight(Proxy proxy)
        {
            if (proxy == Proxy.Free)
            {
                return;
            }

            ref var node = ref _nodes[proxy];

            if (node.IsLeaf)
            {
                Assert(node.Height == 0);
                return;
            }

            var child1 = node.Child1;
            var child2 = node.Child2;
            ref var child1Node = ref _nodes[child1];
            ref var child2Node = ref _nodes[child2];

            var height1 = child1Node.Height;
            var height2 = child2Node.Height;

            var height = 1 + Math.Max(height1, height2);

            Assert(node.Height == height);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        private void InsertLeaf(Proxy leaf)
        {
            if (_root == Proxy.Free)
            {
                _root = leaf;
                _nodes[_root].Parent = Proxy.Free;
                return;
            }

            Validate();

            ref var leafNode = ref _nodes[leaf];

            _nodeLookup[leafNode.Item] = leaf;

            ref var leafAabb = ref leafNode.Aabb;

            var index = _root;
#if DEBUG
            var loopCount = 0;
#endif
            for (;;)
            {
#if DEBUG
                Assert(loopCount++ < Capacity * 2);
#endif

                ref var indexNode = ref _nodes[index];
                if (indexNode.IsLeaf) break;

                // assert no loops
                Assert(_nodes[indexNode.Child1].Child1 != index);
                Assert(_nodes[indexNode.Child1].Child2 != index);
                Assert(_nodes[indexNode.Child2].Child1 != index);
                Assert(_nodes[indexNode.Child2].Child2 != index);

                var child1 = indexNode.Child1;
                var child2 = indexNode.Child2;
                ref var child1Node = ref _nodes[child1];
                ref var child2Node = ref _nodes[child2];
                ref var indexAabb = ref indexNode.Aabb;
                var indexPeri = Box2.Perimeter(indexAabb);
                var combinedAabb = indexAabb.Union(leafAabb);
                var combinedPeri = Box2.Perimeter(combinedAabb);
                var cost = 2 * combinedPeri;
                var inheritCost = 2 * (combinedPeri - indexPeri);

                var cost1 = EstimateCost(leafAabb, child1Node) + inheritCost;
                var cost2 = EstimateCost(leafAabb, child2Node) + inheritCost;

                if (cost < cost1 && cost < cost2)
                {
                    break;
                }

                index = cost1 < cost2 ? child1 : child2;
            }

            var newParent = AllocateNode();

            var sibling = index;
            ref var siblingNode = ref _nodes[sibling];

            var oldParent = siblingNode.Parent;

            ref var newParentNode = ref _nodes[newParent];
            newParentNode.Parent = oldParent;
            newParentNode.Aabb = leafAabb.Union(siblingNode.Aabb);
            newParentNode.Height = 1 + siblingNode.Height;

            ref var proxyNode = ref _nodes[leaf];
            if (oldParent != Proxy.Free)
            {
                ref var oldParentNode = ref _nodes[oldParent];

                if (oldParentNode.Child1 == sibling)
                {
                    oldParentNode.Child1 = newParent;
                }
                else
                {
                    oldParentNode.Child2 = newParent;
                }

                newParentNode.Child1 = sibling;
                newParentNode.Child2 = leaf;
                siblingNode.Parent = newParent;
                proxyNode.Parent = newParent;
            }
            else
            {
                newParentNode.Child1 = sibling;
                newParentNode.Child2 = leaf;
                siblingNode.Parent = newParent;
                proxyNode.Parent = newParent;
                _root = newParent;
            }

            Balance(proxyNode.Parent);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        private void RemoveLeaf(Proxy leaf)
        {
            if (leaf == _root)
            {
                _root = Proxy.Free;
                return;
            }

            ref var leafNode = ref _nodes[leaf];

            Assert(leafNode.IsLeaf);

            var parent = leafNode.Parent;

            if (parent == Proxy.Free)
            {
                return;
            }

            Validate();

            ref var parentNode = ref _nodes[parent];
            var grandParent = parentNode.Parent;
            var sibling = parentNode.Child1 == leaf
                ? parentNode.Child2
                : parentNode.Child1;
            ref var siblingNode = ref _nodes[sibling];

            if (grandParent == Proxy.Free)
            {
                _root = Proxy.Free;
                siblingNode.Parent = Proxy.Free;
                FreeNode(parent);
                return;
            }

            ref var grandParentNode = ref _nodes[grandParent];
            if (grandParentNode.Child1 == parent)
            {
                grandParentNode.Child1 = sibling;
            }
            else
            {
                grandParentNode.Child2 = sibling;
            }

            siblingNode.Parent = grandParent;
            FreeNode(parent);

            Balance(grandParent);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        private void Balance(Proxy index)
        {
            while (index != Proxy.Free)
            {
                index = BalanceStep(index);

                ref var indexNode = ref _nodes[index];

                var child1 = indexNode.Child1;
                var child2 = indexNode.Child2;

                Assert(child1 != Proxy.Free);
                Assert(child2 != Proxy.Free);

                ref var child1Node = ref _nodes[child1];
                ref var child2Node = ref _nodes[child2];

                indexNode.Height = Math.Max(child1Node.Height, child2Node.Height) + 1;
                indexNode.Aabb = child1Node.Aabb.Union(child2Node.Aabb);

                index = indexNode.Parent;
            }

            Validate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Proxy BalanceStep(Proxy iA)
        {
            ref var a = ref _nodes[iA];

            if (a.IsLeaf || a.Height < 2)
            {
                return iA;
            }

            var iB = a.Child1;
            var iC = a.Child2;
            Assert(iA != iB);
            Assert(iA != iC);
            Assert(iB != iC);

            ref var b = ref _nodes[iB];
            ref var c = ref _nodes[iC];

            var balance = c.Height - b.Height;

            // Rotate C up
            if (balance > 1)
            {
                var iF = c.Child1;
                var iG = c.Child2;
                Assert(iC != iF);
                Assert(iC != iG);
                Assert(iF != iG);

                ref var f = ref _nodes[iF];
                ref var g = ref _nodes[iG];

                // A <> C

                // this creates a loop ...
                c.Child1 = iA;
                c.Parent = a.Parent;
                a.Parent = iC;

                if (c.Parent == Proxy.Free)
                {
                    _root = iC;
                }
                else
                {
                    ref var cParent = ref _nodes[c.Parent];
                    if (cParent.Child1 == iA)
                    {
                        cParent.Child1 = iC;
                    }
                    else
                    {
                        Assert(cParent.Child2 == iA);
                        cParent.Child2 = iC;
                    }
                }

                // Rotate
                if (f.Height > g.Height)
                {
                    c.Child2 = iF;
                    a.Child2 = iG;
                    g.Parent = iA;
                    a.Aabb = b.Aabb.Union(g.Aabb);
                    c.Aabb = a.Aabb.Union(f.Aabb);

                    a.Height = Math.Max(b.Height, g.Height) + 1;
                    c.Height = Math.Max(a.Height, f.Height) + 1;
                }
                else
                {
                    c.Child2 = iG;
                    a.Child2 = iF;
                    f.Parent = iA;
                    a.Aabb = b.Aabb.Union(f.Aabb);
                    c.Aabb = a.Aabb.Union(g.Aabb);

                    a.Height = Math.Max(b.Height, f.Height) + 1;
                    c.Height = Math.Max(a.Height, g.Height) + 1;
                }

                return iC;
            }

            // Rotate B up
            if (balance < -1)
            {
                var iD = b.Child1;
                var iE = b.Child2;
                Assert(iB != iD);
                Assert(iB != iE);
                Assert(iD != iE);

                ref var d = ref _nodes[iD];
                ref var e = ref _nodes[iE];

                // A <> B

                // this creates a loop ...
                b.Child1 = iA;
                b.Parent = a.Parent;
                a.Parent = iB;

                if (b.Parent == Proxy.Free)
                {
                    _root = iB;
                }
                else
                {
                    ref var bParent = ref _nodes[b.Parent];
                    if (bParent.Child1 == iA)
                    {
                        bParent.Child1 = iB;
                    }
                    else
                    {
                        Assert(bParent.Child2 == iA);
                        bParent.Child2 = iB;
                    }
                }

                // Rotate
                if (d.Height > e.Height)
                {
                    b.Child2 = iD;
                    a.Child1 = iE;
                    e.Parent = iA;
                    a.Aabb = c.Aabb.Union(e.Aabb);
                    b.Aabb = a.Aabb.Union(d.Aabb);

                    a.Height = Math.Max(c.Height, e.Height) + 1;
                    b.Height = Math.Max(a.Height, d.Height) + 1;
                }
                else
                {
                    b.Child2 = iE;
                    a.Child1 = iD;
                    d.Parent = iA;
                    a.Aabb = c.Aabb.Union(d.Aabb);
                    b.Aabb = a.Aabb.Union(e.Aabb);

                    a.Height = Math.Max(c.Height, d.Height) + 1;
                    b.Height = Math.Max(a.Height, e.Height) + 1;
                }

                return iB;
            }

            return iA;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float EstimateCost(in Box2 baseAabb, in Node node)
        {
            var cost = Box2.Perimeter(
                baseAabb.Union(node.Aabb)
            );

            if (!node.IsLeaf)
            {
                cost -= Box2.Perimeter(node.Aabb);
            }

            return cost;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ComputeHeight()
            => ComputeHeight(_root);

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        private int ComputeHeight(Proxy proxy)
        {
            ref var node = ref _nodes[proxy];
            if (node.IsLeaf)
            {
                return 0;
            }

            return Math.Max(
                ComputeHeight(node.Child1),
                ComputeHeight(node.Child2)
            ) + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AddOrUpdate(T item) => Update(item) || Add(item);

        [Conditional("DEBUG_DYNAMIC_TREE")]
        [Conditional("DEBUG_DYNAMIC_TREE_ASSERTS")]
        [DebuggerNonUserCode] [DebuggerHidden] [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool assertion, [CallerMemberName] string member = default, [CallerFilePath] string file = default, [CallerLineNumber] int line = default)
        {
            if (assertion) return;

            var msg = $"Assertion failure in {member} ({file}:{line})";
            Debug.Print(msg);
            Debugger.Break();
            throw new InvalidOperationException(msg);
        }

    }

}
