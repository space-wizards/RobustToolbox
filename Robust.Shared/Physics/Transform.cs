using System;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    // TODO: Probably replace this internally with just the Vector2 and radians but I'd need to re-learn trig so yeah....
    internal struct Transform
    {
        public Vector2 Position;
        public Quaternion Quaternion;

        public Transform(Vector2 position, float angle)
        {
            Position = position;
            Quaternion = new Quaternion(angle);
        }

        public Transform(float angle)
        {
            Position = Vector2.Zero;
            Quaternion = new Quaternion(angle);
        }

        public static Vector2 Mul(in Transform transform, in Vector2 vector)
        {
            float x = (transform.Quaternion.C * vector.X - transform.Quaternion.S * vector.Y) + transform.Position.X;
            float y = (transform.Quaternion.S * vector.X + transform.Quaternion.C * vector.Y) + transform.Position.Y;

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
            float x = (T.Quaternion.C * px + T.Quaternion.S * py);
            float y = (-T.Quaternion.S * px + T.Quaternion.C * py);

            return new Vector2(x, y);
        }

        /// Transpose multiply two rotations: qT * r
        public static Quaternion MulT(in Quaternion q, in Quaternion r)
        {
            // [ qc qs] * [rc -rs] = [qc*rc+qs*rs -qc*rs+qs*rc]
            // [-qs qc]   [rs  rc]   [-qs*rc+qc*rs qs*rs+qc*rc]
            // s = qc * rs - qs * rc
            // c = qc * rc + qs * rs
            Quaternion qr;
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
                Quaternion = MulT(A.Quaternion, B.Quaternion),
                Position = MulT(A.Quaternion, B.Position - A.Position)
            };
            return C;
        }

        /// <summary>
        ///     Inverse rotate a vector
        /// </summary>
        /// <param name="q"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector2 MulT(Quaternion q, Vector2 v)
        {
            return new(q.C * v.X + q.S * v.Y, -q.S * v.X + q.C * v.Y);
        }

        /// <summary>
        ///     Rotate a vector
        /// </summary>
        /// <param name="quaternion"></param>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector2 Mul(in Quaternion quaternion, in Vector2 vector)
        {
            return new(quaternion.C * vector.X - quaternion.S * vector.Y, quaternion.S * vector.X + quaternion.C * vector.Y);
        }

        public static Vector2 Mul(in Vector2[] A, in Vector2 v)
        {
            // A needing to be a 2 x 2 matrix
            DebugTools.Assert(A.Length == 2);
            return new Vector2(A[0].X * v.X + A[1].X * v.Y, A[0].Y * v.X + A[1].Y * v.Y);
        }
    }

    internal struct Quaternion
    {
        public float C;
        public float S;

        public Quaternion(float cos, float sin)
        {
            C = cos;
            S = sin;
        }

        public Quaternion(float angle)
        {
            C = MathF.Cos(angle);
            S = MathF.Sin(angle);
        }

        public Quaternion Set(float angle)
        {
            //FPE: Optimization
            if (angle == 0.0f)
            {
                return new Quaternion(1.0f, 0.0f);
            }

            // TODO_ERIN optimize
            return new Quaternion(MathF.Cos(angle), MathF.Sin(angle));
        }
    }
}
