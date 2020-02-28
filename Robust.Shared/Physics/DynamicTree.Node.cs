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

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{

    public partial class DynamicTree<T>
    {

        public struct Node
        {

            public Box2 Aabb;

            public Proxy Parent;

            public Proxy Child1, Child2;

            public int Height;

            public T Item;

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
                            ? $"Leaf: {Item}"
                            : $"Leaf (invalid height of {Height}): {Item}"
                        : IsFree
                            ? "Free"
                            : $"Branch at height {Height}, children: {Child1} and {Child2}")}";

        }

    }

}
