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
    public struct Matrix22
    {
        public Vector2 EX;
        public Vector2 EY;

        public Matrix22(Vector2 c1, Vector2 c2)
        {
            EX = c1;
            EY = c2;
        }

        public Matrix22(float a11, float a12, float a21, float a22)
        {
            EX.X = a11; EX.Y = a21;
            EY.X = a12; EY.Y = a22;
        }

        public void Set(Vector2 c1, Vector2 c2)
        {
            EX = c1;
            EY = c2;
        }

        public void SetIdentity()
        {
            EX.X = 1.0f; EY.X = 0.0f;
            EX.Y = 0.0f; EY.Y = 1.0f;
        }

        public void SetZero()
        {
            EX.X = 0f;
            EX.Y = 0f;
            EY.X = 0f;
            EY.Y = 0f;
        }

        public Matrix22 GetInverse()
        {
            float a = EX.X, b = EY.X, c = EX.Y, d = EY.Y;
            float det = a * d - b * c;
            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            return new Matrix22(det * d, -det * c, -det * b, det * a);
        }

        /// <summary>
        /// Solve A * x = b, where b is a column vector. This is more efficient
        /// than computing the inverse in one-shot cases.
        /// </summary>
        public Vector2 Solve(Vector2 b)
        {
            float a11 = EX.X, a12 = EY.X, a21 = EX.Y, a22 = EY.Y;
            float det = a11 * a22 - a12 * a21;
            if (det != 0.0f)
            {
                det = 1.0f / det;
            }

            return new Vector2(det * (a22 * b.X - a12 * b.Y), det * (a11 * b.Y - a21 * b.X));
        }
    }
}
