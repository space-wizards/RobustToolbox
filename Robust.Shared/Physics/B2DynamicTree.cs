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

// #define DEBUG_DYNAMIC_TREE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Robust.Shared.Physics
{
    // This is a closer port of Box2D's b2DynamicTree than our DynamicTree<T> is.
    // Differences are:
    // 1. only deals with AABBs and proxies it allocates itself.
    //    No internal object -> proxy dict or AABB extraction delegate.
    // 2. only does approximate lookups.
    // 3. more lightweight and faster.
    // 4. support for moved flags on proxies.

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class B2DynamicTree<T> : DynamicTree
    {
        public delegate bool RayQueryCallback<TState>(ref TState state, Proxy proxy, in Vector2 hitPos, float distance);

        public delegate bool RayQueryCallback(Proxy proxy, in Vector2 hitPos, float distance);

        public delegate bool QueryCallback(Proxy proxy);

        public delegate bool QueryCallback<TState>(ref TState state, Proxy proxy);

        private struct Node
        {
            public Box2 Aabb;
            public Proxy Parent;
            public Proxy Child1;
            public Proxy Child2;

            public int Height;
            public T UserData;
            public bool Moved;

            public bool IsLeaf
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Child2 == Proxy.Free;
            }

            public bool IsFree
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Height == -1;
            }

            public override string ToString()
                => $@"Parent: {(Parent == Proxy.Free ? "None" : Parent.ToString())}, {
                    (IsLeaf
                        ? Height == 0
                            ? $"Leaf: {UserData}"
                            : $"Leaf (invalid height of {Height}): {UserData}"
                        : IsFree
                            ? "Free"
                            : $"Branch at height {Height}, children: {Child1} and {Child2}")}";
        }

        public int Capacity => _nodes.Length;
        private Node[] _nodes;
        private Proxy _root;
        private Proxy _freeNodes;
        private int _nodeCount;

        public int Height
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _root == Proxy.Free ? 0 : _nodes[_root].Height;
        }

        public int NodeCount => _nodeCount;

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

        public B2DynamicTree(float aabbExtendSize = 1f / 32, int capacity = 256, Func<int, int>? growthFunc = null) :
            base(aabbExtendSize, growthFunc)
        {
            capacity = Math.Max(MinimumCapacity, capacity);

            _root = Proxy.Free;
            _nodes = new Node[capacity];

            // Build a linked list for the free list.
            ref var node = ref _nodes[0];
            var l = Capacity - 1;
            for (var i = 0; i < l; i++, node = ref Unsafe.Add(ref node, 1))
            {
                node.Parent = (Proxy) (i + 1);
                node.Height = -1;
            }

            ref var lastNode = ref _nodes[^1];

            lastNode.Parent = Proxy.Free;
            lastNode.Height = -1;
        }

        /// <summary>Allocate a node from the pool. Grow the pool if necessary.</summary>
        /// <remarks>
        ///     If allocation occurs, references to <see cref="Node" />s will be invalid.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref Node AllocateNode(out Proxy proxy)
        {
            // Expand the node pool as needed.
            if (_freeNodes == Proxy.Free)
            {
                // Separate method to aid inlining since this is a cold path.
                Expand();
            }

            // Peel a node off the free list.
            var alloc = _freeNodes;
            ref var allocNode = ref _nodes[alloc];
            Assert(allocNode.IsFree);
            _freeNodes = allocNode.Parent;
            Assert(_freeNodes == -1 || _nodes[_freeNodes].IsFree);
            allocNode.Parent = Proxy.Free;
            allocNode.Child1 = Proxy.Free;
            allocNode.Child2 = Proxy.Free;
            allocNode.Height = 0;
            ++_nodeCount;
            proxy = alloc;
            return ref allocNode;

            void Expand()
            {
                Assert(_nodeCount == Capacity);

                // The free list is empty. Rebuild a bigger pool.
                var newNodeCap = GrowthFunc(Capacity);

                if (newNodeCap <= Capacity)
                {
                    throw new InvalidOperationException(
                        "Growth function returned invalid new capacity, must be greater than current capacity.");
                }

                var oldNodes = _nodes;

                _nodes = new Node[newNodeCap];

                Array.Copy(oldNodes, _nodes, _nodeCount);

                // Build a linked list for the free list. The parent
                // pointer becomes the "next" pointer.
                var l = _nodes.Length - 1;
                ref var node = ref _nodes[_nodeCount];
                for (var i = _nodeCount; i < l; ++i, node = ref Unsafe.Add(ref node, 1))
                {
                    node.Parent = (Proxy) (i + 1);
                    node.Height = -1;
                }

                ref var lastNode = ref _nodes[l];
                lastNode.Parent = Proxy.Free;
                lastNode.Height = -1;
                _freeNodes = (Proxy) _nodeCount;
            }
        }

        /// <summary>
        ///     Return a node to the pool.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FreeNode(Proxy proxy)
        {
            ref var node = ref _nodes[proxy];
            node.Parent = _freeNodes;
            node.Height = -1;
#if DEBUG_DYNAMIC_TREE
            node.Child1 = Proxy.Free;
            node.Child2 = Proxy.Free;
#endif
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                node.UserData = default!;
            }
            _freeNodes = proxy;
            --_nodeCount;
        }

        /// <summary>
        ///     Create a proxy in the tree as a leaf node.
        /// </summary>
        public Proxy CreateProxy(in Box2 aabb, T userData)
        {
            // Also catches NaN fuckery.
            Assert(aabb.Right >= aabb.Left && aabb.Top >= aabb.Bottom);

            ref var proxy = ref AllocateNode(out var proxyId);

            // Fatten the aabb.
            proxy.Aabb = aabb.Enlarged(AabbExtendSize);
            proxy.UserData = userData;
            proxy.Height = 0;
            proxy.Moved = true;

            InsertLeaf(proxyId);
            return proxyId;
        }

        public void DestroyProxy(Proxy proxy)
        {
            Assert(0 <= proxy && proxy < Capacity);
            Assert(_nodes[proxy].IsLeaf);

            RemoveLeaf(proxy);
            FreeNode(proxy);
        }

        public bool MoveProxy(Proxy proxy, in Box2 aabb, Vector2 displacement)
        {
            Assert(0 <= proxy && proxy < Capacity);
            // Also catches NaN fuckery.
            Assert(aabb.Right >= aabb.Left && aabb.Top >= aabb.Bottom);

            ref var leafNode = ref _nodes[proxy];

            Assert(leafNode.IsLeaf);

            // Extend AABB
            var ext = new Vector2(AabbExtendSize, AabbExtendSize);
            var fatAabb = aabb.Enlarged(AabbExtendSize);

            // Predict AABB movement
            var d = displacement * AabbMultiplier;

            if (d.X < 0)
            {
                fatAabb.Left += d.X;
            }
            else
            {
                fatAabb.Right += d.X;
            }

            if (d.Y < 0)
            {
                fatAabb.Bottom += d.Y;
            }
            else
            {
                fatAabb.Top += d.Y;
            }

            ref var treeAabb = ref leafNode.Aabb;

            if (treeAabb.Contains(aabb))
            {
                // The tree AABB still contains the object, but it might be too large.
                // Perhaps the object was moving fast but has since gone to sleep.
                // The huge AABB is larger than the new fat AABB.
                var hugeAabb = new Box2(
                    fatAabb.BottomLeft - (4, 4) * ext,
                    fatAabb.TopRight + (4, 4) * ext);

                if (hugeAabb.Contains(treeAabb))
                {
                    // The tree AABB contains the object AABB and the tree AABB is
                    // not too large. No tree update needed.
                    return false;
                }

                // Otherwise the tree AABB is huge and needs to be shrunk
            }

            RemoveLeaf(proxy);

            leafNode.Aabb = fatAabb;

            InsertLeaf(proxy);

            leafNode.Moved = true;

            return true;
        }

        [return: MaybeNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetUserData(Proxy proxy)
        {
            return _nodes[proxy].UserData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool WasMoved(Proxy proxy)
        {
            return _nodes[proxy].Moved;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearMoved(Proxy proxy)
        {
            _nodes[proxy].Moved = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Box2 GetFatAabb(Proxy proxy)
        {
            return _nodes[proxy].Aabb;
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
            ref var parentNode = ref _nodes[parent];
            var grandParent = parentNode.Parent;
            var sibling = parentNode.Child1 == leaf
                ? parentNode.Child2
                : parentNode.Child1;

            ref var siblingNode = ref _nodes[sibling];

            if (grandParent != Proxy.Free)
            {
                // Destroy parent and connect sibling to grandParent.
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

                // Adjust ancestor bounds.
                Balance(grandParent);
            }
            else
            {
                _root = sibling;
                siblingNode.Parent = Proxy.Free;
                FreeNode(parent);
            }

            Validate();
        }

        private void InsertLeaf(Proxy leaf)
        {
            if (_root == Proxy.Free)
            {
                _root = leaf;
                _nodes[_root].Parent = Proxy.Free;
                return;
            }

            Validate();

            // Find the best sibling for this node
            ref var leafNode = ref _nodes[leaf];
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
                // Cost of creating a new parent for this node and the new leaf
                var cost = 2 * combinedPeri;
                // Minimum cost of pushing the leaf further down the tree
                var inheritCost = 2 * (combinedPeri - indexPeri);

                // Cost of descending into child1
                var cost1 = EstimateCost(leafAabb, child1Node) + inheritCost;
                // Cost of descending into child2
                var cost2 = EstimateCost(leafAabb, child2Node) + inheritCost;

                // Descend according to the minimum cost.
                if (cost < cost1 && cost < cost2)
                {
                    break;
                }

                // Descend
                index = cost1 < cost2 ? child1 : child2;
            }

            var sibling = index;

            // Create a new parent.
            ref var newParentNode = ref AllocateNode(out var newParent);
            ref var siblingNode = ref _nodes[sibling];

            var oldParent = siblingNode.Parent;

            newParentNode.Parent = oldParent;
            newParentNode.Aabb = leafAabb.Union(siblingNode.Aabb);
            newParentNode.Height = 1 + siblingNode.Height;

            ref var proxyNode = ref _nodes[leaf];
            if (oldParent != Proxy.Free)
            {
                // The sibling was not the root.
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
                // The sibling was the root.
                newParentNode.Child1 = sibling;
                newParentNode.Child2 = leaf;
                siblingNode.Parent = newParent;
                proxyNode.Parent = newParent;
                _root = newParent;
            }

            // Walk back up the tree fixing heights and AABBs
            Balance(proxyNode.Parent);

            Validate();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float EstimateCost(in Box2 baseAabb, in Node node)
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

        /// <summary>
        ///     Perform a left or right rotation if node A is imbalanced.
        /// </summary>
        /// <returns>The new root index.</returns>
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
        private int ComputeHeight()
            => ComputeHeight(_root);

        /// <summary>
        ///     Compute the height of a sub-tree.
        /// </summary>
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

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
        public void RebuildBottomUp(int free = 0)
        {
            var proxies = new Proxy[NodeCount + free];
            var count = 0;

            // Build array of leaves. Free the rest.
            for (var i = 0; i < Capacity; ++i)
            {
                ref var node = ref _nodes[i];
                if (node.Height < 0)
                {
                    // free node in pool
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

                ref var parentNode = ref AllocateNode(out var parent);
                ref var child1Node = ref _nodes[child1];
                ref var child2Node = ref _nodes[child2];

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

        public void ShiftOrigin(Vector2 newOrigin)
        {
            for (var i = 0; i < _nodes.Length; i++)
            {
                ref var node = ref _nodes[i];
                var lb = node.Aabb.BottomLeft;
                var tr = node.Aabb.TopRight;

                node.Aabb = new Box2(lb - newOrigin, tr - newOrigin);
            }
        }

        private static readonly QueryCallback<QueryCallback> EasyQueryCallback =
            (ref QueryCallback callback, Proxy proxy) => callback(proxy);

        public void Query(QueryCallback callback, in Box2 aabb)
        {
            Query(ref callback, EasyQueryCallback, aabb);
        }

        public void Query<TState>(ref TState state, QueryCallback<TState> callback, in Box2 aabb)
        {
            using var stack = new GrowableStack<Proxy>(stackalloc Proxy[256]);
            stack.Push(_root);

            ref var baseRef = ref _nodes[0];
            while (stack.GetCount() != 0)
            {
                var nodeId = stack.Pop();
                if (nodeId == Proxy.Free)
                {
                    continue;
                }

                // Skip bounds check with Unsafe.Add().
                ref var node = ref Unsafe.Add(ref baseRef, nodeId);
                if (node.Aabb.Intersects(aabb))
                {
                    if (node.IsLeaf)
                    {
                        var proceed = callback(ref state, nodeId);
                        if (proceed == false)
                        {
                            return;
                        }
                    }
                    else
                    {
                        stack.Push(node.Child1);
                        stack.Push(node.Child2);
                    }
                }
            }
        }

        private static readonly RayQueryCallback<RayQueryCallback> EasyRayQueryCallback =
            (ref RayQueryCallback callback, Proxy proxy, in Vector2 hitPos, float distance) => callback(proxy, hitPos, distance);

        public void RayCast(RayQueryCallback callback, in Ray input)
        {
            RayCast(ref callback, EasyRayQueryCallback, input);
        }

        public void RayCast<TState>(ref TState state, RayQueryCallback<TState> callback, in Ray input)
        {
            // NOTE: This is not Box2D's normal ray cast function, since our rays have infinite length.

            using var stack = new GrowableStack<Proxy>(stackalloc Proxy[256]);

            stack.Push(_root);

            ref var baseRef = ref _nodes[0];
            while (stack.GetCount() > 0)
            {
                var proxy = stack.Pop();

                if (proxy == Proxy.Free)
                {
                    continue;
                }

                ref var node = ref Unsafe.Add(ref baseRef, proxy);

                if (!input.Intersects(node.Aabb, out var dist, out var hit))
                {
                    continue;
                }

                if (node.IsLeaf)
                {
                    var carryOn = callback(ref state, proxy, hit, dist);

                    if (!carryOn)
                    {
                        return;
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
    }
}
