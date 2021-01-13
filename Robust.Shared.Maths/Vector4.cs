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
using System.Xml.Serialization;

namespace Robust.Shared.Maths
{
    /// <summary>Represents a 4D vector using four single-precision floating-point numbers.</summary>
    /// <remarks>
    /// The Vector4 structure is suitable for interoperation with unmanaged code requiring four consecutive floats.
    /// </remarks>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector4 : IEquatable<Vector4>
    {
        #region Fields

        /// <summary>
        /// The X component of the Vector4.
        /// </summary>
        public float X;

        /// <summary>
        /// The Y component of the Vector4.
        /// </summary>
        public float Y;

        /// <summary>
        /// The Z component of the Vector4.
        /// </summary>
        public float Z;

        /// <summary>
        /// The W component of the Vector4.
        /// </summary>
        public float W;

        /// <summary>
        /// Defines a unit-length Vector4 that points towards the X-axis.
        /// </summary>
        public static readonly Vector4 UnitX = new(1, 0, 0, 0);

        /// <summary>
        /// Defines a unit-length Vector4 that points towards the Y-axis.
        /// </summary>
        public static readonly Vector4 UnitY = new(0, 1, 0, 0);

        /// <summary>
        /// Defines a unit-length Vector4 that points towards the Z-axis.
        /// </summary>
        public static readonly Vector4 UnitZ = new(0, 0, 1, 0);

        /// <summary>
        /// Defines a unit-length Vector4 that points towards the W-axis.
        /// </summary>
        public static readonly Vector4 UnitW = new(0, 0, 0, 1);

        /// <summary>
        /// Defines a zero-length Vector4.
        /// </summary>
        public static readonly Vector4 Zero = new(0, 0, 0, 0);

        /// <summary>
        /// Defines an instance with all components set to 1.
        /// </summary>
        public static readonly Vector4 One = new(1, 1, 1, 1);

        /// <summary>
        /// Defines the size of the Vector4 struct in bytes.
        /// </summary>
        public static readonly int SizeInBytes = Marshal.SizeOf(new Vector4());

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="value">The value that will initialize this instance.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4(float value)
        {
            X = value;
            Y = value;
            Z = value;
            W = value;
        }

        /// <summary>
        /// Constructs a new Vector4.
        /// </summary>
        /// <param name="x">The x component of the Vector4.</param>
        /// <param name="y">The y component of the Vector4.</param>
        /// <param name="z">The z component of the Vector4.</param>
        /// <param name="w">The w component of the Vector4.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        /// <summary>
        /// Constructs a new Vector4 from the given Vector2.
        /// </summary>
        /// <param name="v">The Vector2 to copy components from.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4(Vector2 v)
        {
            X = v.X;
            Y = v.Y;
            Z = 0.0f;
            W = 0.0f;
        }

        /// <summary>
        /// Constructs a new Vector4 from the given Vector3.
        /// The w component is initialized to 0.
        /// </summary>
        /// <param name="v">The Vector3 to copy components from.</param>
        /// <remarks><seealso cref="Vector4(Vector3, float)"/></remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4(Vector3 v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
            W = 0.0f;
        }

