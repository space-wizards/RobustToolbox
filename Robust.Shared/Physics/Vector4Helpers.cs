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
using System.Numerics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    internal static class Vector4Helpers
    {
        public static System.Numerics.Vector4 Inverse(System.Numerics.Vector4 matrix)
        {
            float a = matrix.X, b = matrix.Z, c = matrix.Y, d = matrix.W;
            float det = a * d - b * c;
            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            return new System.Numerics.Vector4(det * d, -det * c, -det * b, det * a);
        }

        public static void Inverse(Span<Vector2> matrix)
        {
            DebugTools.Assert(matrix.Length == 2);
            float a = matrix[0].X, b = matrix[1].X, c = matrix[0].Y, d = matrix[1].Y;
            float det = a * d - b * c;
            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            matrix[0].X = det * d;
            matrix[0].Y = -det * c;

            matrix[1].X = -det * b;
            matrix[1].Y = det * a;
        }
    }
}
