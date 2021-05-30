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
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    // TODO: Probably replace this internally with just the Vector2 and radians but I'd need to re-learn trig so yeah....
    public struct Transform
    {
        public Vector2 Position;
        public Quaternion2D Quaternion2D;

        public Transform(Vector2 position, float angle)
        {
            Position = position;
            Quaternion2D = new Quaternion2D(angle);
        }

        public Transform(float angle)
        {
            Position = Vector2.Zero;
            Quaternion2D = new Quaternion2D(angle);
        }

        public static Vector2 Mul(in Transform transform, in Vector2 vector)
        {
            float x = (transform.Quaternion2D.C * vector.X - transform.Quaternion2D.S * vector.Y) + transform.Position.X;
            float y = (transform.Quaternion2D.S * vector.X + transform.Quaternion2D.C * vector.Y) + transform.Position.Y;

            return new Vector2(x, y);
        }

        public static Vector2 MulT(in Vector2[] A, in Vector2 v)
        {
            DebugTools.Assert(A.Length == 2);
            return new Vector2(v.X * A[0].X + v.Y * A[0].Y, v.X * A[1].X + v.Y * A[1].Y);
        }

        public static Vector2 MulT(in Transform T, in Vector2 v)
        {
            float px = v.X - T.Position.X;
            float py = v.Y - T.Position.Y;
            float x = (T.Quaternion2D.C * px + T.Quaternion2D.S * py);
            float y = (-T.Quaternion2D.S * px + T.Quaternion2D.C * py);

            return new Vector2(x, y);
        }

        /// Transpose multiply two rotations: qT * r
        public static Quaternion2D MulT(in Quaternion2D q, in Quaternion2D r)
        {
            // [ qc qs] * [rc -rs] = [qc*rc+qs*rs -qc*rs+qs*rc]
            // [-qs qc]   [rs  rc]   [-qs*rc+qc*rs qs*rs+qc*rc]
            // s = qc * rs - qs * rc
            // c = qc * rc + qs * rs
            Quaternion2D qr;
            qr.S = q.C * r.S - q.S * r.C;
            qr.C = q.C * r.C + q.S * r.S;
            return qr;
        }

        // v2 = A.q' * (B.q * v1 + B.p - A.p)
        //    = A.q' * B.q * v1 + A.q' * (B.p - A.p)
        public static Transform MulT(in Transform A, in Transform B)
        {
            Transform C = new Transform
            {
                Quaternion2D = MulT(A.Quaternion2D, B.Quaternion2D),
                Position = MulT(A.Quaternion2D, B.Position - A.Position)
            };
            return C;
        }

        /// <summary>
        ///     Inverse rotate a vector
        /// </summary>
        /// <param name="q"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector2 MulT(Quaternion2D q, Vector2 v)
        {
            return new(q.C * v.X + q.S * v.Y, -q.S * v.X + q.C * v.Y);
        }

        /// <summary>
        ///     Rotate a vector
        /// </summary>
        /// <param name="quaternion2D"></param>
        /// <param name="vector"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Mul(in Quaternion2D quaternion2D, in Vector2 vector)
        {
            return new(quaternion2D.C * vector.X - quaternion2D.S * vector.Y, quaternion2D.S * vector.X + quaternion2D.C * vector.Y);
        }

        public static Vector2 Mul(in Vector2[] A, in Vector2 v)
        {
            // A needing to be a 2 x 2 matrix
            DebugTools.Assert(A.Length == 2);
            return new Vector2(A[0].X * v.X + A[1].X * v.Y, A[0].Y * v.X + A[1].Y * v.Y);
        }
    }

    public struct Quaternion2D
    {
        public float C;
        public float S;

        public Quaternion2D(float cos, float sin)
        {
            C = cos;
            S = sin;
        }

        public Quaternion2D(float angle)
        {
            C = MathF.Cos(angle);
            S = MathF.Sin(angle);
        }

        public Quaternion2D Set(float angle)
        {
            //FPE: Optimization
            if (angle == 0.0f)
            {
                return new Quaternion2D(1.0f, 0.0f);
            }

            // TODO_ERIN optimize
            return new Quaternion2D(MathF.Cos(angle), MathF.Sin(angle));
        }
    }
}
