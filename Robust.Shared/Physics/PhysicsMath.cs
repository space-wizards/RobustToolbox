using System;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    public static class PhysicsMath
    {
        public static Vector2 Mul(ref Mat22 A, ref Vector2 v)
        {
            return new Vector2(A.ex.X * v.X + A.ey.X * v.Y, A.ex.Y * v.X + A.ey.Y * v.Y);
        }

        /// Multiply a matrix times a vector.
        public static Vector2 Mul22(Mat33 A, Vector2 v)
        {
            return new Vector2(A.ex.X * v.X + A.ey.X * v.Y, A.ex.Y * v.X + A.ey.Y * v.Y);
        }

        /// Multiply a matrix times a vector.
        public static Vector3 Mul(Mat33 A, Vector3 v)
        {
            return v.X * A.ex + v.Y * A.ey + v.Z * A.ez;
        }

        // TODO: Combine this shit
        public static Vector2 Multiply(ref Vector2 left, ref PhysicsTransform right)
        {
            return new Vector2(
                (left.X * right.Quaternion.Real - left.Y * right.Quaternion.Imaginary) + right.Position.X,
                (left.Y * right.Quaternion.Real + left.X * right.Quaternion.Imaginary) + right.Position.Y);
        }

        public static Vector2 Multiply(Vector2 left, PhysicsTransform right)
        {
            return new Vector2(
                (left.X * right.Quaternion.Real - left.Y * right.Quaternion.Imaginary) + right.Position.X,
                (left.Y * right.Quaternion.Real + left.X * right.Quaternion.Imaginary) + right.Position.Y);
        }

        /// <summary>
        /// Return the angle between two vectors on a plane
        /// The angle is from vector 1 to vector 2, positive anticlockwise
        /// The result is between -pi -> pi
        /// </summary>
        public static double VectorAngle(ref Vector2 p1, ref Vector2 p2)
        {
            double theta1 = Math.Atan2(p1.Y, p1.X);
            double theta2 = Math.Atan2(p2.Y, p2.X);
            double dtheta = theta2 - theta1;
            while (dtheta > Math.PI)
                dtheta -= (2 * Math.PI);
            while (dtheta < -Math.PI)
                dtheta += (2 * Math.PI);

            return (dtheta);
        }

        /// <summary>
        /// Returns a positive number if c is to the left of the line going from a to b.
        /// </summary>
        /// <returns>Positive number if point is left, negative if point is right,
        /// and 0 if points are collinear.</returns>
        public static float Area(ref Vector2 a, ref Vector2 b, ref Vector2 c)
        {
            return a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y);
        }

        public static Vertices Vertices(this Box2 box)
        {
            var vertices = new Vertices(4)
            {
                box.TopRight,
                new Vector2(box.Right, box.Bottom),
                box.BottomLeft,
                new Vector2(box.Left, box.Top)
            };
            return vertices;
        }
    }

    /// <summary>
    /// A 2-by-2 matrix. Stored in column-major order.
    /// </summary>
    public struct Mat22
    {
        public Vector2 ex, ey;

        /// <summary>
        /// Construct this matrix using columns.
        /// </summary>
        /// <param name="c1">The c1.</param>
        /// <param name="c2">The c2.</param>
        public Mat22(Vector2 c1, Vector2 c2)
        {
            ex = c1;
            ey = c2;
        }

        /// <summary>
        /// Construct this matrix using scalars.
        /// </summary>
        /// <param name="a11">The a11.</param>
        /// <param name="a12">The a12.</param>
        /// <param name="a21">The a21.</param>
        /// <param name="a22">The a22.</param>
        public Mat22(float a11, float a12, float a21, float a22)
        {
            ex = new Vector2(a11, a21);
            ey = new Vector2(a12, a22);
        }

        public Mat22 Inverse
        {
            get
            {
                float a = ex.X, b = ey.X, c = ex.Y, d = ey.Y;
                float det = a * d - b * c;
                if (det != 0.0f)
                {
                    det = 1.0f / det;
                }

                Mat22 result = new Mat22();
                result.ex.X = det * d;
                result.ex.Y = -det * c;

                result.ey.X = -det * b;
                result.ey.Y = det * a;

                return result;
            }
        }

        /// <summary>
        /// Initialize this matrix using columns.
        /// </summary>
        /// <param name="c1">The c1.</param>
        /// <param name="c2">The c2.</param>
        public void Set(Vector2 c1, Vector2 c2)
        {
            ex = c1;
            ey = c2;
        }

        /// <summary>
        /// Set this to the identity matrix.
        /// </summary>
        public void SetIdentity()
        {
            ex.X = 1.0f;
            ey.X = 0.0f;
            ex.Y = 0.0f;
            ey.Y = 1.0f;
        }

        /// <summary>
        /// Set this matrix to all zeros.
        /// </summary>
        public void SetZero()
        {
            ex.X = 0.0f;
            ey.X = 0.0f;
            ex.Y = 0.0f;
            ey.Y = 0.0f;
        }

        /// <summary>
        /// Solve A * x = b, where b is a column vector. This is more efficient
        /// than computing the inverse in one-shot cases.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <returns></returns>
        public Vector2 Solve(Vector2 b)
        {
            float a11 = ex.X, a12 = ey.X, a21 = ex.Y, a22 = ey.Y;
            float det = a11 * a22 - a12 * a21;
            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            return new Vector2(det * (a22 * b.X - a12 * b.Y), det * (a11 * b.Y - a21 * b.X));
        }

        public static void Add(ref Mat22 A, ref Mat22 B, out Mat22 R)
        {
            R.ex = A.ex + B.ex;
            R.ey = A.ey + B.ey;
        }
    }

    /// <summary>
    /// A 3-by-3 matrix. Stored in column-major order.
    /// </summary>
    public struct Mat33
    {
        public Vector3 ex, ey, ez;

        /// <summary>
        /// Construct this matrix using columns.
        /// </summary>
        /// <param name="c1">The c1.</param>
        /// <param name="c2">The c2.</param>
        /// <param name="c3">The c3.</param>
        public Mat33(Vector3 c1, Vector3 c2, Vector3 c3)
        {
            ex = c1;
            ey = c2;
            ez = c3;
        }

        /// <summary>
        /// Set this matrix to all zeros.
        /// </summary>
        public void SetZero()
        {
            ex = Vector3.Zero;
            ey = Vector3.Zero;
            ez = Vector3.Zero;
        }

        /// <summary>
        /// Solve A * x = b, where b is a column vector. This is more efficient
        /// than computing the inverse in one-shot cases.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <returns></returns>
        public Vector3 Solve33(Vector3 b)
        {
            float det = Vector3.Dot(ex, Vector3.Cross(ey, ez));
            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            return new Vector3(det * Vector3.Dot(b, Vector3.Cross(ey, ez)), det * Vector3.Dot(ex, Vector3.Cross(b, ez)), det * Vector3.Dot(ex, Vector3.Cross(ey, b)));
        }

        /// <summary>
        /// Solve A * x = b, where b is a column vector. This is more efficient
        /// than computing the inverse in one-shot cases. Solve only the upper
        /// 2-by-2 matrix equation.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <returns></returns>
        public Vector2 Solve22(Vector2 b)
        {
            float a11 = ex.X, a12 = ey.X, a21 = ex.Y, a22 = ey.Y;
            float det = a11 * a22 - a12 * a21;

            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            return new Vector2(det * (a22 * b.X - a12 * b.Y), det * (a11 * b.Y - a21 * b.X));
        }

        /// Get the inverse of this matrix as a 2-by-2.
        /// Returns the zero matrix if singular.
        public void GetInverse22(ref Mat33 M)
        {
            float a = ex.X, b = ey.X, c = ex.Y, d = ey.Y;
            float det = a * d - b * c;
            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            M.ex.X = det * d; M.ey.X = -det * b; M.ex.Z = 0.0f;
            M.ex.Y = -det * c; M.ey.Y = det * a; M.ey.Z = 0.0f;
            M.ez.X = 0.0f; M.ez.Y = 0.0f; M.ez.Z = 0.0f;
        }

        /// Get the symmetric inverse of this matrix as a 3-by-3.
        /// Returns the zero matrix if singular.
        public void GetSymInverse33(ref Mat33 M)
        {
            float det = Vector3.Dot(ex, Vector3.Cross(ey, ez));
            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            float a11 = ex.X, a12 = ey.X, a13 = ez.X;
            float a22 = ey.Y, a23 = ez.Y;
            float a33 = ez.Z;

            M.ex.X = det * (a22 * a33 - a23 * a23);
            M.ex.Y = det * (a13 * a23 - a12 * a33);
            M.ex.Z = det * (a12 * a23 - a13 * a22);

            M.ey.X = M.ex.Y;
            M.ey.Y = det * (a11 * a33 - a13 * a13);
            M.ey.Z = det * (a13 * a12 - a11 * a23);

            M.ez.X = M.ex.Z;
            M.ez.Y = M.ey.Z;
            M.ez.Z = det * (a11 * a22 - a12 * a12);
        }
    }
}
