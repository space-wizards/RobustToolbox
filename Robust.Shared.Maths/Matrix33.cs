// MIT License

// Copyright (c) 2019 Erin Catto

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace Robust.Shared.Maths
{
    public struct Matrix33
    {
        public Vector3 EX;
        public Vector3 EY;
        public Vector3 EZ;

        public Matrix33(Vector3 c1, Vector3 c2, Vector3 c3)
        {
            EX = c1;
            EY = c2;
            EZ = c3;
        }

        public void SetZero()
        {
            EX = Vector3.Zero;
            EY = Vector3.Zero;
            EZ = Vector3.Zero;
        }

        /// <summary>
        /// Solve A * x = b, where b is a column vector. This is more efficient
        /// than computing the inverse in one-shot cases.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public Vector3 Solve33(Vector3 b)
        {
            float det = Vector3.Dot(EX, Vector3.Cross(EY, EZ));

            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            Vector3 x;
            x.X = det * Vector3.Dot(b, Vector3.Cross(EY, EZ));
            x.Y = det * Vector3.Dot(EX, Vector3.Cross(b, EZ));
            x.Z = det * Vector3.Dot(EX, Vector3.Cross(EY, b));
            return x;
        }

        /// <summary>
        /// Solve A * x = b, where b is a column vector. This is more efficient
        /// than computing the inverse in one-shot cases. Solve only the upper
        /// 2-by-2 matrix equation.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public Vector2 Solve22(Vector2 b)
        {
            float a11 = EX.X, a12 = EY.X, a21 = EX.Y, a22 = EY.Y;
            float det = a11 * a22 - a12 * a21;
            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            Vector2 x;
            x.X = det * (a22 * b.X - a12 * b.Y);
            x.Y = det * (a11 * b.Y - a21 * b.X);
            return x;
        }

        /// <summary>
        /// Get the inverse of this matrix as a 2-by-2.
        /// Returns the zero matrix if singular.
        /// </summary>
        /// <returns></returns>
        public void GetInverse22(ref Matrix33 matrix)
        {
            float a = EX.X, b = EY.X, c = EX.Y, d = EY.Y;
            float det = a * d - b * c;
            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            matrix.EX.X =  det * d;	matrix.EY.X = -det * b; matrix.EX.Z = 0.0f;
            matrix.EX.Y = -det * c;	matrix.EY.Y =  det * a; matrix.EY.Z = 0.0f;
            matrix.EZ.X = 0.0f; matrix.EZ.Y = 0.0f; matrix.EZ.Z = 0.0f;
        }

        /// <summary>
        /// Get the symmetric inverse of this matrix as a 3-by-3.
        /// Returns the zero matrix if singular.
        /// </summary>
        /// <param name="mm"></param>
        public void GetSymInverse33(ref Matrix33 matrix)
        {
            float det = Vector3.Dot(EX, Vector3.Cross(EY, EZ));
            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            float a11 = EX.X, a12 = EY.X, a13 = EZ.X;
            float a22 = EY.Y, a23 = EZ.Y;
            float a33 = EZ.Z;

            matrix.EX.X = det * (a22 * a33 - a23 * a23);
            matrix.EX.Y = det * (a13 * a23 - a12 * a33);
            matrix.EX.Z = det * (a12 * a23 - a13 * a22);

            matrix.EY.X = matrix.EX.Y;
            matrix.EY.Y = det * (a11 * a33 - a13 * a13);
            matrix.EY.Z = det * (a13 * a12 - a11 * a23);

            matrix.EZ.X = matrix.EX.Z;
            matrix.EZ.Y = matrix.EY.Z;
            matrix.EZ.Z = det * (a11 * a22 - a12 * a12);
        }

        /// <summary>
        /// Multiply a matrix times a vector.
        /// </summary>
        public Vector3 Mul(Vector3 v)
        {
            return v.X * EX + v.Y * EY + v.Z * EZ;
        }

        /// <summary>
        /// Multiply a matrix times a vector.
        /// </summary>
        public Vector2 Mul22(Vector2 v)
        {
            return new Vector2(EX.X * v.X + EY.X * v.Y, EX.Y * v.X + EY.Y * v.Y);
        }
    }
}
