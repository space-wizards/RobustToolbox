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
#define B2_TREE_HEURISTIC
#undef B2_TREE_HEURISTIC

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

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
        public const int B2_Bin_Count = 8;

        public delegate bool RayQueryCallback<TState>(ref TState state, Proxy proxy, in Vector2 hitPos, float distance);

        public delegate bool RayQueryCallback(Proxy proxy, in Vector2 hitPos, float distance);

        public delegate bool QueryCallback(Proxy proxy);

        public delegate bool QueryCallback<TState>(ref TState state, Proxy proxy);

        private enum RotateType : byte
        {
            None,
            BF,
            BG,
            CD,
            CE,
        }

        private struct Node
        {
            /// <summary>
            /// Node's bounding box
            /// </summary>
            public Box2 Aabb;

            /// <summary>
            /// Category bits for collision filtering.
            /// </summary>
            public uint CategoryBits;

            /// <summary>
            /// Node parent index.
            /// </summary>
            public Proxy Parent;

            /// <summary>
            /// Node freelist next index.
            /// </summary>
            public Proxy Next;

            public Proxy Child1;
            public Proxy Child2;

            public T UserData;

            /// <summary>
            /// Leaf = 0, Free node = -1
            /// </summary>
            public short Height;

            /// <summary>
            /// Has the AABB been enlarged?
            /// </summary>
            public bool Enlarged;

            public bool IsLeaf
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Height == 0;
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

        private const int TreeStackSize = 1024;

        /// <summary>
        /// Tree nodes.
        /// </summary>
        private Node[] _nodes;

        /// <summary>
        /// Root index.
        /// </summary>
        private Proxy _root;

        /// <summary>
        /// The number of nodes.
        /// </summary>
        public int NodeCount { get; private set; }

        /// <summary>
        /// The allocated node space.
        /// </summary>
        public int Capacity => _nodes.Length;

        private Proxy _freeList;

        /// <summary>
        /// The number of proxies created.
        /// </summary>
        public int ProxyCount;

        /// <summary>
        /// Leaf indices for rebuild.
        /// </summary>
        public Proxy[] LeafIndices = Array.Empty<Proxy>();

        /// <summary>
        /// Leaf bounding boxes for rebuild.
        /// </summary>
        public Box2[] LeafBoxes = Array.Empty<Box2>();

        /// <summary>
        /// Leaf bounding box centers for rebuild.
        /// </summary>
        public Vector2[] LeafCenters = Array.Empty<Vector2>();

        /// <summary>
        /// Bins for sorting during rebuild.
        /// </summary>
        public int[] BinIndices = Array.Empty<int>();

        /// <summary>
        /// Allocated space for rebuilding.
        /// </summary>
        public int RebuildCapacity;

        public int Height
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _root == Proxy.Free ? 0 : _nodes[_root].Height;
        }

        public int MaxBalance
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
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

                    Assert(!node.IsLeaf);

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
            [MethodImpl(MethodImplOptions.NoInlining)]
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
                    if (node.Height < 0 || node.IsLeaf || i == _root)
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
                node.Next = (Proxy) (i + 1);
                node.Height = -1;
            }

            ref var lastNode = ref _nodes[^1];

            lastNode.Next = Proxy.Free;
            lastNode.Height = -1;
        }

        public Box2? GetFatAabb(Proxy proxy)
        {
            return _nodes[proxy].Aabb;
        }

        /// <summary>Allocate a node from the pool. Grow the pool if necessary.</summary>
        /// <remarks>
        ///     If allocation occurs, references to <see cref="Node" />s will be invalid.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref Node AllocateNode(out Proxy proxy)
        {
            // Expand the node pool as needed.
            if (_freeList == Proxy.Free)
            {
                // Separate method to aid inlining since this is a cold path.
                Expand();
            }

            // Peel a node off the free list.
            var alloc = _freeList;
            ref var allocNode = ref _nodes[alloc];
            Assert(allocNode.IsFree);
            _freeList = allocNode.Next;
            Assert(_freeList == -1 || _nodes[_freeList].IsFree);
            allocNode = default;
            ++NodeCount;
            proxy = alloc;
            return ref allocNode;

            void Expand()
            {
                Assert(NodeCount == Capacity);

                // The free list is empty. Rebuild a bigger pool.
                var newNodeCap = GrowthFunc(Capacity);

                if (newNodeCap <= Capacity)
                {
                    throw new InvalidOperationException(
                        "Growth function returned invalid new capacity, must be greater than current capacity.");
                }

                Array.Resize(ref _nodes, newNodeCap);

                // Build a linked list for the free list. The parent
                // pointer becomes the "next" pointer.
                var l = _nodes.Length - 1;
                ref var node = ref _nodes[NodeCount];
                for (var i = NodeCount; i < l; ++i, node = ref Unsafe.Add(ref node, 1))
                {
                    node.Next = (Proxy) (i + 1);
                    node.Height = -1;
                }

                ref var lastNode = ref _nodes[l];
                lastNode.Next = Proxy.Free;
                lastNode.Height = -1;
                _freeList = (Proxy) NodeCount;
            }
        }

        /// <summary>
        ///     Return a node to the pool.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FreeNode(Proxy proxy)
        {
            Assert(0 <= proxy && proxy < Capacity);
            Assert(0 < NodeCount);

            ref var node = ref _nodes[proxy];
            node.Next = _freeList;
            node.Height = -1;

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                node.UserData = default!;
            }
            _freeList = proxy;
            --NodeCount;
        }

        // Greedy algorithm for sibling selection using the SAH
        // We have three nodes A-(B,C) and want to add a leaf D, there are three choices.
        // 1: make a new parent for A and D : E-(A-(B,C), D)
        // 2: associate D with B
        //   a: B is a leaf : A-(E-(B,D), C)
        //   b: B is an internal node: A-(B{D},C)
        // 3: associate D with C
        //   a: C is a leaf : A-(B, E-(C,D))
        //   b: C is an internal node: A-(B, C{D})
        // All of these have a clear cost except when B or C is an internal node. Hence we need to be greedy.

        // The cost for cases 1, 2a, and 3a can be computed using the sibling cost formula.
        // cost of sibling H = area(union(H, D)) + increased are of ancestors

        // Suppose B (or C) is an internal node, then the lowest cost would be one of two cases:
        // case1: D becomes a sibling of B
        // case2: D becomes a descendant of B along with a new internal node of area(D).
        public Proxy FindBestSibling(Box2 boxD)
        {
            var centerD = boxD.Center;
            float areaD = Box2.Perimeter(boxD);

            var nodes = _nodes;
            var rootIndex = _root;

            ref var rootBox = ref nodes[rootIndex].Aabb;

            // Area of current node
            float areaBase = Box2.Perimeter(rootBox);

            // Area of inflated node
            float directCost = Box2.Perimeter(rootBox.Union(boxD));
            float inheritedCost = 0.0f;

            Proxy bestSibling = rootIndex;
            float bestCost = directCost;

            // Descend the tree from root, following a single greedy path.
            Proxy index = rootIndex;
            while (nodes[index].Height > 0)
            {
                ref var child1 = ref nodes[index].Child1;
                ref var child2 = ref nodes[index].Child2;

                // Cost of creating a new parent for this node and the new leaf
                float cost = directCost + inheritedCost;

                // Sometimes there are multiple identical costs within tolerance.
                // This breaks the ties using the centroid distance.
                if (cost < bestCost)
                {
                    bestSibling = index;
                    bestCost = cost;
                }

                // Inheritance cost seen by children
                inheritedCost += directCost - areaBase;

                bool leaf1 = nodes[child1].Height == 0;
                bool leaf2 = nodes[child2].Height == 0;

                // Cost of descending into child 1
                float lowerCost1 = float.MaxValue;
                var box1 = nodes[child1].Aabb;
                float directCost1 = Box2.Perimeter(box1.Union(boxD));
                float area1 = 0.0f;
                if (leaf1)
                {
                    // Child 1 is a leaf
                    // Cost of creating new node and increasing area of node P
                    float cost1 = directCost1 + inheritedCost;

                    // Need this here due to while condition above
                    if (cost1 < bestCost)
                    {
                        bestSibling = child1;
                        bestCost = cost1;
                    }
                }
                else
                {
                    // Child 1 is an internal node
                    area1 = Box2.Perimeter(box1);

                    // Lower bound cost of inserting under child 1.
                    lowerCost1 = inheritedCost + directCost1 + MathF.Min(areaD - area1, 0.0f);
                }

                // Cost of descending into child 2
                float lowerCost2 = float.MaxValue;
                var box2 = nodes[child2].Aabb;
                float directCost2 = Box2.Perimeter(box2.Union(boxD));
                float area2 = 0.0f;
                if (leaf2)
                {
                    // Child 2 is a leaf
                    // Cost of creating new node and increasing area of node P
                    float cost2 = directCost2 + inheritedCost;

                    // Need this here due to while condition above
                    if (cost2 < bestCost)
                    {
                        bestSibling = child2;
                        bestCost = cost2;
                    }
                }
                else
                {
                    // Child 2 is an internal node
                    area2 = Box2.Perimeter(box2);

                    // Lower bound cost of inserting under child 2. This is not the cost
                    // of child 2, it is the best we can hope for under child 2.
                    lowerCost2 = inheritedCost + directCost2 + MathF.Min(areaD - area2, 0.0f);
                }

                if (leaf1 && leaf2)
                {
                    break;
                }

                // Can the cost possibly be decreased?
                if (bestCost <= lowerCost1 && bestCost <= lowerCost2)
                {
                    break;
                }

                if (lowerCost1 == lowerCost2 && leaf1 == false)
                {
                    Assert(lowerCost1 < float.MaxValue);
                    Assert(lowerCost2 < float.MaxValue);

                    // No clear choice based on lower bound surface area. This can happen when both
                    // children fully contain D. Fall back to node distance.
                    var d1 = Vector2.Subtract(box1.Center, centerD);
                    var d2 = Vector2.Subtract(box2.Center, centerD);
                    lowerCost1 = d1.LengthSquared();
                    lowerCost2 = d2.LengthSquared();
                }

                // Descend
                if (lowerCost1 < lowerCost2 && leaf1 == false)
                {
                    index = child1;
                    areaBase = area1;
                    directCost = directCost1;
                }
                else
                {
                    index = child2;
                    areaBase = area2;
                    directCost = directCost2;
                }

                Assert(nodes[index].Height > 0);
            }

            return bestSibling;
        }

        /// <summary>
        /// Perform a left or right rotation if node A is imbalance.
        /// </summary>
        /// <returns>The new root index.</returns>
        public void RotateNodes(Proxy iA)
        {
            Assert(iA != Proxy.Free);

            ref var A = ref _nodes[iA];
	        if (A.Height < 2)
	        {
		        return;
	        }

	        var iB = A.Child1;
	        var iC = A.Child2;
	        Assert(0 <= iB && iB < Capacity);
            Assert(0 <= iC && iC < Capacity);

	        ref var B = ref _nodes[iB];
            ref var C = ref _nodes[iC];

	        if (B.Height == 0)
	        {
		        // B is a leaf and C is internal
		        Assert(C.Height > 0);

		        Proxy iF = C.Child1;
                Proxy iG = C.Child2;
		        ref var F = ref _nodes[iF];
		        ref var G = ref _nodes[iG];
		        Assert(0 <= iF && iF < Capacity);
		        Assert(0 <= iG && iG < Capacity);

		        // Base cost
		        float costBase = Box2.Perimeter(C.Aabb);

		        // Cost of swapping B and F
		        var aabbBG = B.Aabb.Union(G.Aabb);
		        float costBF = Box2.Perimeter(aabbBG);

		        // Cost of swapping B and G
		        var aabbBF = B.Aabb.Union(F.Aabb);
		        float costBG = Box2.Perimeter(aabbBF);

		        if (costBase < costBF && costBase < costBG)
		        {
			        // Rotation does not improve cost
			        return;
		        }

		        if (costBF < costBG)
		        {
			        // Swap B and F
			        A.Child1 = iF;
			        C.Child1 = iB;

			        B.Parent = iC;
			        F.Parent = iA;

			        C.Aabb = aabbBG;

			        C.Height = (short)(1 + Math.Max(B.Height, G.Height));
			        A.Height = (short)(1 + Math.Max(C.Height, F.Height));
			        C.CategoryBits = B.CategoryBits | G.CategoryBits;
			        A.CategoryBits = C.CategoryBits | F.CategoryBits;
			        C.Enlarged = B.Enlarged || G.Enlarged;
			        A.Enlarged = C.Enlarged || F.Enlarged;
		        }
		        else
		        {
			        // Swap B and G
			        A.Child1 = iG;
			        C.Child2 = iB;

			        B.Parent = iC;
			        G.Parent = iA;

			        C.Aabb = aabbBF;

			        C.Height = (short)(1 + Math.Max(B.Height, F.Height));
			        A.Height = (short)(1 + Math.Max(C.Height, G.Height));
			        C.CategoryBits = B.CategoryBits | F.CategoryBits;
			        A.CategoryBits = C.CategoryBits | G.CategoryBits;
			        C.Enlarged = B.Enlarged || F.Enlarged;
			        A.Enlarged = C.Enlarged || G.Enlarged;
		        }
	        }
	        else if (C.Height == 0)
	        {
		        // C is a leaf and B is internal
		        Assert(B.Height > 0);

		        var iD = B.Child1;
		        var iE = B.Child2;
		        ref var D = ref _nodes[iD];
                ref var E = ref _nodes[iE];
		        Assert(0 <= iD && iD < Capacity);
                Assert(0 <= iE && iE < Capacity);

		        // Base cost
		        float costBase = Box2.Perimeter(B.Aabb);

		        // Cost of swapping C and D
		        var aabbCE = C.Aabb.Union(E.Aabb);
		        float costCD = Box2.Perimeter(aabbCE);

		        // Cost of swapping C and E
		        var aabbCD = C.Aabb.Union(D.Aabb);
		        float costCE = Box2.Perimeter(aabbCD);

		        if (costBase < costCD && costBase < costCE)
		        {
			        // Rotation does not improve cost
			        return;
		        }

		        if (costCD < costCE)
		        {
			        // Swap C and D
			        A.Child2 = iD;
			        B.Child1 = iC;

			        C.Parent = iB;
			        D.Parent = iA;

			        B.Aabb = aabbCE;

			        B.Height = (short)(1 + Math.Max(C.Height, E.Height));
			        A.Height = (short)(1 + Math.Max(B.Height, D.Height));
			        B.CategoryBits = C.CategoryBits | E.CategoryBits;
			        A.CategoryBits = B.CategoryBits | D.CategoryBits;
			        B.Enlarged = C.Enlarged || E.Enlarged;
			        A.Enlarged = B.Enlarged || D.Enlarged;
		        }
		        else
		        {
			        // Swap C and E
			        A.Child2 = iE;
			        B.Child2 = iC;

			        C.Parent = iB;
			        E.Parent = iA;

			        B.Aabb = aabbCD;
			        B.Height = (short)(1 + Math.Max(C.Height, D.Height));
			        A.Height = (short)(1 + Math.Max(B.Height, E.Height));
			        B.CategoryBits = C.CategoryBits | D.CategoryBits;
			        A.CategoryBits = B.CategoryBits | E.CategoryBits;
			        B.Enlarged = C.Enlarged || D.Enlarged;
			        A.Enlarged = B.Enlarged || E.Enlarged;
		        }
	        }
	        else
	        {
		        var iD = B.Child1;
		        var iE = B.Child2;
		        var iF = C.Child1;
		        var iG = C.Child2;

                ref var D = ref _nodes[iD];
                ref var E = ref _nodes[iE];
                ref var F = ref _nodes[iF];
                ref var G = ref _nodes[iG];

		        Assert(0 <= iD && iD < Capacity);
		        Assert(0 <= iE && iE < Capacity);
		        Assert(0 <= iF && iF < Capacity);
		        Assert(0 <= iG && iG < Capacity);

		        // Base cost
		        float areaB = Box2.Perimeter(B.Aabb);
		        float areaC = Box2.Perimeter(C.Aabb);
		        float costBase = areaB + areaC;
		        var bestRotation = RotateType.None;
		        float bestCost = costBase;

		        // Cost of swapping B and F
		        var aabbBG = B.Aabb.Union(G.Aabb);
		        float costBF = areaB + Box2.Perimeter(aabbBG);
		        if (costBF < bestCost)
		        {
			        bestRotation = RotateType.BF;
			        bestCost = costBF;
		        }

		        // Cost of swapping B and G
		        var aabbBF = B.Aabb.Union(F.Aabb);
		        float costBG = areaB + Box2.Perimeter(aabbBF);
		        if (costBG < bestCost)
		        {
			        bestRotation = RotateType.BG;
			        bestCost = costBG;
		        }

		        // Cost of swapping C and D
		        var aabbCE = C.Aabb.Union(E.Aabb);
		        float costCD = areaC + Box2.Perimeter(aabbCE);
		        if (costCD < bestCost)
		        {
			        bestRotation = RotateType.CD;
			        bestCost = costCD;
		        }

		        // Cost of swapping C and E
		        var aabbCD = C.Aabb.Union(D.Aabb);
		        float costCE = areaC + Box2.Perimeter(aabbCD);
		        if (costCE < bestCost)
		        {
			        bestRotation = RotateType.CE;
			        // bestCost = costCE;
		        }

		        switch (bestRotation)
		        {
			        case RotateType.None:
				        break;

			        case RotateType.BF:
				        A.Child1 = iF;
				        C.Child1 = iB;

				        B.Parent = iC;
				        F.Parent = iA;

				        C.Aabb = aabbBG;
				        C.Height = (short)(1 + Math.Max(B.Height, G.Height));
				        A.Height = (short)(1 + Math.Max(C.Height, F.Height));
				        C.CategoryBits = B.CategoryBits | G.CategoryBits;
				        A.CategoryBits = C.CategoryBits | F.CategoryBits;
				        C.Enlarged = B.Enlarged || G.Enlarged;
				        A.Enlarged = C.Enlarged || F.Enlarged;
				        break;

			        case RotateType.BG:
				        A.Child1 = iG;
				        C.Child2 = iB;

				        B.Parent = iC;
				        G.Parent = iA;

				        C.Aabb = aabbBF;
				        C.Height = (short)(1 + Math.Max(B.Height, F.Height));
				        A.Height = (short)(1 + Math.Max(C.Height, G.Height));
				        C.CategoryBits = B.CategoryBits | F.CategoryBits;
				        A.CategoryBits = C.CategoryBits | G.CategoryBits;
				        C.Enlarged = B.Enlarged || F.Enlarged;
				        A.Enlarged = C.Enlarged || G.Enlarged;
				        break;

			        case RotateType.CD:
				        A.Child2 = iD;
				        B.Child1 = iC;

				        C.Parent = iB;
				        D.Parent = iA;

				        B.Aabb = aabbCE;
				        B.Height = (short)(1 + Math.Max(C.Height, E.Height));
				        A.Height = (short)(1 + Math.Max(B.Height, D.Height));
				        B.CategoryBits = C.CategoryBits | E.CategoryBits;
				        A.CategoryBits = B.CategoryBits | D.CategoryBits;
				        B.Enlarged = C.Enlarged || E.Enlarged;
				        A.Enlarged = B.Enlarged || D.Enlarged;
				        break;

			        case RotateType.CE:
				        A.Child2 = iE;
				        B.Child2 = iC;

				        C.Parent = iB;
				        E.Parent = iA;

				        B.Aabb = aabbCD;
				        B.Height = (short)(1 + Math.Max(C.Height, D.Height));
				        A.Height = (short)(1 + Math.Max(B.Height, E.Height));
				        B.CategoryBits = C.CategoryBits | D.CategoryBits;
				        A.CategoryBits = B.CategoryBits | E.CategoryBits;
				        B.Enlarged = C.Enlarged || D.Enlarged;
				        A.Enlarged = B.Enlarged || E.Enlarged;
				        break;

			        default:
				        Assert(false);
				        break;
		        }
	        }
        }

        /// <summary>
        ///     Create a proxy in the tree as a leaf node.
        /// </summary>
        public Proxy CreateProxy(in Box2 aabb, uint categoryBits, T userData)
        {
            Assert(-PhysicsConstants.Huge < aabb.Left && aabb.Left < PhysicsConstants.Huge);
            Assert(-PhysicsConstants.Huge < aabb.Bottom && aabb.Bottom < PhysicsConstants.Huge);
            Assert(-PhysicsConstants.Huge < aabb.Right && aabb.Right < PhysicsConstants.Huge);
            Assert(-PhysicsConstants.Huge < aabb.Top && aabb.Top < PhysicsConstants.Huge);

            ref var node = ref AllocateNode(out var proxyId);

            node.Aabb = aabb;
            node.UserData = userData;
            node.CategoryBits = categoryBits;
            node.Height = 0;

            bool shouldRotate = true;
            InsertLeaf(proxyId, shouldRotate);

            ProxyCount += 1;

            return proxyId;
        }

        public void DestroyProxy(Proxy proxy)
        {
            Assert(0 <= proxy && proxy < Capacity);
            Assert(_nodes[proxy].IsLeaf);

            RemoveLeaf(proxy);
            FreeNode(proxy);

            Assert(ProxyCount > 0);
            ProxyCount -= 1;
        }

        public void MoveProxy(Proxy proxy, in Box2 aabb)
        {
            Assert(aabb.IsValid());
            Assert(aabb.Right - aabb.Left < PhysicsConstants.Huge);
            Assert(aabb.Top - aabb.Bottom < PhysicsConstants.Huge);
            Assert(0 <= proxy && proxy < Capacity);
            Assert(_nodes[proxy].IsLeaf);

            RemoveLeaf(proxy);

            _nodes[proxy].Aabb = aabb;

            bool shouldRotate = false;
            InsertLeaf(proxy, shouldRotate);
        }

        public void EnlargeProxy(Proxy proxy, Box2 aabb)
        {
            var nodes = _nodes;
            ref var node = ref _nodes[proxy];

            Assert(aabb.IsValid());
            Assert(aabb.Right - aabb.Left < PhysicsConstants.Huge);
            Assert(aabb.Top - aabb.Bottom < PhysicsConstants.Huge);
            Assert(0 <= proxy && proxy < Capacity);
            Assert(node.IsLeaf);

            // Caller must ensure this
            Assert(!nodes[proxy].Aabb.Contains(aabb));

            node.Aabb = aabb;

            var parentIndex = node.Parent;
            while (parentIndex != Proxy.Free)
            {
                ref var parentNode = ref nodes[parentIndex];

                bool changed = parentNode.Aabb.EnlargeAabb(aabb);
                parentNode.Enlarged = true;
                parentIndex = parentNode.Parent;

                if (!changed)
                {
                    break;
                }
            }

            while (parentIndex != Proxy.Free)
            {
                ref var parentNode = ref nodes[parentIndex];

                if (parentNode.Enlarged)
                {
                    // early out because this ancestor was previously ascended and marked as enlarged
                    break;
                }

                parentNode.Enlarged = true;
                parentIndex = parentNode.Parent;
            }
        }

        [return: MaybeNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetUserData(Proxy proxy)
        {
            return _nodes[proxy].UserData;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RemoveLeaf(Proxy leaf)
        {
            if (leaf == _root)
            {
                _root = Proxy.Free;
                return;
            }

            var nodes = _nodes;

            var parent = nodes[leaf].Parent;
            var grandParent = nodes[parent].Parent;
            Proxy sibling;
            if (nodes[parent].Child1 == leaf)
            {
                sibling = nodes[parent].Child2;
            }
            else
            {
                sibling = nodes[parent].Child1;
            }

            if (grandParent != Proxy.Free)
            {
                // Destroy parent and connect sibling to grandParent.
                if (nodes[grandParent].Child1 == parent)
                {
                    nodes[grandParent].Child1 = sibling;
                }
                else
                {
                    nodes[grandParent].Child2 = sibling;
                }
                nodes[sibling].Parent = grandParent;
                FreeNode(parent);

                // Adjust ancestor bounds.
                var index = grandParent;
                while (index != Proxy.Free)
                {
                    ref var node = ref nodes[index];
                    ref var child1 = ref nodes[node.Child1];
                    ref var child2 = ref nodes[node.Child2];

                    // Fast union using SSE
                    //__m128 aabb1 = _mm_load_ps(&child1->aabb.lowerBound.x);
                    //__m128 aabb2 = _mm_load_ps(&child2->aabb.lowerBound.x);
                    //__m128 lower = _mm_min_ps(aabb1, aabb2);
                    //__m128 upper = _mm_max_ps(aabb1, aabb2);
                    //__m128 aabb = _mm_shuffle_ps(lower, upper, _MM_SHUFFLE(3, 2, 1, 0));
                    //_mm_store_ps(&node->aabb.lowerBound.x, aabb);

                    node.Aabb = child1.Aabb.Union(child2.Aabb);
                    node.CategoryBits = child1.CategoryBits | child2.CategoryBits;
                    node.Height = (short)(1 + Math.Max(child1.Height, child2.Height));

                    index = node.Parent;
                }
            }
            else
            {
                _root = sibling;
                _nodes[sibling].Parent = Proxy.Free;
                FreeNode(parent);
            }
        }

        private void InsertLeaf(Proxy leaf, bool shouldRotate)
        {
            if (_root == Proxy.Free)
	        {
		        _root = leaf;
		        _nodes[_root].Parent = Proxy.Free;
		        return;
	        }

	        // Stage 1: find the best sibling for this node
	        ref var leafAABB = ref _nodes[leaf].Aabb;
	        var sibling = FindBestSibling(leafAABB);

	        // Stage 2: create a new parent for the leaf and sibling
	        ref var oldParent = ref _nodes[sibling].Parent;
	        ref var node = ref AllocateNode(out var newParent);

	        // warning: node pointer can change after allocation
	        var nodes = _nodes;
	        node.Parent = oldParent;
            node.UserData = default!;
            node.Aabb = leafAABB.Union(nodes[sibling].Aabb);
            node.CategoryBits = nodes[leaf].CategoryBits | nodes[sibling].CategoryBits;
            node.Height = (short)(nodes[sibling].Height + 1);

	        if (oldParent != Proxy.Free)
	        {
		        // The sibling was not the root.
		        if (nodes[oldParent].Child1 == sibling)
		        {
			        nodes[oldParent].Child1 = newParent;
		        }
		        else
		        {
			        nodes[oldParent].Child2 = newParent;
		        }

		        node.Child1 = sibling;
                node.Child2 = leaf;
		        nodes[sibling].Parent = newParent;
		        nodes[leaf].Parent = newParent;
	        }
	        else
	        {
		        // The sibling was the root.
                node.Child1 = sibling;
                node.Child2 = leaf;
		        nodes[sibling].Parent = newParent;
		        nodes[leaf].Parent = newParent;
		        _root = newParent;
	        }

	        // Stage 3: walk back up the tree fixing heights and AABBs
	        var index = nodes[leaf].Parent;
	        while (index != Proxy.Free)
            {
                ref var indexNode = ref nodes[index];

		        var child1 = indexNode.Child1;
		        var child2 = indexNode.Child2;

                ref var childNode1 = ref nodes[child1];
                ref var childNode2 = ref nodes[child2];

		        Assert(child1 != Proxy.Free);
		        Assert(child2 != Proxy.Free);

                indexNode.Aabb = childNode1.Aabb.Union(childNode2.Aabb);
                indexNode.CategoryBits = childNode1.CategoryBits | childNode2.CategoryBits;
                indexNode.Height = (short)(1 + Math.Max(childNode1.Height, childNode2.Height));
                indexNode.Enlarged = childNode1.Enlarged || childNode2.Enlarged;

		        if (shouldRotate)
		        {
			        RotateNodes(index);
		        }

		        index = indexNode.Parent;
	        }
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

        [MethodImpl(MethodImplOptions.NoInlining)]
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

                indexNode.Height = (short)(Math.Max(child1Node.Height, child2Node.Height) + 1);
                indexNode.Aabb = child1Node.Aabb.Union(child2Node.Aabb);

                if (index == indexNode.Parent)
                    throw new Exception($"Infinite loop in B2DynamicTree.Balance(). Trace: {Environment.StackTrace}");

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
            ref var baseRef = ref _nodes[0];
            ref var a = ref Unsafe.Add(ref baseRef, iA);

            if (a.IsLeaf || a.Height < 2)
            {
                return iA;
            }

            var iB = a.Child1;
            var iC = a.Child2;
            Assert(iA != iB);
            Assert(iA != iC);
            Assert(iB != iC);

            ref var b = ref Unsafe.Add(ref baseRef, iB);
            ref var c = ref Unsafe.Add(ref baseRef, iC);

            var balance = c.Height - b.Height;

            // Rotate C up
            if (balance > 1)
            {
                var iF = c.Child1;
                var iG = c.Child2;
                Assert(iC != iF);
                Assert(iC != iG);
                Assert(iF != iG);

                ref var f = ref Unsafe.Add(ref baseRef, iF);
                ref var g = ref Unsafe.Add(ref baseRef, iG);

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
                    ref var cParent = ref Unsafe.Add(ref baseRef, c.Parent);
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

                    a.Height = (short) (Math.Max(b.Height, g.Height) + 1);
                    c.Height = (short) (Math.Max(a.Height, f.Height) + 1);
                }
                else
                {
                    c.Child2 = iG;
                    a.Child2 = iF;
                    f.Parent = iA;
                    a.Aabb = b.Aabb.Union(f.Aabb);
                    c.Aabb = a.Aabb.Union(g.Aabb);

                    a.Height = (short)(Math.Max(b.Height, f.Height) + 1);
                    c.Height = (short)(Math.Max(a.Height, g.Height) + 1);
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

                ref var d = ref Unsafe.Add(ref baseRef, iD);
                ref var e = ref Unsafe.Add(ref baseRef, iE);

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
                    ref var bParent = ref Unsafe.Add(ref baseRef, b.Parent);
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

                    a.Height = (short)(Math.Max(c.Height, e.Height) + 1);
                    b.Height = (short)(Math.Max(a.Height, d.Height) + 1);
                }
                else
                {
                    b.Child2 = iE;
                    a.Child1 = iD;
                    d.Parent = iA;
                    a.Aabb = c.Aabb.Union(d.Aabb);
                    b.Aabb = a.Aabb.Union(e.Aabb);

                    a.Height = (short)(Math.Max(c.Height, d.Height) + 1);
                    b.Height = (short)(Math.Max(a.Height, e.Height) + 1);
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
        [MethodImpl(MethodImplOptions.NoInlining)]
        private int ComputeHeight(Proxy proxy)
        {
            Assert(0 <= proxy && proxy < Capacity);
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RebuildBottomUp()
        {
            Span<Proxy> newNodes = stackalloc Proxy[Capacity];
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

                if (node.IsLeaf)
                {
                    node.Parent = Proxy.Free;
                    newNodes[count] = new Proxy(i);
                    ++count;
                }
                else
                {
                    FreeNode(new Proxy(i));
                }
            }

            while (count > 1)
            {
                float minCost = float.MaxValue;
                int iMin = -1, jMin = -1;

                for (var i = 0; i < count; ++i)
                {
                    var aabbi = _nodes[newNodes[i]].Aabb;

                    for (var j = i + 1; j < count; ++j)
                    {
                        var aabbj = _nodes[newNodes[j]].Aabb;
                        var b = aabbi.Union(aabbj);
                        float cost = Box2.Perimeter(b);
                        if (cost < minCost)
                        {
                            iMin = i;
                            jMin = j;
                            minCost = cost;
                        }
                    }
                }

                var index1 = newNodes[iMin];
                var index2 = newNodes[jMin];
                ref var child1 = ref _nodes[index1];
                ref var child2 = ref _nodes[index2];

                ref var parent = ref AllocateNode(out var parentIndex);
                parent.Child1 = index1;
                parent.Child2 = index2;
                parent.Aabb = child1.Aabb.Union(child2.Aabb);
                parent.CategoryBits = child1.CategoryBits | child2.CategoryBits;
                parent.Height = (short)(1 + Math.Max(child1.Height, child2.Height));
                parent.Parent = Proxy.Free;

                child1.Parent = parentIndex;
                child2.Parent = parentIndex;

                newNodes[jMin] = newNodes[count - 1];
                newNodes[iMin] = parentIndex;
                --count;
            }

            _root = newNodes[0];

            Validate();
        }

        public void ShiftOrigin(Vector2 newOrigin)
        {
            // shift all AABBs
            for (var i = 0; i < _nodes.Length; i++)
            {
                ref var node = ref _nodes[i];
                var lb = node.Aabb.BottomLeft;
                var tr = node.Aabb.TopRight;

                node.Aabb = new Box2(lb - newOrigin, tr - newOrigin);
            }
        }

        #region Queries

        public delegate bool TreeQueryCallback(Proxy proxyId, T userData);

        public void Query(Box2 aabb, uint maskBits, TreeQueryCallback callback)
        {
            var stack = new GrowableStack<Proxy>(stackalloc Proxy[TreeStackSize]);
            stack.Push(_root);
            ref var baseRef = ref _nodes[0];

            while (stack.GetCount() > 0)
            {
                var nodeId = stack.Pop();
                if (nodeId == Proxy.Free)
                {
                    continue;
                }

                ref var node = ref Unsafe.Add(ref baseRef, nodeId);

                if (node.Aabb.Intersects(aabb) && (node.CategoryBits & maskBits) != 0)
                {
                    if (node.IsLeaf)
                    {
                        // callback to user code with proxy id
                        bool proceed = callback(nodeId, node.UserData);
                        if (proceed == false)
                        {
                            return;
                        }
                    }
                    else
                    {
                        Assert(stack.GetCount() < TreeStackSize - 1);
                        if (stack.GetCount() < TreeStackSize - 1)
                        {
                            stack.Push(node.Child1);
                            stack.Push(node.Child2);
                        }
                    }
                }
            }
        }

#if !B2_TREE_HEURISTIC

        // Median split heuristic
        public int PartitionMid(Proxy[] indices, Vector2[] centers, int count)
        {
            // Handle trivial case
            if (count <= 2)
            {
                return count / 2;
            }

            // erin: todo SIMD?
            ref var firstCenter = ref centers[0];
            var lowerBound = firstCenter;
            var upperBound = firstCenter;

            for (var i = 1; i < count; ++i)
            {
                var offsetCenter = Unsafe.Add(ref firstCenter, i);
                lowerBound = Vector2.Min(lowerBound, offsetCenter);
                upperBound = Vector2.Max(upperBound, offsetCenter);
            }

            var d = Vector2.Subtract(upperBound, lowerBound);
            var c = new Vector2(0.5f * (lowerBound.X + upperBound.X), 0.5f * (lowerBound.Y + upperBound.Y));

            // Partition longest axis using the Hoare partition scheme
            // https://en.wikipedia.org/wiki/Quicksort
            // https://nicholasvadivelu.com/2021/01/11/array-partition/
            int i1 = 0, i2 = count;

            if (d.X > d.Y)
            {
                float pivot = c.X;

                while (i1 < i2)
                {
                    while (i1 < i2 && centers[i1].X < pivot)
                    {
                        i1 += 1;
                    }

                    while (i1 < i2 && centers[i2 - 1].X >= pivot)
                    {
                        i2 -= 1;
                    }

                    if (i1 < i2)
                    {
                        // Swap indices
                        {
                            (indices[i1], indices[i2 - 1]) = (indices[i2 - 1], indices[i1]);
                        }

                        // Swap centers
                        {
                            (centers[i1], centers[i2 - 1]) = (centers[i2 - 1], centers[i1]);
                        }

                        i1 += 1;
                        i2 -= 1;
                    }
                }
            }
            else
            {
                float pivot = c.Y;

                while (i1 < i2)
                {
                    while (i1 < i2 && centers[i1].Y < pivot)
                    {
                        i1 += 1;
                    }

                    while (i1 < i2 && centers[i2 - 1].Y >= pivot)
                    {
                        i2 -= 1;
                    }

                    if (i1 < i2)
                    {
                        // Swap indices
                        {
                            (indices[i1], indices[i2 - 1]) = (indices[i2 - 1], indices[i1]);
                        }

                        // Swap centers
                        {
                            (centers[i1], centers[i2 - 1]) = (centers[i2 - 1], centers[i1]);
                        }

                        i1 += 1;
                        i2 -= 1;
                    }
                }
            }

            Assert(i1 == i2);

            if (i1 > 0 && i1 < count)
            {
                return i1;
            }
            else
            {
                return count / 2;
            }
        }

#else

        private struct TreeBin
        {
            public Box2 aabb;
            public int count;
        }

        private struct TreePlane
        {
            public Box2 leftAABB;
            public Box2 rightAABB;
            public int leftCount;
            public int rightCount;
        }

        // "On Fast Construction of SAH-based Bounding Volume Hierarchies" by Ingo Wald
        // Returns the left child count
        public int PartitionSAH(Proxy[] indices, int[] binIndices, Box2[] boxes, int count)
        {
	        Assert(count > 0);

	        var bins = new TreeBin[B2_Bin_Count];
	        var planes = new TreePlane[B2_Bin_Count - 1];

	        var center = boxes[0].Center;
            var centroidAABB = new Box2(center, center);

	        for (var i = 1; i < count; ++i)
	        {
		        center = boxes[i].Center;
		        centroidAABB.BottomLeft = Vector2.Min(centroidAABB.BottomLeft, center);
		        centroidAABB.TopRight = Vector2.Max(centroidAABB.TopRight, center);
	        }

	        var d = Vector2.Subtract(centroidAABB.TopRight, centroidAABB.BottomLeft);

	        // Find longest axis
	        int axisIndex;
	        float invD;
	        if (d.X > d.Y)
	        {
		        axisIndex = 0;
		        invD = d.X;
	        }
	        else
	        {
		        axisIndex = 1;
		        invD = d.Y;
	        }

	        invD = invD > 0.0f ? 1.0f / invD : 0.0f;

	        // Initialize bin bounds and count
	        for (int i = 0; i < B2_Bin_Count; ++i)
	        {
		        bins[i].aabb.BottomLeft = new Vector2(float.MaxValue, float.MaxValue);
		        bins[i].aabb.TopRight = new Vector2(float.MinValue, float.MinValue);
		        bins[i].count = 0;
	        }

	        // Assign boxes to bins and compute bin boxes
	        // TODO_ERIN optimize
	        float binCount = B2_Bin_Count;
	        var lowerBoundArray = new float[2] {centroidAABB.Left, centroidAABB.Bottom};
	        float minC = lowerBoundArray[axisIndex];
	        for (var i = 0; i < count; ++i)
	        {
		        var c = boxes[i].Center;
		        var cArray = new float[2]{c.X, c.Y};
		        var binIndex = (int)(binCount * (cArray[axisIndex] - minC) * invD);
		        binIndex = Math.Clamp(binIndex, 0, B2_Bin_Count - 1);
		        binIndices[i] = binIndex;
		        bins[binIndex].count += 1;
		        bins[binIndex].aabb = Box2.Union(bins[binIndex].aabb, boxes[i]);
	        }

	        var planeCount = B2_Bin_Count - 1;

	        // Prepare all the left planes, candidates for left child
	        planes[0].leftCount = bins[0].count;
	        planes[0].leftAABB = bins[0].aabb;
	        for (var i = 1; i < planeCount; ++i)
	        {
		        planes[i].leftCount = planes[i - 1].leftCount + bins[i].count;
		        planes[i].leftAABB = Box2.Union(planes[i - 1].leftAABB, bins[i].aabb);
	        }

	        // Prepare all the right planes, candidates for right child
	        planes[planeCount - 1].rightCount = bins[planeCount].count;
	        planes[planeCount - 1].rightAABB = bins[planeCount].aabb;
	        for (var i = planeCount - 2; i >= 0; --i)
	        {
		        planes[i].rightCount = planes[i + 1].rightCount + bins[i + 1].count;
		        planes[i].rightAABB = Box2.Union(planes[i + 1].rightAABB, bins[i + 1].aabb);
	        }

	        // Find best split to minimize SAH
	        float minCost = float.MaxValue;
	        var bestPlane = 0;
	        for (var i = 0; i < planeCount; ++i)
	        {
		        float leftArea = Box2.Perimeter(planes[i].leftAABB);
		        float rightArea = Box2.Perimeter(planes[i].rightAABB);
		        int leftCount = planes[i].leftCount;
		        int rightCount = planes[i].rightCount;

		        float cost = leftCount * leftArea + rightCount * rightArea;
		        if (cost < minCost)
		        {
			        bestPlane = i;
			        minCost = cost;
		        }
	        }

	        // Partition node indices and boxes using the Hoare partition scheme
	        // https://en.wikipedia.org/wiki/Quicksort
	        // https://nicholasvadivelu.com/2021/01/11/array-partition/
	        int i1 = 0, i2 = count;
	        while (i1 < i2)
	        {
		        while (i1 < i2 && binIndices[i1] < bestPlane)
		        {
			        i1 += 1;
		        };

		        while (i1 < i2 && binIndices[i2 - 1] >= bestPlane)
		        {
			        i2 -= 1;
		        };

		        if (i1 < i2)
		        {
			        // Swap indices
			        {
				        var temp = indices[i1];
				        indices[i1] = indices[i2 - 1];
				        indices[i2 - 1] = temp;
			        }

			        // Swap boxes
			        {
				        Box2 temp = boxes[i1];
				        boxes[i1] = boxes[i2 - 1];
				        boxes[i2 - 1] = temp;
			        }

			        i1 += 1;
			        i2 -= 1;
		        }
	        }
	        Assert(i1 == i2);

	        if (i1 > 0 && i1 < count)
	        {
		        return i1;
	        }
	        else
	        {
		        return count / 2;
	        }
        }

#endif

        private struct RebuildItem
        {
            public Proxy NodeIndex;
            public int ChildCount;

            // Leaf indices
            public int StartIndex;
            public int SplitIndex;
            public int EndIndex;
        }

        // Returns root node index
        public Proxy BuildTree(int leafCount)
        {
            var nodes = _nodes;
	        var leafIndices = LeafIndices;

	        if (leafCount == 1)
	        {
		        nodes[leafIndices[0]].Parent = Proxy.Free;
		        return leafIndices[0];
	        }

#if !B2_TREE_HEURISTIC
            var leafCenters = LeafCenters;
#else
            var leafBoxes = LeafBoxes;
            var binIndices = BinIndices;
#endif

	        var stack = new GrowableStack<RebuildItem>(stackalloc RebuildItem[TreeStackSize]);
	        var top = 0;
            AllocateNode(out var topProxy);

            stack.Push(new RebuildItem()
            {
                NodeIndex = topProxy,
                ChildCount = -1,
                StartIndex = 0,
                EndIndex = leafCount,
#if !B2_TREE_HEURISTIC
                SplitIndex = PartitionMid(leafIndices, leafCenters, leafCount),
#else
                SplitIndex = PartitionSAH(leafIndices, binIndices, leafBoxes, leafCount),
#endif
            });

	        while (true)
	        {
		        ref var item = ref stack[top];

		        item.ChildCount += 1;

		        if (item.ChildCount == 2)
		        {
			        // This internal node has both children established

			        if (top == 0)
			        {
				        // all done
				        break;
			        }

			        ref var parentItem = ref stack[top - 1];
			        ref var parentNode = ref nodes[parentItem.NodeIndex];

			        if (parentItem.ChildCount == 0)
			        {
				        Assert(parentNode.Child1 == Proxy.Free);
				        parentNode.Child1 = item.NodeIndex;
			        }
			        else
			        {
				        Assert(parentItem.ChildCount == 1);
				        Assert(parentNode.Child2 == Proxy.Free);
				        parentNode.Child2 = item.NodeIndex;
			        }

			        ref var node = ref nodes[item.NodeIndex];

			        Assert(node.Parent == Proxy.Free);
			        node.Parent = parentItem.NodeIndex;

			        Assert(node.Child1 != Proxy.Free);
			        Assert(node.Child2 != Proxy.Free);
                    {
                        ref var child1 = ref nodes[node.Child1];
                        ref var child2 = ref nodes[node.Child2];

                        node.Aabb = Box2.Union(child1.Aabb, child2.Aabb);
                        node.Height = (short) (1 + Math.Max(child1.Height, child2.Height));
                        node.CategoryBits = child1.CategoryBits | child2.CategoryBits;
                    }

			        // Pop stack
			        top -= 1;
		        }
		        else
		        {
			        int startIndex, endIndex;
			        if (item.ChildCount == 0)
			        {
				        startIndex = item.StartIndex;
				        endIndex = item.SplitIndex;
			        }
			        else
			        {
				        Assert(item.ChildCount == 1);
				        startIndex = item.SplitIndex;
				        endIndex = item.EndIndex;
			        }

			        int count = endIndex - startIndex;

			        if (count == 1)
			        {
				        var childIndex = leafIndices[startIndex];
				        ref var node = ref nodes[item.NodeIndex];

				        if (item.ChildCount == 0)
				        {
					        Assert(node.Child1 == Proxy.Free);
					        node.Child1 = childIndex;
				        }
				        else
				        {
					        Assert(item.ChildCount == 1);
					        Assert(node.Child2 == Proxy.Free);
					        node.Child2 = childIndex;
				        }

				        ref var childNode = ref nodes[childIndex];
				        Assert(childNode.Parent == Proxy.Free);
				        childNode.Parent = item.NodeIndex;
			        }
			        else
			        {
				        Assert(count > 0);
				        Assert(top < TreeStackSize);

				        top += 1;
				        ref var newItem = ref stack[top];
                        AllocateNode(out var nodeIndex);
				        newItem.NodeIndex = nodeIndex;
				        newItem.ChildCount = -1;
				        newItem.StartIndex = startIndex;
				        newItem.EndIndex = endIndex;
        #if !B2_TREE_HEURISTIC
				        newItem.SplitIndex = PartitionMid(leafIndices[startIndex..], leafCenters[startIndex..], count);
        #else
				        newItem.SplitIndex =
					        PartitionSAH(leafIndices[startIndex..], binIndices[startIndex..], leafBoxes[startIndex..], count);
        #endif
				        newItem.SplitIndex += startIndex;
			        }
		        }
	        }

	        ref var rootNode = ref nodes[stack[0].NodeIndex];
	        Assert(rootNode.Parent == Proxy.Free);
	        Assert(rootNode.Child1 != Proxy.Free);
	        Assert(rootNode.Child2 != Proxy.Free);

            {
                ref var child1 = ref nodes[rootNode.Child1];
                ref var child2 = ref nodes[rootNode.Child2];

                rootNode.Aabb = Box2.Union(child1.Aabb, child2.Aabb);
                rootNode.Height = (short) (1 + Math.Max(child1.Height, child2.Height));
                rootNode.CategoryBits = child1.CategoryBits | child2.CategoryBits;
            }

	        return stack[0].NodeIndex;
        }

        // Not safe to access tree during this operation because it may grow
        public int Rebuild(bool fullBuild)
        {
	        var proxyCount = ProxyCount;
	        if (proxyCount == 0)
	        {
		        return 0;
	        }

	        // Ensure capacity for rebuild space
	        if (proxyCount > RebuildCapacity)
	        {
		        var newCapacity = proxyCount + proxyCount / 2;

                Array.Resize(ref LeafIndices, newCapacity);

#if !B2_TREE_HEURISTIC
                Array.Resize(ref LeafCenters, newCapacity);
#else
                Array.Resize(ref LeafBoxes, newCapacity);
                Array.Resize(ref BinIndices, newCapacity);
#endif

		        RebuildCapacity = newCapacity;
	        }

	        var leafCount = 0;
	        var stack = new GrowableStack<Proxy>(stackalloc Proxy[TreeStackSize]);

	        var nodeIndex = _root;
            ref var baseRef = ref _nodes[0];
	        var node = baseRef;

	        // These are the nodes that get sorted to rebuild the tree.
	        // I'm using indices because the node pool may grow during the build.
	        var leafIndices = LeafIndices;

#if !B2_TREE_HEURISTIC
            var leafCenters = LeafCenters;
#else
            var leafBoxes = LeafBoxes;
#endif

	        // Gather all proxy nodes that have grown and all internal nodes that haven't grown. Both are
	        // considered leaves in the tree rebuild.
	        // Free all internal nodes that have grown.
	        // todo use a node growth metric instead of simply enlarged to reduce rebuild size and frequency
	        // this should be weighed against b2_aabbMargin
	        while (true)
	        {
		        if (node.Height == 0 || (!node.Enlarged && !fullBuild))
		        {
			        leafIndices[leafCount] = nodeIndex;
        #if !B2_TREE_HEURISTIC
			        leafCenters[leafCount] = node.Aabb.Center;
        #else
			        leafBoxes[leafCount] = node.Aabb;
        #endif
			        leafCount += 1;

			        // Detach
			        node.Parent = Proxy.Free;
		        }
		        else
		        {
			        var doomedNodeIndex = nodeIndex;

			        // Handle children
			        nodeIndex = node.Child1;

			        Assert(stack.GetCount() < TreeStackSize);
			        if (stack.GetCount() < TreeStackSize)
			        {
                        stack.Push(node.Child2);
			        }

                    node = Unsafe.Add(ref baseRef, nodeIndex);

			        // Remove doomed node
			        FreeNode(doomedNodeIndex);

			        continue;
		        }

		        if (stack.GetCount() == 0)
		        {
			        break;
		        }

                nodeIndex = stack.Pop();
		        node = Unsafe.Add(ref baseRef, nodeIndex);
	        }

        #if B2_VALIDATE
            int capacity = Capacity;
	        for (int32_t i = 0; i < capacity; ++i)
	        {
		        if (nodes[i].Height >= 0)
		        {
			        Assert(!nodes[i].Enlarged);
		        }
	        }
        #endif

	        Assert(leafCount <= proxyCount);

	        _root = BuildTree(leafCount);

	        Validate();

	        return leafCount;
        }

        #endregion

        private static readonly QueryCallback<QueryCallback> EasyQueryCallback =
            (ref QueryCallback callback, Proxy proxy) => callback(proxy);

        public void Query(QueryCallback callback, in Box2 aabb)
        {
            Query(ref callback, EasyQueryCallback, aabb);
        }

        public void Query<TState>(ref TState state, QueryCallback<TState> callback, in Box2 aabb)
        {
            var stack = new GrowableStack<Proxy>(stackalloc Proxy[256]);
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

        public delegate void FastQueryCallback(ref T userData);

        public void FastQuery(ref Box2 aabb, FastQueryCallback callback)
        {
            var stack = new GrowableStack<Proxy>(stackalloc Proxy[256]);
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
                ref var nodeAabb = ref node.Aabb;
                if (nodeAabb.Intersects(aabb))
                {
                    if (node.IsLeaf)
                    {
                        callback(ref node.UserData);
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

        internal delegate float RayCallback(RayCastInput input, T context, ref WorldRayCastContext state);

        internal void RayCastNew(RayCastInput input, long mask, ref WorldRayCastContext state, RayCallback callback)
        {
            var p1 = input.Origin;
            var d = input.Translation;

            var r = d.Normalized();

	        // v is perpendicular to the segment.
	        var v = Vector2Helpers.Cross(1.0f, r);
            var abs_v = Vector2.Abs(v);

	        // Separating axis for segment (Gino, p80).
	        // |dot(v, p1 - c)| > dot(|v|, h)

	        float maxFraction = input.MaxFraction;

	        var p2 = Vector2.Add(p1, maxFraction * d);

	        // Build a bounding box for the segment.
	        var segmentAABB = new Box2(Vector2.Min(p1, p2), Vector2.Max(p1, p2));

	        var stack = new GrowableStack<Proxy>(stackalloc Proxy[256]);
            ref var baseRef = ref _nodes[0];
	        stack.Push(_root);

	        var subInput = input;

	        while (stack.GetCount() > 0)
            {
                var nodeId = stack.Pop();

		        if (nodeId == Proxy.Free)
		        {
			        continue;
		        }

		        var node = Unsafe.Add(ref baseRef, nodeId);

		        if (!node.Aabb.Intersects(segmentAABB))// || ( node->categoryBits & maskBits ) == 0 )
		        {
			        continue;
		        }

		        // Separating axis for segment (Gino, p80).
		        // |dot(v, p1 - c)| > dot(|v|, h)
		        // radius extension is added to the node in this case
		        var c = node.Aabb.Center;
		        var h = node.Aabb.Extents;
		        float term1 = MathF.Abs(Vector2.Dot(v, Vector2.Subtract(p1, c)));
		        float term2 = Vector2.Dot(abs_v, h);
		        if ( term2 < term1 )
		        {
			        continue;
		        }

		        if (node.IsLeaf)
		        {
			        subInput.MaxFraction = maxFraction;

			        float value = callback(subInput, node.UserData, ref state);

			        if (value == 0.0f)
			        {
				        // The client has terminated the ray cast.
				        return;
			        }

			        if (0.0f < value && value < maxFraction)
			        {
				        // Update segment bounding box.
				        maxFraction = value;
				        p2 = Vector2.Add(p1, maxFraction * d);
				        segmentAABB.BottomLeft = Vector2.Min( p1, p2 );
				        segmentAABB.TopRight = Vector2.Max( p1, p2 );
			        }
		        }
		        else
                {
                    var stackCount = stack.GetCount();
			        Assert( stackCount < 256 - 1 );
			        if (stackCount < 256 - 1 )
			        {
				        // TODO_ERIN just put one node on the stack, continue on a child node
				        // TODO_ERIN test ordering children by nearest to ray origin
				        stack.Push(node.Child1);
				        stack.Push(node.Child2);
			        }
		        }
	        }
        }

        /// This function receives clipped ray-cast input for a proxy. The function
        /// returns the new ray fraction.
        /// - return a value of 0 to terminate the ray-cast
        /// - return a value less than input->maxFraction to clip the ray
        /// - return a value of input->maxFraction to continue the ray cast without clipping
        internal delegate float TreeShapeCastCallback(ShapeCastInput input, T userData, ref WorldRayCastContext state);

        internal void ShapeCast(ShapeCastInput input, long maskBits, TreeShapeCastCallback callback, ref WorldRayCastContext state)
        {
	        if (input.Count == 0)
	        {
		        return;
	        }

            var originAABB = new Box2(input.Points[0], input.Points[0]);

	        for (var i = 1; i < input.Count; ++i)
	        {
		        originAABB.BottomLeft = Vector2.Min(originAABB.BottomLeft, input.Points[i]);
		        originAABB.TopRight = Vector2.Max(originAABB.TopRight, input.Points[i]);
	        }

	        var radius = new Vector2(input.Radius, input.Radius);

	        originAABB.BottomLeft = Vector2.Subtract(originAABB.BottomLeft, radius);
	        originAABB.TopRight = Vector2.Add(originAABB.TopRight, radius );

	        var p1 = originAABB.Center;
	        var extension = originAABB.Extents;

	        // v is perpendicular to the segment.
	        var r = input.Translation;
	        var v = Vector2Helpers.Cross(1.0f, r);
	        var abs_v = Vector2.Abs(v);

	        // Separating axis for segment (Gino, p80).
	        // |dot(v, p1 - c)| > dot(|v|, h)

	        float maxFraction = input.MaxFraction;

	        // Build total box for the shape cast
	        var t = Vector2.Multiply(maxFraction, input.Translation);

            var totalAABB = new Box2(
		        Vector2.Min(originAABB.BottomLeft, Vector2.Add(originAABB.BottomLeft, t)),
		        Vector2.Max(originAABB.TopRight, Vector2.Add( originAABB.TopRight, t))
	        );

	        var subInput = input;

            ref var baseRef = ref _nodes[0];
            var stack = new GrowableStack<Proxy>(stackalloc Proxy[256]);
	        stack.Push(_root);

	        while (stack.GetCount() > 0)
            {
		        var nodeId = stack.Pop();

		        if (nodeId == Proxy.Free)
		        {
			        continue;
		        }

                var node = Unsafe.Add(ref baseRef, nodeId);
		        if (!node.Aabb.Intersects(totalAABB))// || ( node->categoryBits & maskBits ) == 0 )
		        {
			        continue;
		        }

		        // Separating axis for segment (Gino, p80).
		        // |dot(v, p1 - c)| > dot(|v|, h)
		        // radius extension is added to the node in this case
		        var c = node.Aabb.Center;
		        var h = Vector2.Add(node.Aabb.Extents, extension);
		        float term1 = MathF.Abs(Vector2.Dot(v, Vector2.Subtract(p1, c)));
		        float term2 = Vector2.Dot(abs_v, h);
		        if (term2 < term1)
		        {
			        continue;
		        }

		        if (node.IsLeaf)
		        {
			        subInput.MaxFraction = maxFraction;

			        float value = callback(subInput, node.UserData, ref state);

			        if ( value == 0.0f )
			        {
				        // The client has terminated the ray cast.
				        return;
			        }

			        if (0.0f < value && value < maxFraction)
			        {
				        // Update segment bounding box.
				        maxFraction = value;
				        t = Vector2.Multiply(maxFraction, input.Translation);
				        totalAABB.BottomLeft = Vector2.Min( originAABB.BottomLeft, Vector2.Add(originAABB.BottomLeft, t));
				        totalAABB.TopRight = Vector2.Max( originAABB.TopRight, Vector2.Add( originAABB.TopRight, t));
			        }
		        }
		        else
		        {
                    var stackCount = stack.GetCount();
			        Assert(stackCount < 256 - 1);

			        if (stackCount < 255)
			        {
				        // TODO_ERIN just put one node on the stack, continue on a child node
				        // TODO_ERIN test ordering children by nearest to ray origin
				        stack.Push(node.Child1);
				        stack.Push(node.Child2);
			        }
		        }
	        }
        }

        public void RayCast(RayQueryCallback callback, in Ray input)
        {
            RayCast(ref callback, EasyRayQueryCallback, input);
        }

        public void RayCast<TState>(ref TState state, RayQueryCallback<TState> callback, in Ray input)
        {
            // NOTE: This is not Box2D's normal ray cast function, since our rays have infinite length.

            var stack = new GrowableStack<Proxy>(stackalloc Proxy[256]);

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
        public void ValidateStructure(Proxy proxy)
        {
            if (proxy == Proxy.Free)
            {
                return;
            }

            if (proxy == _root)
            {
                Assert(_nodes[proxy].Parent == Proxy.Free);
            }

            ref var node = ref _nodes[proxy];

            var child1 = node.Child1;
            var child2 = node.Child2;

            if (node.IsLeaf)
            {
                Assert(child1 == Proxy.Free);
                Assert(child2 == Proxy.Free);
                Assert(node.Height == 0);
                return;
            }

            Assert(0 <= child1 && child1 < Capacity);
            Assert(0 <= child2 && child2 < Capacity);

            Assert(_nodes[child1].Parent == proxy);
            Assert(_nodes[child2].Parent == proxy);

            if (_nodes[child1].Enlarged || _nodes[child2].Enlarged)
            {
                Assert(node.Enlarged);
            }

            ValidateStructure(child1);
            ValidateStructure(child2);
        }

        [Conditional("DEBUG_DYNAMIC_TREE")]
        public void ValidateMetrics(Proxy proxy)
        {
            if (proxy == Proxy.Free)
            {
                return;
            }

            ref var node = ref _nodes[proxy];

            var child1 = node.Child1;
            var child2 = node.Child2;

            if (node.IsLeaf)
            {
                Assert(child1 == Proxy.Free);
                Assert(child2 == Proxy.Free);
                Assert(node.Height == 0);
                return;
            }

            Assert(0 <= child1 && child1 < Capacity);
            Assert(0 <= child2 && child2 < Capacity);

            var height1 = _nodes[child1].Height;
            var height2 = _nodes[child2].Height;
            int height;
            height = 1 + Math.Max(height1, height2);
            Assert(node.Height == height);

            // b2AABB aabb = b2AABB_Union(tree->nodes[child1].aabb, tree->nodes[child2].aabb);

            Assert(node.Aabb.Contains(_nodes[child1].Aabb));
            Assert(node.Aabb.Contains(_nodes[child2].Aabb));

            // Assert(aabb.lowerBound.x == node->aabb.lowerBound.x);
            // Assert(aabb.lowerBound.y == node->aabb.lowerBound.y);
            // Assert(aabb.upperBound.x == node->aabb.upperBound.x);
            // Assert(aabb.upperBound.y == node->aabb.upperBound.y);

            uint categoryBits = _nodes[child1].CategoryBits | _nodes[child2].CategoryBits;
            Assert(node.CategoryBits == categoryBits);

            ValidateMetrics(child1);
            ValidateMetrics(child2);
        }

        [Conditional("DEBUG_DYNAMIC_TREE")]
        private void Validate()
        {
            if (_root == Proxy.Free)
            {
                return;
            }

            ValidateStructure(_root);
            ValidateMetrics(_root);

            var freeCount = 0;
            var freeIndex = _freeList;
            while (freeIndex != Proxy.Free)
            {
                Assert(0 <= freeIndex && freeIndex < Capacity);
                freeIndex = _nodes[freeIndex].Next;
                ++freeCount;
            }

            var height = Height;
            var computedHeight = ComputeHeight();
            Assert(height == computedHeight);

            Assert(NodeCount + freeCount == Capacity);
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
