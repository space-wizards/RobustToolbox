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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    // TODO: Probably replace this internally with just the Vector2 and radians but I'd need to re-learn trig so yeah....
    public struct Transform
    {
        public static readonly Transform Empty = new Transform(0f);

        public Vector2 Position;
        public Quaternion2D Quaternion2D;

        public Transform(Vector2 position, Quaternion2D quat)
        {
            Position = position;
            Quaternion2D = quat;
        }

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

        public Transform(Vector2 position, Angle angle)
        {
            Position = position;
            Quaternion2D = new Quaternion2D(angle);
        }

        /// Inverse transform a point (e.g. world space to local space)
        [Pure]
        public static Vector2 InvTransformPoint(Transform t, Vector2 p)
        {
            float vx = p.X - t.Position.X;
            float vy = p.Y - t.Position.Y;
            return new Vector2(t.Quaternion2D.C * vx + t.Quaternion2D.S * vy, -t.Quaternion2D.S * vx + t.Quaternion2D.C * vy);
        }

        [Pure]
        public static Vector2 Mul(in Transform transform, in Vector2 vector)
        {
            float x = (transform.Quaternion2D.C * vector.X - transform.Quaternion2D.S * vector.Y) + transform.Position.X;
            float y = (transform.Quaternion2D.S * vector.X + transform.Quaternion2D.C * vector.Y) + transform.Position.Y;

            return new Vector2(x, y);
        }

        [Pure]
        public static Vector2 MulT(in Vector2[] A, in Vector2 v)
        {
            DebugTools.Assert(A.Length == 2);
            return new Vector2(v.X * A[0].X + v.Y * A[0].Y, v.X * A[1].X + v.Y * A[1].Y);
        }

        [Pure]
        public static Vector2 MulT(in Transform T, in Vector2 v)
        {
            float px = v.X - T.Position.X;
            float py = v.Y - T.Position.Y;
            float x = (T.Quaternion2D.C * px + T.Quaternion2D.S * py);
            float y = (-T.Quaternion2D.S * px + T.Quaternion2D.C * py);

            return new Vector2(x, y);
        }

        /// Transpose multiply two rotations: qT * r
        [Pure]
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

        [Pure]
        public static Transform InvMulTransforms(in Transform A, in Transform B)
        {
            return new Transform(Quaternion2D.InvRotateVector(A.Quaternion2D, Vector2.Subtract(B.Position, A.Position)), Quaternion2D.InvMulRot(A.Quaternion2D, B.Quaternion2D));
        }

        // v2 = A.q' * (B.q * v1 + B.p - A.p)
        //    = A.q' * B.q * v1 + A.q' * (B.p - A.p)
        [Pure]
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

        public static Vector2 Mul(System.Numerics.Vector4 A, Vector2 v)
        {
            return new Vector2(A.X * v.X + A.Y * v.Y, A.Z * v.X + A.W * v.Y);
        }

        public static Vector2 Mul(in Vector2[] A, in Vector2 v)
        {
            // A needing to be a 2 x 2 matrix
            DebugTools.Assert(A.Length == 2);
            return new Vector2(A[0].X * v.X + A[1].X * v.Y, A[0].Y * v.X + A[1].Y * v.Y);
        }

        public static Vector2 Mul(Matrix22 A, Vector2 v)
        {
            return new Vector2(A.EX.X * v.X + A.EY.X * v.Y, A.EX.Y * v.X + A.EY.Y * v.Y);
        }
    }

    public struct Quaternion2D
    {
        public float C;
        public float S;

        public float Angle => MathF.Atan2(S, C);

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

        public Quaternion2D(Angle angle)
        {
            var radians = angle.Theta;

            C = (float) Math.Cos(radians);
            S = (float) Math.Sin(radians);
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

        /// Rotate a vector
        [Pure]
        public static Vector2 RotateVector(Quaternion2D q, Vector2 v )
        {
            return new Vector2(q.C * v.X - q.S * v.Y, q.S * v.X + q.C * v.Y);
        }

        /// Inverse rotate a vector
        [Pure]
        public static Vector2 InvRotateVector(Quaternion2D q, Vector2 v)
        {
            return new Vector2(q.C * v.X + q.S * v.Y, -q.S * v.X + q.C * v.Y);
        }

        public bool IsValid()
        {
            if (float.IsNaN(S ) || float.IsNaN(C))
            {
                return false;
            }

            if (float.IsInfinity(S) || float.IsInfinity(C))
            {
                return false;
            }

            return IsNormalized();
        }

        public bool IsNormalized()
        {
            // larger tolerance due to failure on mingw 32-bit
            float qq = S * S + C * C;
            return 1.0f - 0.0006f < qq && qq < 1.0f + 0.0006f;
        }

        [Pure]
        public static Quaternion2D InvMulRot(Quaternion2D q, Quaternion2D r)
        {
            // [ qc qs] * [rc -rs] = [qc*rc+qs*rs -qc*rs+qs*rc]
            // [-qs qc]   [rs  rc]   [-qs*rc+qc*rs qs*rs+qc*rc]
            // s(q - r) = qc * rs - qs * rc
            // c(q - r) = qc * rc + qs * rs
            return new Quaternion2D(q.C * r.C + q.S * r.S, q.C * r.S - q.S * r.C);
        }
    }
}
