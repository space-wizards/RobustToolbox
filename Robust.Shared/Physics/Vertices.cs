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
    public static class Vertices
    {
        public static void ForceCounterClockwise(this List<Vector2> vertices)
        {
            if (vertices.Count < 3) return;

            if (!vertices.IsCounterClockwise()) vertices.Reverse();
        }

        public static bool IsCounterClockwise(this List<Vector2> vertices)
        {
            if (vertices.Count < 3) return false;
            return vertices.GetSignedArea() > 0.0f;
        }

        /// <summary>
        /// Gets the signed area.
        /// If the area is less than 0, it indicates that the polygon is clockwise winded.
        /// </summary>
        /// <returns>The signed area</returns>
        public static float GetSignedArea(this List<Vector2> vertices)
        {
            var count = vertices.Count;

            //The simplest polygon which can exist in the Euclidean plane has 3 sides.
            if (count < 3)
                return 0;

            int i;
            float area = 0;

            for (i = 0; i < count; i++)
            {
                int j = (i + 1) % count;

                Vector2 vi = vertices[i];
                Vector2 vj = vertices[j];

                area += vi.X * vj.Y;
                area -= vi.Y * vj.X;
            }
            area /= 2.0f;
            return area;
        }
    }
}
