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

using System;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    internal static class Vector2Helpers
    {
        public static Vector2[] Inverse(this Vector2[] matrix)
        {
            DebugTools.Assert(matrix.Length == 2);
            float a = matrix[0].X, b = matrix[1].X, c = matrix[0].Y, d = matrix[1].Y;
            float det = a * d - b * c;
            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            Vector2[] result = new Vector2[2];
            result[0].X = det * d;
            result[0].Y = -det * c;

            result[1].X = -det * b;
            result[1].Y = det * a;

            return result;
        }

        public static Vector2[] Inverse(Span<Vector2> matrix)
        {
            DebugTools.Assert(matrix.Length == 2);
            float a = matrix[0].X, b = matrix[1].X, c = matrix[0].Y, d = matrix[1].Y;
            float det = a * d - b * c;
            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            Vector2[] result = new Vector2[2];
            result[0].X = det * d;
            result[0].Y = -det * c;

            result[1].X = -det * b;
            result[1].Y = det * a;

            return result;
        }
    }
}
