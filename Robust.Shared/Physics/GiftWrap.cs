/*
* Farseer Physics Engine:
* Copyright (c) 2012 Ian Qvist
*
* Original source Box2D:
* Copyright (c) 2006-2011 Erin Catto http://www.box2d.org
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

using System.Collections.Generic;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    /// <summary>
    /// Giftwrap convex hull algorithm.
    /// O(nh) time complexity, where n is the number of points and h is the number of points on the convex hull.
    ///
    /// See http://en.wikipedia.org/wiki/Gift_wrapping_algorithm for more details.
    /// </summary>
    public static class GiftWrap
    {
        //Extracted from Box2D

        /// <summary>
        /// Returns the convex hull from the given vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        public static List<Vector2> GetConvexHull(List<Vector2> vertices)
        {
            if (vertices.Count <= 3)
                return vertices;

            // Find the right most point on the hull
            int i0 = 0;
            float x0 = vertices[0].X;
            for (int i = 1; i < vertices.Count; ++i)
            {
                float x = vertices[i].X;
                if (x > x0 || (MathHelper.CloseTo(x, x0) && vertices[i].Y < vertices[i0].Y))
                {
                    i0 = i;
                    x0 = x;
                }
            }

            int[] hull = new int[vertices.Count];
            int m = 0;
            int ih = i0;

            for (; ; )
            {
                hull[m] = ih;

                int ie = 0;
                for (int j = 1; j < vertices.Count; ++j)
                {
                    if (ie == ih)
                    {
                        ie = j;
                        continue;
                    }

                    Vector2 r = vertices[ie] - vertices[hull[m]];
                    Vector2 v = vertices[j] - vertices[hull[m]];
                    float c = Vector2.Cross(r, v);
                    if (c < 0.0f)
                    {
                        ie = j;
                    }

                    // Collinearity check
                    if (c == 0.0f && v.LengthSquared > r.LengthSquared)
                    {
                        ie = j;
                    }
                }

                ++m;
                ih = ie;

                if (ie == i0)
                {
                    break;
                }
            }

            var result = new List<Vector2>(m);

            // Copy vertices.
            for (var i = 0; i < m; ++i)
            {
                result.Add(vertices[hull[i]]);
            }

            return result;
        }
    }
}
