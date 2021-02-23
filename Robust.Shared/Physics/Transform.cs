/*
Microsoft Permissive License (Ms-PL)

This license governs use of the accompanying software. If you use the software, you accept this license.
If you do not accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under
U.S. copyright law.
A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3,
each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution,
prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3,
each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to
make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or
derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software,
your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark,
and attribution notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by
including a complete copy of this license with your distribution.
If you distribute any portion of the software in compiled or object code form, you may only do so under a license that
complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions.
You may have additional consumer rights under your local laws which this license cannot change.
To the extent permitted under your local laws, the contributors exclude the implied warranties of
merchantability, fitness for a particular purpose and non-infringement.
*/

using System;
using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    // TODO: Probably replace this internally with just the Vector2 and radians but I'd need to re-learn trig so yeah....
    internal struct Transform
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