        /// <summary>
        /// Constructs a new Vector4 from the specified Vector3 and w component.
        /// </summary>
        /// <param name="v">The Vector3 to copy components from.</param>
        /// <param name="w">The w component of the new Vector4.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4(Vector3 v, float w)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
            W = w;
        }

        /// <summary>
        /// Constructs a new Vector4 from the given Vector4.
        /// </summary>
        /// <param name="v">The Vector4 to copy components from.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4(Vector4 v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
            W = v.W;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out float x, out float y, out float z, out float w)
        {
            x = X;
            y = Y;
            z = Z;
            w = W;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector4((float x, float y, float z, float w) tuple)
        {
            var (x, y, z, w) = tuple;
            return new Vector4(x, y, z, w);
        }

        #endregion Constructors

        #region Public Members

        #region Instance

        #region public float Length

        /// <summary>
        /// Gets the length (magnitude) of the vector.
        /// </summary>
        /// <seealso cref="LengthSquared"/>
        public float Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MathF.Sqrt(LengthSquared);
        }

        #endregion public float Length

        #region public float LengthSquared

        /// <summary>
        /// Gets the square of the vector length (magnitude).
        /// </summary>
        /// <remarks>
        /// This property avoids the costly square root operation required by the Length property. This makes it more suitable
        /// for comparisons.
        /// </remarks>
        /// <see cref="Length"/>
        public float LengthSquared
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => X * X + Y * Y + Z * Z + W * W;
        }

        #endregion public float LengthSquared

        #region public void Normalize()

        /// <summary>
        /// Scales the Vector4 to unit length.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize()
        {
            var scale = 1.0f / Length;
            X *= scale;
            Y *= scale;
            Z *= scale;
            W *= scale;
        }

        #endregion public void Normalize()

        #endregion Instance

        #region Static

        #region Add

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>Result of operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Add(Vector4 a, Vector4 b)
        {
            Add(ref a, ref b, out a);
            return a;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <param name="result">Result of operation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(ref Vector4 a, ref Vector4 b, out Vector4 result)
        {
            result = new Vector4(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
        }

        #endregion Add

        #region Subtract

        /// <summary>
        /// Subtract one Vector from another
        /// </summary>
        /// <param name="a">First operand</param>
        /// <param name="b">Second operand</param>
        /// <returns>Result of subtraction</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Subtract(Vector4 a, Vector4 b)
        {
            Subtract(ref a, ref b, out a);
            return a;
        }

        /// <summary>
        /// Subtract one Vector from another
        /// </summary>
        /// <param name="a">First operand</param>
        /// <param name="b">Second operand</param>
        /// <param name="result">Result of subtraction</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Subtract(ref Vector4 a, ref Vector4 b, out Vector4 result)
        {
            result = new Vector4(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
        }

        #endregion Subtract

        #region Multiply

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="vector">Left operand.</param>
        /// <param name="scale">Right operand.</param>
        /// <returns>Result of the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Multiply(Vector4 vector, float scale)
        {
            Multiply(ref vector, scale, out vector);
            return vector;
        }

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="vector">Left operand.</param>
        /// <param name="scale">Right operand.</param>
        /// <param name="result">Result of the operation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Multiply(ref Vector4 vector, float scale, out Vector4 result)
        {
            result = new Vector4(vector.X * scale, vector.Y * scale, vector.Z * scale, vector.W * scale);
        }

        /// <summary>
        /// Multiplies a vector by the components a vector (scale).
        /// </summary>
        /// <param name="vector">Left operand.</param>
        /// <param name="scale">Right operand.</param>
        /// <returns>Result of the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Multiply(Vector4 vector, Vector4 scale)
        {
            Multiply(ref vector, ref scale, out vector);
            return vector;
        }

        /// <summary>
        /// Multiplies a vector by the components of a vector (scale).
        /// </summary>
        /// <param name="vector">Left operand.</param>
        /// <param name="scale">Right operand.</param>
        /// <param name="result">Result of the operation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Multiply(ref Vector4 vector, ref Vector4 scale, out Vector4 result)
        {
            result = new Vector4(vector.X * scale.X, vector.Y * scale.Y, vector.Z * scale.Z, vector.W * scale.W);
        }

        #endregion Multiply

        #region Divide

        /// <summary>
        /// Divides a vector by a scalar.
        /// </summary>
        /// <param name="vector">Left operand.</param>
        /// <param name="scale">Right operand.</param>
        /// <returns>Result of the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Divide(Vector4 vector, float scale)
        {
            Divide(ref vector, scale, out vector);
            return vector;
        }

        /// <summary>
        /// Divides a vector by a scalar.
        /// </summary>
        /// <param name="vector">Left operand.</param>
        /// <param name="scale">Right operand.</param>
        /// <param name="result">Result of the operation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Divide(ref Vector4 vector, float scale, out Vector4 result)
        {
            Multiply(ref vector, 1 / scale, out result);
        }

        /// <summary>
        /// Divides a vector by the components of a vector (scale).
        /// </summary>
        /// <param name="vector">Left operand.</param>
        /// <param name="scale">Right operand.</param>
        /// <returns>Result of the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Divide(Vector4 vector, Vector4 scale)
        {
            Divide(ref vector, ref scale, out vector);
            return vector;
        }

        /// <summary>
        /// Divide a vector by the components of a vector (scale).
        /// </summary>
        /// <param name="vector">Left operand.</param>
        /// <param name="scale">Right operand.</param>
        /// <param name="result">Result of the operation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Divide(ref Vector4 vector, ref Vector4 scale, out Vector4 result)
        {
            result = new Vector4(vector.X / scale.X, vector.Y / scale.Y, vector.Z / scale.Z, vector.W / scale.W);
        }

        #endregion Divide

        #region Min

        /// <summary>
        /// Calculate the component-wise minimum of two vectors
        /// </summary>
        /// <param name="a">First operand</param>
        /// <param name="b">Second operand</param>
        /// <returns>The component-wise minimum</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Min(Vector4 a, Vector4 b)
        {
            a.X = MathF.Min(a.X, b.X);
            a.Y = MathF.Min(a.Y, b.Y);
            a.Z = MathF.Min(a.Z, b.Z);
            a.W = MathF.Min(a.W, b.W);
            return a;
        }

        /// <summary>
        /// Calculate the component-wise minimum of two vectors
        /// </summary>
        /// <param name="a">First operand</param>
        /// <param name="b">Second operand</param>
        /// <param name="result">The component-wise minimum</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Min(ref Vector4 a, ref Vector4 b, out Vector4 result)
        {
            result.X = MathF.Min(a.X, b.X);
            result.Y = MathF.Min(a.Y, b.Y);
            result.Z = MathF.Min(a.Z, b.Z);
            result.W = MathF.Min(a.W, b.W);
        }

        #endregion Min

        #region Max

        /// <summary>
        /// Calculate the component-wise maximum of two vectors
        /// </summary>
        /// <param name="a">First operand</param>
        /// <param name="b">Second operand</param>
        /// <returns>The component-wise maximum</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Max(Vector4 a, Vector4 b)
        {
            a.X = MathF.Max(a.X, b.X);
            a.Y = MathF.Max(a.Y, b.Y);
            a.Z = MathF.Max(a.Z, b.Z);
            a.W = MathF.Max(a.W, b.W);
            return a;
        }

        /// <summary>
        /// Calculate the component-wise maximum of two vectors
        /// </summary>
        /// <param name="a">First operand</param>
        /// <param name="b">Second operand</param>
        /// <param name="result">The component-wise maximum</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Max(ref Vector4 a, ref Vector4 b, out Vector4 result)
        {
            result.X = MathF.Max(a.X, b.X);
            result.Y = MathF.Max(a.Y, b.Y);
            result.Z = MathF.Max(a.Z, b.Z);
            result.W = MathF.Max(a.W, b.W);
        }

        #endregion Max

        #region Clamp

        /// <summary>
        /// Clamp a vector to the given minimum and maximum vectors
        /// </summary>
        /// <param name="vec">Input vector</param>
        /// <param name="min">Minimum vector</param>
        /// <param name="max">Maximum vector</param>
        /// <returns>The clamped vector</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Clamp(Vector4 vec, Vector4 min, Vector4 max)
        {
            vec.X = MathHelper.Clamp( vec.X, min.X, max.X );
            vec.Y = MathHelper.Clamp( vec.Y, min.Y, max.Y );
            vec.Z = MathHelper.Clamp( vec.Z, min.Z, max.Z );
            vec.W = MathHelper.Clamp( vec.W, min.W, max.W );
            return vec;
        }

        /// <summary>
        /// Clamp a vector to the given minimum and maximum vectors
        /// </summary>
        /// <param name="vec">Input vector</param>
        /// <param name="min">Minimum vector</param>
        /// <param name="max">Maximum vector</param>
        /// <param name="result">The clamped vector</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clamp(ref Vector4 vec, ref Vector4 min, ref Vector4 max, out Vector4 result)
        {
            result.X = MathHelper.Clamp( vec.X, min.X, max.X );
            result.Y = MathHelper.Clamp( vec.Y, min.Y, max.Y );
            result.Z = MathHelper.Clamp( vec.Z, min.Z, max.Z );
            result.W = MathHelper.Clamp( vec.W, min.W, max.W );
        }

        #endregion Clamp

        #region Normalize

        /// <summary>
        /// Scale a vector to unit length
        /// </summary>
        /// <param name="vec">The input vector</param>
        /// <returns>The normalized vector</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Normalize(Vector4 vec)
        {
            var scale = 1.0f / vec.Length;
            vec.X *= scale;
            vec.Y *= scale;
            vec.Z *= scale;
            vec.W *= scale;
            return vec;
        }

        /// <summary>
        /// Scale a vector to unit length
        /// </summary>
        /// <param name="vec">The input vector</param>
        /// <param name="result">The normalized vector</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Normalize(ref Vector4 vec, out Vector4 result)
        {
            var scale = 1.0f / vec.Length;
            result.X = vec.X * scale;
            result.Y = vec.Y * scale;
            result.Z = vec.Z * scale;
            result.W = vec.W * scale;
        }

        #endregion Normalize

        #region Dot

        /// <summary>
        /// Calculate the dot product of two vectors
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <returns>The dot product of the two inputs</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(Vector4 left, Vector4 right)
        {
            return left.X * right.X + left.Y * right.Y + left.Z * right.Z + left.W * right.W;
        }

        /// <summary>
        /// Calculate the dot product of two vectors
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <param name="result">The dot product of the two inputs</param>
        public static void Dot(ref Vector4 left, ref Vector4 right, out float result)
        {
            result = left.X * right.X + left.Y * right.Y + left.Z * right.Z + left.W * right.W;
        }

        #endregion Dot

        #region Lerp

        /// <summary>
        /// Returns a new Vector that is the linear blend of the 2 given Vectors
        /// </summary>
        /// <param name="a">First input vector</param>
        /// <param name="b">Second input vector</param>
        /// <param name="blend">The blend factor. a when blend=0, b when blend=1.</param>
        /// <returns>a when blend=0, b when blend=1, and a linear combination otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Lerp(Vector4 a, Vector4 b, float blend)
        {
            a.X = blend * (b.X - a.X) + a.X;
            a.Y = blend * (b.Y - a.Y) + a.Y;
            a.Z = blend * (b.Z - a.Z) + a.Z;
            a.W = blend * (b.W - a.W) + a.W;
            return a;
        }

        /// <summary>
        /// Returns a new Vector that is the linear blend of the 2 given Vectors
        /// </summary>
        /// <param name="a">First input vector</param>
        /// <param name="b">Second input vector</param>
        /// <param name="blend">The blend factor. a when blend=0, b when blend=1.</param>
        /// <param name="result">a when blend=0, b when blend=1, and a linear combination otherwise</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Lerp(ref Vector4 a, ref Vector4 b, float blend, out Vector4 result)
        {
            result.X = MathHelper.Lerp(a.X, b.X, blend);
            result.Y = MathHelper.Lerp(a.Y, b.Y, blend);
            result.Z = MathHelper.Lerp(a.Z, b.Z, blend);
            result.W = MathHelper.Lerp(a.W, b.W, blend);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 InterpolateCubic(Vector4 preA, Vector4 a, Vector4 b, Vector4 postB, float t)
        {
            return a +
                   (b - preA + (preA * 2.0f - a * 5.0f + b * 4.0f - postB + ((a - b) * 3.0f + postB - preA) * t) * t) *
                   t * 0.5f;
        }

        #endregion Lerp

        #region Barycentric

        /// <summary>
        /// Interpolate 3 Vectors using Barycentric coordinates
        /// </summary>
        /// <param name="a">First input Vector</param>
        /// <param name="b">Second input Vector</param>
        /// <param name="c">Third input Vector</param>
        /// <param name="u">First Barycentric Coordinate</param>
        /// <param name="v">Second Barycentric Coordinate</param>
        /// <returns>a when u=v=0, b when u=1,v=0, c when u=0,v=1, and a linear combination of a,b,c otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 BaryCentric(Vector4 a, Vector4 b, Vector4 c, float u, float v)
        {
            return a + u * (b - a) + v * (c - a);
        }

        /// <summary>Interpolate 3 Vectors using Barycentric coordinates</summary>
        /// <param name="a">First input Vector.</param>
        /// <param name="b">Second input Vector.</param>
        /// <param name="c">Third input Vector.</param>
        /// <param name="u">First Barycentric Coordinate.</param>
        /// <param name="v">Second Barycentric Coordinate.</param>
        /// <param name="result">Output Vector. a when u=v=0, b when u=1,v=0, c when u=0,v=1, and a linear combination of a,b,c otherwise</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BaryCentric(ref Vector4 a, ref Vector4 b, ref Vector4 c, float u, float v, out Vector4 result)
        {
            result = a; // copy

            var temp = b; // copy
            Subtract(ref temp, ref a, out temp);
            Multiply(ref temp, u, out temp);
            Add(ref result, ref temp, out result);

            temp = c; // copy
            Subtract(ref temp, ref a, out temp);
            Multiply(ref temp, v, out temp);
            Add(ref result, ref temp, out result);
        }

        #endregion Barycentric

        #region Transform

        /// <summary>Transform a Vector by the given Matrix</summary>
        /// <param name="vec">The vector to transform</param>
        /// <param name="mat">The desired transformation</param>
        /// <returns>The transformed vector</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Transform(Vector4 vec, Matrix4 mat)
        {
            Transform(ref vec, ref mat, out var result);
            return result;
        }

        /// <summary>Transform a Vector by the given Matrix</summary>
        /// <param name="vec">The vector to transform</param>
        /// <param name="mat">The desired transformation</param>
        /// <param name="result">The transformed vector</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transform(ref Vector4 vec, ref Matrix4 mat, out Vector4 result)
        {
            result = new Vector4(
                vec.X * mat.Row0.X + vec.Y * mat.Row1.X + vec.Z * mat.Row2.X + vec.W * mat.Row3.X,
                vec.X * mat.Row0.Y + vec.Y * mat.Row1.Y + vec.Z * mat.Row2.Y + vec.W * mat.Row3.Y,
                vec.X * mat.Row0.Z + vec.Y * mat.Row1.Z + vec.Z * mat.Row2.Z + vec.W * mat.Row3.Z,
                vec.X * mat.Row0.W + vec.Y * mat.Row1.W + vec.Z * mat.Row2.W + vec.W * mat.Row3.W);
        }

        /// <summary>
        /// Transforms a vector by a quaternion rotation.
        /// </summary>
        /// <param name="vec">The vector to transform.</param>
        /// <param name="quat">The quaternion to rotate the vector by.</param>
        /// <returns>The result of the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Transform(Vector4 vec, Quaternion quat)
        {
            Transform(ref vec, ref quat, out var result);
            return result;
        }

        /// <summary>
        /// Transforms a vector by a quaternion rotation.
        /// </summary>
        /// <param name="vec">The vector to transform.</param>
        /// <param name="quat">The quaternion to rotate the vector by.</param>
        /// <param name="result">The result of the operation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transform(ref Vector4 vec, ref Quaternion quat, out Vector4 result)
        {
            var v = new Quaternion(vec.X, vec.Y, vec.Z, vec.W);
            Quaternion.Invert(ref quat, out var i);
            Quaternion.Multiply(ref quat, ref v, out var t);
            Quaternion.Multiply(ref t, ref i, out v);

            result = new Vector4(v.X, v.Y, v.Z, v.W);
        }

        #endregion Transform

        #endregion Static

        #region Swizzle

        /// <summary>
        /// Gets or sets an OpenTK.Vector2 with the X and Y components of this instance.
        /// </summary>
        [XmlIgnore]
        public Vector2 Xy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(X, Y);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        /// <summary>
        /// Gets or sets an OpenTK.Vector3 with the X, Y and Z components of this instance.
        /// </summary>
        [XmlIgnore]
        public Vector3 Xyz
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(X, Y, Z);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }

        #endregion Swizzle

        #region Operators

        /// <summary>
        /// Adds two instances.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>The result of the calculation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator +(Vector4 left, Vector4 right)
        {
            left.X += right.X;
            left.Y += right.Y;
            left.Z += right.Z;
            left.W += right.W;
            return left;
        }

        /// <summary>
        /// Subtracts two instances.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>The result of the calculation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator -(Vector4 left, Vector4 right)
        {
            left.X -= right.X;
            left.Y -= right.Y;
            left.Z -= right.Z;
            left.W -= right.W;
            return left;
        }

        /// <summary>
        /// Negates an instance.
        /// </summary>
        /// <param name="vec">The instance.</param>
        /// <returns>The result of the calculation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator -(Vector4 vec)
        {
            vec.X = -vec.X;
            vec.Y = -vec.Y;
            vec.Z = -vec.Z;
            vec.W = -vec.W;
            return vec;
        }

        /// <summary>
        /// Multiplies an instance by a scalar.
        /// </summary>
        /// <param name="vec">The instance.</param>
        /// <param name="scale">The scalar.</param>
        /// <returns>The result of the calculation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator *(Vector4 vec, float scale)
        {
            vec.X *= scale;
            vec.Y *= scale;
            vec.Z *= scale;
            vec.W *= scale;
            return vec;
        }

        /// <summary>
        /// Multiplies an instance by a scalar.
        /// </summary>
        /// <param name="scale">The scalar.</param>
        /// <param name="vec">The instance.</param>
        /// <returns>The result of the calculation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator *(float scale, Vector4 vec)
        {
            vec.X *= scale;
            vec.Y *= scale;
            vec.Z *= scale;
            vec.W *= scale;
            return vec;
        }

        /// <summary>
        /// Divides an instance by a scalar.
        /// </summary>
        /// <param name="vec">The instance.</param>
        /// <param name="scale">The scalar.</param>
        /// <returns>The result of the calculation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator /(Vector4 vec, float scale)
        {
            var mult = 1.0f / scale;
            vec.X *= mult;
            vec.Y *= mult;
            vec.Z *= mult;
            vec.W *= mult;
            return vec;
        }

        /// <summary>
        /// Compares two instances for equality.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>True, if left equals right; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector4 left, Vector4 right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two instances for inequality.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>True, if left does not equal right; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector4 left, Vector4 right)
        {
            return !left.Equals(right);
        }

        #endregion Operators

        #region Overrides

        #region public override string ToString()

        /// <summary>
        /// Returns a System.String that represents the current Vector4.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"({X}, {Y}, {Z}, {W})";
        }

        #endregion public override string ToString()

        #region public override int GetHashCode()

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A System.Int32 containing the unique hashcode for this instance.</returns>
        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ W.GetHashCode();
        }

        #endregion public override int GetHashCode()

        #region public override bool Equals(object obj)

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>True if the instances are equal; false otherwise.</returns>
        public override bool Equals(object? obj)
        {
            if (!(obj is Vector4))
                return false;

            return Equals((Vector4) obj);
        }

        #endregion public override bool Equals(object obj)

        #endregion Overrides

        #endregion Public Members

        #region IEquatable<Vector4> Members

        /// <summary>Indicates whether the current vector is equal to another vector.</summary>
        /// <param name="other">A vector to compare with this vector.</param>
        /// <returns>true if the current vector is equal to the vector parameter; otherwise, false.</returns>
        public bool Equals(Vector4 other)
        {
            return
                X == other.X &&
                Y == other.Y &&
                Z == other.Z &&
                W == other.W;
        }

        #endregion IEquatable<Vector4> Members
    }
}
