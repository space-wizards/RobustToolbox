#region --- License ---

/*
Copyright (c) 2006 - 2008 The Open Toolkit library.

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

#endregion --- License ---

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Robust.Shared.Maths
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Matrix3 : IEquatable<Matrix3>, IApproxEquatable<Matrix3>
    {
        #region Fields & Access

        /// <summary>Row 0, Column 0</summary>
        public float R0C0;

        /// <summary>Row 0, Column 1</summary>
        public float R0C1;

        /// <summary>Row 0, Column 2</summary>
        public float R0C2;

        /// <summary>Row 1, Column 0</summary>
        public float R1C0;

        /// <summary>Row 1, Column 1</summary>
        public float R1C1;

        /// <summary>Row 1, Column 2</summary>
        public float R1C2;

        /// <summary>Row 2, Column 0</summary>
        public float R2C0;

        /// <summary>Row 2, Column 1</summary>
        public float R2C1;

        /// <summary>Row 2, Column 2</summary>
        public float R2C2;

        /// <summary>Gets the component at the given row and column in the matrix.</summary>
        /// <param name="row">The row of the matrix.</param>
        /// <param name="column">The column of the matrix.</param>
        /// <returns>The component at the given row and column in the matrix.</returns>
        public float this[int row, int column]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (row)
                {
                    case 0:
                        switch (column)
                        {
                            case 0: return R0C0;
                            case 1: return R0C1;
                            case 2: return R0C2;
                        }

                        break;

                    case 1:
                        switch (column)
                        {
                            case 0: return R1C0;
                            case 1: return R1C1;
                            case 2: return R1C2;
                        }

                        break;

                    case 2:
                        switch (column)
                        {
                            case 0: return R2C0;
                            case 1: return R2C1;
                            case 2: return R2C2;
                        }

                        break;
                }

                throw new IndexOutOfRangeException();
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                switch (row)
                {
                    case 0:
                        switch (column)
                        {
                            case 0:
                                R0C0 = value;
                                return;
                            case 1:
                                R0C1 = value;
                                return;
                            case 2:
                                R0C2 = value;
                                return;
                        }

                        break;

                    case 1:
                        switch (column)
                        {
                            case 0:
                                R1C0 = value;
                                return;
                            case 1:
                                R1C1 = value;
                                return;
                            case 2:
                                R1C2 = value;
                                return;
                        }

                        break;

                    case 2:
                        switch (column)
                        {
                            case 0:
                                R2C0 = value;
                                return;
                            case 1:
                                R2C1 = value;
                                return;
                            case 2:
                                R2C2 = value;
                                return;
                        }

                        break;
                }

                throw new IndexOutOfRangeException();
            }
        }

        /// <summary>Gets the component at the index into the matrix.</summary>
        /// <param name="index">The index into the components of the matrix.</param>
        /// <returns>The component at the given index into the matrix.</returns>
        public float this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (index)
                {
                    case 0: return R0C0;
                    case 1: return R0C1;
                    case 2: return R0C2;
                    case 3: return R1C0;
                    case 4: return R1C1;
                    case 5: return R1C2;
                    case 6: return R2C0;
                    case 7: return R2C1;
                    case 8: return R2C2;
                    default: throw new IndexOutOfRangeException();
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                switch (index)
                {
                    case 0:
                        R0C0 = value;
                        return;
                    case 1:
                        R0C1 = value;
                        return;
                    case 2:
                        R0C2 = value;
                        return;
                    case 3:
                        R1C0 = value;
                        return;
                    case 4:
                        R1C1 = value;
                        return;
                    case 5:
                        R1C2 = value;
                        return;
                    case 6:
                        R2C0 = value;
                        return;
                    case 7:
                        R2C1 = value;
                        return;
                    case 8:
                        R2C2 = value;
                        return;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        /// <summary>Converts the matrix into an array of floats.</summary>
        /// <param name="matrix">The matrix to convert.</param>
        /// <returns>An array of floats for the matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator float[](Matrix3 matrix)
        {
            return new[]
            {
                matrix.R0C0,
                matrix.R0C1,
                matrix.R0C2,
                matrix.R1C0,
                matrix.R1C1,
                matrix.R1C2,
                matrix.R2C0,
                matrix.R2C1,
                matrix.R2C2
            };
        }

        #endregion Fields & Access

        #region Constructors

        /// <summary>Constructs left matrix with the same components as the given matrix.</summary>
        /// <param name="matrix">The matrix whose components to copy.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix3(ref Matrix3 matrix)
        {
            R0C0 = matrix.R0C0;
            R0C1 = matrix.R0C1;
            R0C2 = matrix.R0C2;
            R1C0 = matrix.R1C0;
            R1C1 = matrix.R1C1;
            R1C2 = matrix.R1C2;
            R2C0 = matrix.R2C0;
            R2C1 = matrix.R2C1;
            R2C2 = matrix.R2C2;
        }

        /// <summary>Constructs left matrix with the given values.</summary>
        /// <param name="r0c0">The value for row 0 column 0.</param>
        /// <param name="r0c1">The value for row 0 column 1.</param>
        /// <param name="r0c2">The value for row 0 column 2.</param>
        /// <param name="r1c0">The value for row 1 column 0.</param>
        /// <param name="r1c1">The value for row 1 column 1.</param>
        /// <param name="r1c2">The value for row 1 column 2.</param>
        /// <param name="r2c0">The value for row 2 column 0.</param>
        /// <param name="r2c1">The value for row 2 column 1.</param>
        /// <param name="r2c2">The value for row 2 column 2.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix3
        (
            float r0c0,
            float r0c1,
            float r0c2,
            float r1c0,
            float r1c1,
            float r1c2,
            float r2c0,
            float r2c1,
            float r2c2
        )
        {
            R0C0 = r0c0;
            R0C1 = r0c1;
            R0C2 = r0c2;
            R1C0 = r1c0;
            R1C1 = r1c1;
            R1C2 = r1c2;
            R2C0 = r2c0;
            R2C1 = r2c1;
            R2C2 = r2c2;
        }

        /// <summary>Constructs left matrix from the given array of float-precision floating-point numbers.</summary>
        /// <param name="floatArray">The array of floats for the components of the matrix.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix3(float[] floatArray)
        {
            if (floatArray == null || floatArray.GetLength(0) < 9) throw new MissingFieldException();

            R0C0 = floatArray[0];
            R0C1 = floatArray[1];
            R0C2 = floatArray[2];
            R1C0 = floatArray[3];
            R1C1 = floatArray[4];
            R1C2 = floatArray[5];
            R2C0 = floatArray[6];
            R2C1 = floatArray[7];
            R2C2 = floatArray[8];
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="matrix">A Matrix4 to take the upper-left 3x3 from.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix3(Matrix4 matrix)
        {
            R0C0 = matrix.Row0.X;
            R0C1 = matrix.Row0.Y;
            R0C2 = matrix.Row0.Z;

            R1C0 = matrix.Row1.X;
            R1C1 = matrix.Row1.Y;
            R1C2 = matrix.Row1.Z;

            R2C0 = matrix.Row2.X;
            R2C1 = matrix.Row2.Y;
            R2C2 = matrix.Row2.Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3 CreateTranslation(float x, float y)
        {
            var result = Identity;

            /* column major
             0 0 x
             0 0 y
             0 0 1
            */
            result.R0C2 = x;
            result.R1C2 = y;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3 CreateTranslation(Vector2 vector)
        {
            return CreateTranslation(vector.X, vector.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3 CreateRotation(float angle)
        {
            return CreateRotation(new Angle(angle));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3 CreateRotation(Angle angle)
        {
            var cos = (float) Math.Cos(angle);
            var sin = (float) Math.Sin(angle);

            var result = Identity;

            /* column major
             cos -sin 0
             sin  cos 0
              0    0  1
            */
            result.R0C0 = cos;
            result.R1C0 = sin;
            result.R0C1 = -sin;
            result.R1C1 = cos;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3 CreateScale(float x, float y)
        {
            var result = Identity;

            /* column major
             x 0 0
             0 y 0
             0 0 1
            */
            result.R0C0 = x;
            result.R1C1 = y;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3 CreateScale(in Vector2 scale)
        {
            return CreateScale(scale.X, scale.Y);
        }

        #endregion Constructors

        #region Equality

        /// <summary>Indicates whether the current matrix is equal to another matrix.</summary>
        /// <param name="matrix">The Matrix3 structure to compare with.</param>
        /// <returns>true if the current matrix is equal to the matrix parameter; otherwise, false.</returns>
        public bool Equals(Matrix3 other)
        {
            return
                R0C0 == other.R0C0 &&
                R0C1 == other.R0C1 &&
                R0C2 == other.R0C2 &&
                R1C0 == other.R1C0 &&
                R1C1 == other.R1C1 &&
                R1C2 == other.R1C2 &&
                R2C0 == other.R2C0 &&
                R2C1 == other.R2C1 &&
                R2C2 == other.R2C2;
        }

        /// <summary>Indicates whether the current matrix is equal to another matrix.</summary>
        /// <param name="matrix">The Matrix3 structure to compare to.</param>
        /// <returns>true if the current matrix is equal to the matrix parameter; otherwise, false.</returns>
        public bool Equals(ref Matrix3 matrix)
        {
            return
                R0C0 == matrix.R0C0 &&
                R0C1 == matrix.R0C1 &&
                R0C2 == matrix.R0C2 &&
                R1C0 == matrix.R1C0 &&
                R1C1 == matrix.R1C1 &&
                R1C2 == matrix.R1C2 &&
                R2C0 == matrix.R2C0 &&
                R2C1 == matrix.R2C1 &&
                R2C2 == matrix.R2C2;
        }

        /// <summary>Indicates whether the current matrix is equal to another matrix.</summary>
        /// <param name="left">The left-hand operand.</param>
        /// <param name="right">The right-hand operand.</param>
        /// <returns>true if the current matrix is equal to the matrix parameter; otherwise, false.</returns>
        public static bool Equals(ref Matrix3 left, ref Matrix3 right)
        {
            return
                left.R0C0 == right.R0C0 &&
                left.R0C1 == right.R0C1 &&
                left.R0C2 == right.R0C2 &&
                left.R1C0 == right.R1C0 &&
                left.R1C1 == right.R1C1 &&
                left.R1C2 == right.R1C2 &&
                left.R2C0 == right.R2C0 &&
                left.R2C1 == right.R2C1 &&
                left.R2C2 == right.R2C2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EqualsApprox(Matrix3 other)
        {
            return EqualsApprox(ref other, 1.0E-6f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EqualsApprox(Matrix3 other, double tolerance)
        {
            return EqualsApprox(ref other, (float) tolerance);
        }

        /// <summary>Indicates whether the current matrix is approximately equal to another matrix.</summary>
        /// <param name="matrix">The Matrix3 structure to compare with.</param>
        /// <param name="tolerance">The limit below which the matrices are considered equal.</param>
        /// <returns>true if the current matrix is approximately equal to the matrix parameter; otherwise, false.</returns>
        public bool EqualsApprox(ref Matrix3 matrix, float tolerance)
        {
            return
                Math.Abs(R0C0 - matrix.R0C0) <= tolerance &&
                Math.Abs(R0C1 - matrix.R0C1) <= tolerance &&
                Math.Abs(R0C2 - matrix.R0C2) <= tolerance &&
                Math.Abs(R1C0 - matrix.R1C0) <= tolerance &&
                Math.Abs(R1C1 - matrix.R1C1) <= tolerance &&
                Math.Abs(R1C2 - matrix.R1C2) <= tolerance &&
                Math.Abs(R2C0 - matrix.R2C0) <= tolerance &&
                Math.Abs(R2C1 - matrix.R2C1) <= tolerance &&
                Math.Abs(R2C2 - matrix.R2C2) <= tolerance;
        }

        /// <summary>Indicates whether the current matrix is approximately equal to another matrix.</summary>
        /// <param name="left">The left-hand operand.</param>
        /// <param name="right">The right-hand operand.</param>
        /// <param name="tolerance">The limit below which the matrices are considered equal.</param>
        /// <returns>true if the current matrix is approximately equal to the matrix parameter; otherwise, false.</returns>
        public static bool EqualsApprox(ref Matrix3 left, ref Matrix3 right, float tolerance)
        {
            return
                Math.Abs(left.R0C0 - right.R0C0) <= tolerance &&
                Math.Abs(left.R0C1 - right.R0C1) <= tolerance &&
                Math.Abs(left.R0C2 - right.R0C2) <= tolerance &&
                Math.Abs(left.R1C0 - right.R1C0) <= tolerance &&
                Math.Abs(left.R1C1 - right.R1C1) <= tolerance &&
                Math.Abs(left.R1C2 - right.R1C2) <= tolerance &&
                Math.Abs(left.R2C0 - right.R2C0) <= tolerance &&
                Math.Abs(left.R2C1 - right.R2C1) <= tolerance &&
                Math.Abs(left.R2C2 - right.R2C2) <= tolerance;
        }

        #endregion Equality

        #region Arithmetic Operators

        /// <summary>Add left matrix to this matrix.</summary>
        /// <param name="matrix">The matrix to add.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(ref Matrix3 matrix)
        {
            R0C0 = R0C0 + matrix.R0C0;
            R0C1 = R0C1 + matrix.R0C1;
            R0C2 = R0C2 + matrix.R0C2;
            R1C0 = R1C0 + matrix.R1C0;
            R1C1 = R1C1 + matrix.R1C1;
            R1C2 = R1C2 + matrix.R1C2;
            R2C0 = R2C0 + matrix.R2C0;
            R2C1 = R2C1 + matrix.R2C1;
            R2C2 = R2C2 + matrix.R2C2;
        }

        /// <summary>Add left matrix to this matrix.</summary>
        /// <param name="matrix">The matrix to add.</param>
        /// <param name="result">The resulting matrix of the addition.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(ref Matrix3 matrix, out Matrix3 result)
        {
            result.R0C0 = R0C0 + matrix.R0C0;
            result.R0C1 = R0C1 + matrix.R0C1;
            result.R0C2 = R0C2 + matrix.R0C2;
            result.R1C0 = R1C0 + matrix.R1C0;
            result.R1C1 = R1C1 + matrix.R1C1;
            result.R1C2 = R1C2 + matrix.R1C2;
            result.R2C0 = R2C0 + matrix.R2C0;
            result.R2C1 = R2C1 + matrix.R2C1;
            result.R2C2 = R2C2 + matrix.R2C2;
        }

        /// <summary>Add left matrix to left matrix.</summary>
        /// <param name="matrix">The matrix on the matrix side of the equation.</param>
        /// <param name="right">The matrix on the right side of the equation</param>
        /// <param name="result">The resulting matrix of the addition.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(ref Matrix3 left, ref Matrix3 right, out Matrix3 result)
        {
            result.R0C0 = left.R0C0 + right.R0C0;
            result.R0C1 = left.R0C1 + right.R0C1;
            result.R0C2 = left.R0C2 + right.R0C2;
            result.R1C0 = left.R1C0 + right.R1C0;
            result.R1C1 = left.R1C1 + right.R1C1;
            result.R1C2 = left.R1C2 + right.R1C2;
            result.R2C0 = left.R2C0 + right.R2C0;
            result.R2C1 = left.R2C1 + right.R2C1;
            result.R2C2 = left.R2C2 + right.R2C2;
        }

        /// <summary>Subtract matrix from this matrix.</summary>
        /// <param name="matrix">The matrix to subtract.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Subtract(ref Matrix3 matrix)
        {
            R0C0 = R0C0 - matrix.R0C0;
            R0C1 = R0C1 - matrix.R0C1;
            R0C2 = R0C2 - matrix.R0C2;
            R1C0 = R1C0 - matrix.R1C0;
            R1C1 = R1C1 - matrix.R1C1;
            R1C2 = R1C2 - matrix.R1C2;
            R2C0 = R2C0 - matrix.R2C0;
            R2C1 = R2C1 - matrix.R2C1;
            R2C2 = R2C2 - matrix.R2C2;
        }

        /// <summary>Subtract matrix from this matrix.</summary>
        /// <param name="matrix">The matrix to subtract.</param>
        /// <param name="result">The resulting matrix of the subtraction.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Subtract(ref Matrix3 matrix, out Matrix3 result)
        {
            result.R0C0 = R0C0 - matrix.R0C0;
            result.R0C1 = R0C1 - matrix.R0C1;
            result.R0C2 = R0C2 - matrix.R0C2;
            result.R1C0 = R1C0 - matrix.R1C0;
            result.R1C1 = R1C1 - matrix.R1C1;
            result.R1C2 = R1C2 - matrix.R1C2;
            result.R2C0 = R2C0 - matrix.R2C0;
            result.R2C1 = R2C1 - matrix.R2C1;
            result.R2C2 = R2C2 - matrix.R2C2;
        }

        /// <summary>Subtract right matrix from left matrix.</summary>
        /// <param name="left">The matrix on the matrix side of the equation.</param>
        /// <param name="right">The matrix on the right side of the equation</param>
        /// <param name="result">The resulting matrix of the subtraction.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Subtract(ref Matrix3 left, ref Matrix3 right, out Matrix3 result)
        {
            result.R0C0 = left.R0C0 - right.R0C0;
            result.R0C1 = left.R0C1 - right.R0C1;
            result.R0C2 = left.R0C2 - right.R0C2;
            result.R1C0 = left.R1C0 - right.R1C0;
            result.R1C1 = left.R1C1 - right.R1C1;
            result.R1C2 = left.R1C2 - right.R1C2;
            result.R2C0 = left.R2C0 - right.R2C0;
            result.R2C1 = left.R2C1 - right.R2C1;
            result.R2C2 = left.R2C2 - right.R2C2;
        }

        /// <summary>Multiply left matrix times this matrix.</summary>
        /// <param name="matrix">The matrix to multiply.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Multiply(in Matrix3 matrix)
        {
            var r0c0 = matrix.R0C0 * R0C0 + matrix.R0C1 * R1C0 + matrix.R0C2 * R2C0;
            var r0c1 = matrix.R0C0 * R0C1 + matrix.R0C1 * R1C1 + matrix.R0C2 * R2C1;
            var r0c2 = matrix.R0C0 * R0C2 + matrix.R0C1 * R1C2 + matrix.R0C2 * R2C2;

            var r1c0 = matrix.R1C0 * R0C0 + matrix.R1C1 * R1C0 + matrix.R1C2 * R2C0;
            var r1c1 = matrix.R1C0 * R0C1 + matrix.R1C1 * R1C1 + matrix.R1C2 * R2C1;
            var r1c2 = matrix.R1C0 * R0C2 + matrix.R1C1 * R1C2 + matrix.R1C2 * R2C2;

            R2C0 = matrix.R2C0 * R0C0 + matrix.R2C1 * R1C0 + matrix.R2C2 * R2C0;
            R2C1 = matrix.R2C0 * R0C1 + matrix.R2C1 * R1C1 + matrix.R2C2 * R2C1;
            R2C2 = matrix.R2C0 * R0C2 + matrix.R2C1 * R1C2 + matrix.R2C2 * R2C2;

            R0C0 = r0c0;
            R0C1 = r0c1;
            R0C2 = r0c2;

            R1C0 = r1c0;
            R1C1 = r1c1;
            R1C2 = r1c2;
        }

        /// <summary>Multiply matrix times this matrix.</summary>
        /// <param name="matrix">The matrix to multiply.</param>
        /// <param name="result">The resulting matrix of the multiplication.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Multiply(ref Matrix3 matrix, out Matrix3 result)
        {
            result.R0C0 = matrix.R0C0 * R0C0 + matrix.R0C1 * R1C0 + matrix.R0C2 * R2C0;
            result.R0C1 = matrix.R0C0 * R0C1 + matrix.R0C1 * R1C1 + matrix.R0C2 * R2C1;
            result.R0C2 = matrix.R0C0 * R0C2 + matrix.R0C1 * R1C2 + matrix.R0C2 * R2C2;
            result.R1C0 = matrix.R1C0 * R0C0 + matrix.R1C1 * R1C0 + matrix.R1C2 * R2C0;
            result.R1C1 = matrix.R1C0 * R0C1 + matrix.R1C1 * R1C1 + matrix.R1C2 * R2C1;
            result.R1C2 = matrix.R1C0 * R0C2 + matrix.R1C1 * R1C2 + matrix.R1C2 * R2C2;
            result.R2C0 = matrix.R2C0 * R0C0 + matrix.R2C1 * R1C0 + matrix.R2C2 * R2C0;
            result.R2C1 = matrix.R2C0 * R0C1 + matrix.R2C1 * R1C1 + matrix.R2C2 * R2C1;
            result.R2C2 = matrix.R2C0 * R0C2 + matrix.R2C1 * R1C2 + matrix.R2C2 * R2C2;
        }

        /// <summary>Multiply left matrix times right matrix.</summary>
        /// <param name="left">The matrix on the matrix side of the equation.</param>
        /// <param name="right">The matrix on the right side of the equation</param>
        /// <param name="result">The resulting matrix of the multiplication.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Multiply(ref Matrix3 left, ref Matrix3 right, out Matrix3 result)
        {
            result.R0C0 = right.R0C0 * left.R0C0 + right.R0C1 * left.R1C0 + right.R0C2 * left.R2C0;
            result.R0C1 = right.R0C0 * left.R0C1 + right.R0C1 * left.R1C1 + right.R0C2 * left.R2C1;
            result.R0C2 = right.R0C0 * left.R0C2 + right.R0C1 * left.R1C2 + right.R0C2 * left.R2C2;
            result.R1C0 = right.R1C0 * left.R0C0 + right.R1C1 * left.R1C0 + right.R1C2 * left.R2C0;
            result.R1C1 = right.R1C0 * left.R0C1 + right.R1C1 * left.R1C1 + right.R1C2 * left.R2C1;
            result.R1C2 = right.R1C0 * left.R0C2 + right.R1C1 * left.R1C2 + right.R1C2 * left.R2C2;
            result.R2C0 = right.R2C0 * left.R0C0 + right.R2C1 * left.R1C0 + right.R2C2 * left.R2C0;
            result.R2C1 = right.R2C0 * left.R0C1 + right.R2C1 * left.R1C1 + right.R2C2 * left.R2C1;
            result.R2C2 = right.R2C0 * left.R0C2 + right.R2C1 * left.R1C2 + right.R2C2 * left.R2C2;
        }

        /// <summary>Multiply matrix times this scalar.</summary>
        /// <param name="scalar">The scalar to multiply.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Multiply(float scalar)
        {
            R0C0 = scalar * R0C0;
            R0C1 = scalar * R0C1;
            R0C2 = scalar * R0C2;
            R1C0 = scalar * R1C0;
            R1C1 = scalar * R1C1;
            R1C2 = scalar * R1C2;
            R2C0 = scalar * R2C0;
            R2C1 = scalar * R2C1;
            R2C2 = scalar * R2C2;
        }

        /// <summary>Multiply matrix times this matrix.</summary>
        /// <param name="scalar">The scalar to multiply.</param>
        /// <param name="result">The resulting matrix of the multiplication.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Multiply(float scalar, out Matrix3 result)
        {
            result.R0C0 = scalar * R0C0;
            result.R0C1 = scalar * R0C1;
            result.R0C2 = scalar * R0C2;
            result.R1C0 = scalar * R1C0;
            result.R1C1 = scalar * R1C1;
            result.R1C2 = scalar * R1C2;
            result.R2C0 = scalar * R2C0;
            result.R2C1 = scalar * R2C1;
            result.R2C2 = scalar * R2C2;
        }

        /// <summary>Multiply left matrix times left matrix.</summary>
        /// <param name="matrix">The matrix on the matrix side of the equation.</param>
        /// <param name="scalar">The scalar on the right side of the equation</param>
        /// <param name="result">The resulting matrix of the multiplication.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Multiply(ref Matrix3 matrix, float scalar, out Matrix3 result)
        {
            result.R0C0 = scalar * matrix.R0C0;
            result.R0C1 = scalar * matrix.R0C1;
            result.R0C2 = scalar * matrix.R0C2;
            result.R1C0 = scalar * matrix.R1C0;
            result.R1C1 = scalar * matrix.R1C1;
            result.R1C2 = scalar * matrix.R1C2;
            result.R2C0 = scalar * matrix.R2C0;
            result.R2C1 = scalar * matrix.R2C1;
            result.R2C2 = scalar * matrix.R2C2;
        }

        #endregion Arithmetic Operators

        #region Functions

        public float Determinant
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => R0C0 * R1C1 * R2C2 - R0C0 * R1C2 * R2C1 - R0C1 * R1C0 * R2C2 + R0C2 * R1C0 * R2C1 + R0C1 * R1C2 * R2C0 - R0C2 * R1C1 * R2C0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Transpose()
        {
            MathHelper.Swap(ref R0C1, ref R1C0);
            MathHelper.Swap(ref R0C2, ref R2C0);
            MathHelper.Swap(ref R1C2, ref R2C1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Transpose(out Matrix3 result)
        {
            result.R0C0 = R0C0;
            result.R0C1 = R1C0;
            result.R0C2 = R2C0;
            result.R1C0 = R0C1;
            result.R1C1 = R1C1;
            result.R1C2 = R2C1;
            result.R2C0 = R0C2;
            result.R2C1 = R1C2;
            result.R2C2 = R2C2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transpose(ref Matrix3 matrix, out Matrix3 result)
        {
            result.R0C0 = matrix.R0C0;
            result.R0C1 = matrix.R1C0;
            result.R0C2 = matrix.R2C0;
            result.R1C0 = matrix.R0C1;
            result.R1C1 = matrix.R1C1;
            result.R1C2 = matrix.R2C1;
            result.R2C0 = matrix.R0C2;
            result.R2C1 = matrix.R1C2;
            result.R2C2 = matrix.R2C2;
        }

        /// <summary>
        /// Calculate the inverse of the given matrix
        /// </summary>
        /// <param name="mat">The matrix to invert</param>
        /// <returns>The inverse of the given matrix if it has one, or the input if it is singular</returns>
        /// <exception cref="InvalidOperationException">Thrown if the Matrix4 is singular.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3 Invert(Matrix3 mat)
        {
            var result = new Matrix3();
            mat.Invert(ref result);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invert(ref Matrix3 minv)
        {
            //Credit: https://stackoverflow.com/a/18504573

            var d = Determinant;
            if (MathHelper.CloseTo(d, 0))
                throw new InvalidOperationException("Matrix is singular and cannot be inverted.");

            var m = this;

            // computes the inverse of a matrix m
            double det = m.R0C0 * (m.R1C1 * m.R2C2 - m.R2C1 * m.R1C2) -
                         m.R0C1 * (m.R1C0 * m.R2C2 - m.R1C2 * m.R2C0) +
                         m.R0C2 * (m.R1C0 * m.R2C1 - m.R1C1 * m.R2C0);

            var invdet = 1 / det;

            minv.R0C0 = (float) ((m.R1C1 * m.R2C2 - m.R2C1 * m.R1C2) * invdet);
            minv.R0C1 = (float) ((m.R0C2 * m.R2C1 - m.R0C1 * m.R2C2) * invdet);
            minv.R0C2 = (float) ((m.R0C1 * m.R1C2 - m.R0C2 * m.R1C1) * invdet);
            minv.R1C0 = (float) ((m.R1C2 * m.R2C0 - m.R1C0 * m.R2C2) * invdet);
            minv.R1C1 = (float) ((m.R0C0 * m.R2C2 - m.R0C2 * m.R2C0) * invdet);
            minv.R1C2 = (float) ((m.R1C0 * m.R0C2 - m.R0C0 * m.R1C2) * invdet);
            minv.R2C0 = (float) ((m.R1C0 * m.R2C1 - m.R2C0 * m.R1C1) * invdet);
            minv.R2C1 = (float) ((m.R2C0 * m.R0C1 - m.R0C0 * m.R2C1) * invdet);
            minv.R2C2 = (float) ((m.R0C0 * m.R1C1 - m.R1C0 * m.R0C1) * invdet);
        }

        #endregion Functions

        #region Transformation Functions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Transform(ref Vector3 vector)
        {
            var x = R0C0 * vector.X + R0C1 * vector.Y + R0C2 * vector.Z;
            var y = R1C0 * vector.X + R1C1 * vector.Y + R1C2 * vector.Z;
            vector.Z = R2C0 * vector.X + R2C1 * vector.Y + R2C2 * vector.Z;
            vector.X = x;
            vector.Y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transform(in Matrix3 matrix, ref Vector3 vector)
        {
            var x = matrix.R0C0 * vector.X + matrix.R0C1 * vector.Y + matrix.R0C2 * vector.Z;
            var y = matrix.R1C0 * vector.X + matrix.R1C1 * vector.Y + matrix.R1C2 * vector.Z;
            vector.Z = matrix.R2C0 * vector.X + matrix.R2C1 * vector.Y + matrix.R2C2 * vector.Z;
            vector.X = x;
            vector.Y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transform(in Matrix3 matrix, ref Vector2 vector)
        {
            var vec3 = new Vector3(vector.X, vector.Y, 1);
            Transform(matrix, ref vec3);
            vector = vec3.Xy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 Transform(Vector2 vector)
        {
            return Transform(this, vector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Transform(in Matrix3 matrix, Vector2 vector)
        {
            var x = matrix.R0C0 * vector.X + matrix.R0C1 * vector.Y + matrix.R0C2;
            var y = matrix.R1C0 * vector.X + matrix.R1C1 * vector.Y + matrix.R1C2;

            return new Vector2(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Transform(ref Vector3 vector, out Vector3 result)
        {
            result.X = R0C0 * vector.X + R0C1 * vector.Y + R0C2 * vector.Z;
            result.Y = R1C0 * vector.X + R1C1 * vector.Y + R1C2 * vector.Z;
            result.Z = R2C0 * vector.X + R2C1 * vector.Y + R2C2 * vector.Z;
        }

        /// <summary>
        /// Post-multiplies a 3x3 matrix with a 3x1 vector.
        /// </summary>
        /// <param name="matrix">Matrix containing the transformation.</param>
        /// <param name="vector">Input vector to transform.</param>
        /// <param name="result">Transformed vector.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transform(in Matrix3 matrix, in Vector3 vector, out Vector3 result)
        {
            var x = matrix.R0C0 * vector.X + matrix.R0C1 * vector.Y + matrix.R0C2 * vector.Z;
            var y = matrix.R1C0 * vector.X + matrix.R1C1 * vector.Y + matrix.R1C2 * vector.Z;
            var z = matrix.R2C0 * vector.X + matrix.R2C1 * vector.Y + matrix.R2C2 * vector.Z;
            result = new Vector3(x, y, z);
        }

        /// <summary>
        /// Post-multiplies a 3x3 matrix with a 2x1 vector. The column-major 3x3 matrix is treated as
        /// a 3x2 matrix for this calculation.
        /// </summary>
        /// <param name="matrix">Matrix containing the transformation.</param>
        /// <param name="vector">Input vector to transform.</param>
        /// <param name="result">Transformed vector.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transform(in Matrix3 matrix, in Vector2 vector, out Vector2 result)
        {
            var x = matrix.R0C0 * vector.X + matrix.R0C1 * vector.Y + matrix.R0C2;
            var y = matrix.R1C0 * vector.X + matrix.R1C1 * vector.Y + matrix.R1C2;
            result = new Vector2(x, y);
        }

        /// <summary>
        /// Pre-multiples a 1x3 vector with a 3x3 matrix.
        /// </summary>
        /// <param name="matrix">Matrix containing the transformation.</param>
        /// <param name="vector">Input vector to transform.</param>
        /// <param name="result">Transformed vector.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transform(in Vector3 vector, in Matrix3 matrix, out Vector3 result)
        {
            var x = (vector.X * matrix.R0C0) + (vector.Y * matrix.R1C0) + (vector.Z * matrix.R2C0);
            var y = (vector.X * matrix.R0C1) + (vector.Y * matrix.R1C1) + (vector.Z * matrix.R2C1);
            var z = (vector.X * matrix.R0C2) + (vector.Y * matrix.R1C2) + (vector.Z * matrix.R2C2);
            result = new Vector3(x, y, z);
        }

        /// <summary>
        /// Pre-multiples a 1x2 vector with a 3x3 matrix. The row-major 3x3 matrix is treated as
        /// a 2x3 matrix for this calculation.
        /// </summary>
        /// <param name="matrix">Matrix containing the transformation.</param>
        /// <param name="vector">Input vector to transform.</param>
        /// <param name="result">Transformed vector.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transform(in Vector2 vector, in Matrix3 matrix, out Vector2 result)
        {
            var x = (vector.X * matrix.R0C0) + (vector.Y * matrix.R1C0) + (matrix.R2C0);
            var y = (vector.X * matrix.R0C1) + (vector.Y * matrix.R1C1) + (matrix.R2C1);
            result = new Vector2(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rotate(float angle)
        {
            var angleRadians = MathHelper.DegreesToRadians(angle);
            var sin = (float) Math.Sin(angleRadians);
            var cos = (float) Math.Cos(angleRadians);

            var r0c0 = cos * R0C0 + sin * R1C0;
            var r0c1 = cos * R0C1 + sin * R1C1;
            var r0c2 = cos * R0C2 + sin * R1C2;

            R1C0 = cos * R1C0 - sin * R0C0;
            R1C1 = cos * R1C1 - sin * R0C1;
            R1C2 = cos * R1C2 - sin * R0C2;

            R0C0 = r0c0;
            R0C1 = r0c1;
            R0C2 = r0c2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rotate(Angle angle)
        {
            var sin = (float) Math.Sin(angle);
            var cos = (float) Math.Cos(angle);

            var r0c0 = cos * R0C0 + sin * R1C0;
            var r0c1 = cos * R0C1 + sin * R1C1;
            var r0c2 = cos * R0C2 + sin * R1C2;

            R1C0 = cos * R1C0 - sin * R0C0;
            R1C1 = cos * R1C1 - sin * R0C1;
            R1C2 = cos * R1C2 - sin * R0C2;

            R0C0 = r0c0;
            R0C1 = r0c1;
            R0C2 = r0c2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rotate(float angle, out Matrix3 result)
        {
            var angleRadians = MathHelper.DegreesToRadians(angle);
            var sin = (float) Math.Sin(angleRadians);
            var cos = (float) Math.Cos(angleRadians);

            result.R0C0 = cos * R0C0 + sin * R1C0;
            result.R0C1 = cos * R0C1 + sin * R1C1;
            result.R0C2 = cos * R0C2 + sin * R1C2;
            result.R1C0 = cos * R1C0 - sin * R0C0;
            result.R1C1 = cos * R1C1 - sin * R0C1;
            result.R1C2 = cos * R1C2 - sin * R0C2;
            result.R2C0 = R2C0;
            result.R2C1 = R2C1;
            result.R2C2 = R2C2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Rotate(ref Matrix3 matrix, float angle, out Matrix3 result)
        {
            var angleRadians = MathHelper.DegreesToRadians(angle);
            var sin = (float) Math.Sin(angleRadians);
            var cos = (float) Math.Cos(angleRadians);

            result.R0C0 = cos * matrix.R0C0 + sin * matrix.R1C0;
            result.R0C1 = cos * matrix.R0C1 + sin * matrix.R1C1;
            result.R0C2 = cos * matrix.R0C2 + sin * matrix.R1C2;
            result.R1C0 = cos * matrix.R1C0 - sin * matrix.R0C0;
            result.R1C1 = cos * matrix.R1C1 - sin * matrix.R0C1;
            result.R1C2 = cos * matrix.R1C2 - sin * matrix.R0C2;
            result.R2C0 = matrix.R2C0;
            result.R2C1 = matrix.R2C1;
            result.R2C2 = matrix.R2C2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RotateMatrix(float angle, out Matrix3 result)
        {
            var angleRadians = MathHelper.DegreesToRadians(angle);
            var sin = (float) Math.Sin(angleRadians);
            var cos = (float) Math.Cos(angleRadians);

            result.R0C0 = cos;
            result.R0C1 = sin;
            result.R0C2 = 0;
            result.R1C0 = -sin;
            result.R1C1 = cos;
            result.R1C2 = 0;
            result.R2C0 = 0;
            result.R2C1 = 0;
            result.R2C2 = 1;
        }

        #endregion Transformation Functions

        #region Operator Overloads

        /// <summary>
        /// Post-multiplies a 3x3 matrix with a 3x1 vector.
        /// </summary>
        /// <param name="matrix">Matrix containing the transformation.</param>
        /// <param name="vector">Input vector to transform.</param>
        /// <returns>Transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(in Matrix3 matrix, in Vector3 vector)
        {
            Transform(in matrix, in vector, out var result);
            return result;
        }

        /// <summary>
        /// Post-multiplies a 3x3 matrix with a 2x1 vector. The 3x3 matrix is treated as
        /// a 3x2 matrix for this calculation.
        /// </summary>
        /// <param name="matrix">Matrix containing the transformation.</param>
        /// <param name="vector">Input vector to transform.</param>
        /// <returns>Transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(in Matrix3 matrix, in Vector2 vector)
        {
            Transform(in matrix, in vector, out var result);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(in Vector3 vector, in Matrix3 matrix)
        {
            Transform(in vector, in matrix, out var result);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(in Vector2 vector, in Matrix3 matrix)
        {
            Transform(in vector, in matrix, out var result);
            return result;
        }

        /// <summary>Multiply left matrix times right matrix.</summary>
        /// <param name="left">The matrix on the matrix side of the equation.</param>
        /// <param name="right">The matrix on the right side of the equation</param>
        /// <returns>The resulting matrix of the multiplication.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3 operator *(Matrix3 left, Matrix3 right)
        {
            Multiply(ref left, ref right, out var result);
            return result;
        }

        #endregion

        #region Constants

        /// <summary>The identity matrix.</summary>
        public static readonly Matrix3 Identity = new(
            1, 0, 0,
            0, 1, 0,
            0, 0, 1
        );

        /// <summary>A matrix of all zeros.</summary>
        public static readonly Matrix3 Zero = new(
            0, 0, 0,
            0, 0, 0,
            0, 0, 0
        );

        #endregion Constants

        #region HashCode

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            return
                R0C0.GetHashCode() ^ R0C1.GetHashCode() ^ R0C2.GetHashCode() ^
                R1C0.GetHashCode() ^ R1C1.GetHashCode() ^ R1C2.GetHashCode() ^
                R2C0.GetHashCode() ^ R2C1.GetHashCode() ^ R2C2.GetHashCode();
        }

        #endregion HashCode

        #region String

        /// <summary>Returns the fully qualified type name of this instance.</summary>
        /// <returns>A System.String containing left fully qualified type name.</returns>
        public override string ToString()
        {
            return $"|{R0C0}, {R0C1}, {R0C2}|\n"
                   + $"|{R1C0}, {R1C1}, {R1C2}|\n"
                   + $"|{R2C0}, {R2C1}, {R2C2}|\n";
        }

        #endregion String
    }
#pragma warning restore 3019
}
