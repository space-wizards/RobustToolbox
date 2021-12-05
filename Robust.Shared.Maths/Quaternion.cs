#region --- License ---

/*
Copyright (c) 2006 - 2008 The Open Toolkit library.
Copyright 2013 Xamarin Inc

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
    /// <summary>
    /// Represents a Quaternion.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Quaternion : IEquatable<Quaternion>
    {
        #region Fields

        private Vector3 xyz;
        private float w;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Construct a new Quaternion from vector and w components
        /// </summary>
        /// <param name="v">The vector part</param>
        /// <param name="w">The w part</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion(Vector3 v, float w)
        {
            xyz = v;
            this.w = w;
        }

        /// <summary>
        /// Construct a new Quaternion
        /// </summary>
        /// <param name="x">The x component</param>
        /// <param name="y">The y component</param>
        /// <param name="z">The z component</param>
        /// <param name="w">The w component</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion(float x, float y, float z, float w)
            : this(new Vector3(x, y, z), w) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion(ref Matrix3 matrix)
        {
            var scale = Math.Pow(matrix.Determinant, 1.0d / 3.0d);
            float x, y, z;

            w = (float) (Math.Sqrt(Math.Max(0, scale + matrix[0, 0] + matrix[1, 1] + matrix[2, 2])) / 2);
            x = (float) (Math.Sqrt(Math.Max(0, scale + matrix[0, 0] - matrix[1, 1] - matrix[2, 2])) / 2);
            y = (float) (Math.Sqrt(Math.Max(0, scale - matrix[0, 0] + matrix[1, 1] - matrix[2, 2])) / 2);
            z = (float) (Math.Sqrt(Math.Max(0, scale - matrix[0, 0] - matrix[1, 1] + matrix[2, 2])) / 2);

            xyz = new Vector3(x, y, z);

            if (matrix[2, 1] - matrix[1, 2] < 0) X = -X;
            if (matrix[0, 2] - matrix[2, 0] < 0) Y = -Y;
            if (matrix[1, 0] - matrix[0, 1] < 0) Z = -Z;
        }

        #endregion Constructors

        #region Public Members

        #region Properties

        /// <summary>
        /// Gets or sets an OpenTK.Vector3 with the X, Y and Z components of this instance.
        /// </summary>
        public Vector3 Xyz
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => xyz;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => xyz = value;
        }

        /// <summary>
        /// Gets or sets the X component of this instance.
        /// </summary>
        [XmlIgnore]
        public float X
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => xyz.X;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => xyz.X = value;
        }

        /// <summary>
        /// Gets or sets the Y component of this instance.
        /// </summary>
        [XmlIgnore]
        public float Y
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => xyz.Y;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => xyz.Y = value;
        }

        /// <summary>
        /// Gets or sets the Z component of this instance.
        /// </summary>
        [XmlIgnore]
        public float Z
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => xyz.Z;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => xyz.Z = value;
        }

        /// <summary>
        /// Gets or sets the W component of this instance.
        /// </summary>
        public float W
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => w;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => w = value;
        }

        public float x
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => xyz.X;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => xyz.X = value;
        }

        public float y
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => xyz.Y;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => xyz.Y = value;
        }

        public float z
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => xyz.Z;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => xyz.Z = value;
        }

        #endregion Properties

        #region Instance

        #region ToAxisAngle

        /// <summary>
        /// Convert the current quaternion to axis angle representation
        /// </summary>
        /// <param name="axis">The resultant axis</param>
        /// <param name="angle">The resultant angle</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToAxisAngle(out Vector3 axis, out float angle)
        {
            var result = ToAxisAngle();
            axis = result.Xyz;
            angle = result.W;
        }

        /// <summary>
        /// Convert this instance to an axis-angle representation.
        /// </summary>
        /// <returns>A Vector4 that is the axis-angle representation of this quaternion.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4 ToAxisAngle()
        {
            var q = this;
            if (Math.Abs(q.W) > 1.0f)
                q.Normalize();

            var result = new Vector4();

            result.W = 2.0f * (float) Math.Acos(q.W); // angle
            var den = (float) Math.Sqrt(1.0 - q.W * q.W);
            if (den > 0.0001f)
            {
                result.Xyz = q.Xyz / den;
            }
            else
            {
                // This occurs when the angle is zero.
                // Not a problem: just set an arbitrary normalized axis.
                result.Xyz = Vector3.UnitX;
            }

            return result;
        }

        #endregion ToAxisAngle

        #region public float Length

        /// <summary>
        /// Gets the length (magnitude) of the quaternion.
        /// </summary>
        /// <seealso cref="LengthSquared"/>
        public float Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (float) Math.Sqrt(W * W + Xyz.LengthSquared);
        }

        #endregion public float Length

        #region public float LengthSquared

        /// <summary>
        /// Gets the square of the quaternion length (magnitude).
        /// </summary>
        public float LengthSquared
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => W * W + Xyz.LengthSquared;
        }

        #endregion public float LengthSquared

        #region public void Normalize()

        /// <summary>
        /// Scales the Quaternion to unit length.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize()
        {
            var scale = 1.0f / Length;
            Xyz *= scale;
            W *= scale;
        }

        #endregion public void Normalize()

        #region public void Conjugate()

        /// <summary>
        /// Convert this quaternion to its conjugate
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Conjugate()
        {
            Xyz = -Xyz;
        }

        #endregion public void Conjugate()

        #endregion Instance

        #region Static

        #region Fields

        private const float RadToDeg = (float) (180.0 / Math.PI);
        private const float DegToRad = (float) (Math.PI / 180.0);

        /// <summary>
        /// Defines the identity quaternion.
        /// </summary>
        public static readonly Quaternion Identity = new(0, 0, 0, 1);

        #endregion Fields

        #region Add

        /// <summary>
        /// Add two quaternions
        /// </summary>
        /// <param name="left">The first operand</param>
        /// <param name="right">The second operand</param>
        /// <returns>The result of the addition</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Add(Quaternion left, Quaternion right)
        {
            return new(
                left.Xyz + right.Xyz,
                left.W + right.W);
        }

        /// <summary>
        /// Add two quaternions
        /// </summary>
        /// <param name="left">The first operand</param>
        /// <param name="right">The second operand</param>
        /// <param name="result">The result of the addition</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(ref Quaternion left, ref Quaternion right, out Quaternion result)
        {
            result = new Quaternion(
                left.Xyz + right.Xyz,
                left.W + right.W);
        }

        #endregion Add

        #region Sub

        /// <summary>
        /// Subtracts two instances.
        /// </summary>
        /// <param name="left">The left instance.</param>
        /// <param name="right">The right instance.</param>
        /// <returns>The result of the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Sub(Quaternion left, Quaternion right)
        {
            return new(
                left.Xyz - right.Xyz,
                left.W - right.W);
        }

        /// <summary>
        /// Subtracts two instances.
        /// </summary>
        /// <param name="left">The left instance.</param>
        /// <param name="right">The right instance.</param>
        /// <param name="result">The result of the operation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sub(ref Quaternion left, ref Quaternion right, out Quaternion result)
        {
            result = new Quaternion(
                left.Xyz - right.Xyz,
                left.W - right.W);
        }

        #endregion Sub

        #region Mult

        /// <summary>
        /// Multiplies two instances.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>A new instance containing the result of the calculation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Multiply(Quaternion left, Quaternion right)
        {
            Multiply(ref left, ref right, out var result);
            return result;
        }

        /// <summary>
        /// Multiplies two instances.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <param name="result">A new instance containing the result of the calculation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Multiply(ref Quaternion left, ref Quaternion right, out Quaternion result)
        {
            result = new Quaternion(
                right.W * left.Xyz + left.W * right.Xyz + Vector3.Cross(left.Xyz, right.Xyz),
                left.W * right.W - Vector3.Dot(left.Xyz, right.Xyz));
        }

        /// <summary>
        /// Multiplies an instance by a scalar.
        /// </summary>
        /// <param name="quaternion">The instance.</param>
        /// <param name="scale">The scalar.</param>
        /// <param name="result">A new instance containing the result of the calculation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Multiply(ref Quaternion quaternion, float scale, out Quaternion result)
        {
            result = new Quaternion(quaternion.X * scale, quaternion.Y * scale, quaternion.Z * scale, quaternion.W * scale);
        }

        /// <summary>
        /// Multiplies an instance by a scalar.
        /// </summary>
        /// <param name="quaternion">The instance.</param>
        /// <param name="scale">The scalar.</param>
        /// <returns>A new instance containing the result of the calculation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Multiply(Quaternion quaternion, float scale)
        {
            return new(quaternion.X * scale, quaternion.Y * scale, quaternion.Z * scale, quaternion.W * scale);
        }

        #endregion Mult

        #region Dot

        /// <summary>
        ///     Calculates the dot product between two Quaternions.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(Quaternion a, Quaternion b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
        }

        #endregion Dot

        #region Conjugate

        /// <summary>
        /// Get the conjugate of the given quaternion
        /// </summary>
        /// <param name="q">The quaternion</param>
        /// <returns>The conjugate of the given quaternion</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Conjugate(Quaternion q)
        {
            return new(-q.Xyz, q.W);
        }

        /// <summary>
        /// Get the conjugate of the given quaternion
        /// </summary>
        /// <param name="q">The quaternion</param>
        /// <param name="result">The conjugate of the given quaternion</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Conjugate(ref Quaternion q, out Quaternion result)
        {
            result = new Quaternion(-q.Xyz, q.W);
        }

        #endregion Conjugate

        #region Invert

        /// <summary>
        /// Get the inverse of the given quaternion
        /// </summary>
        /// <param name="q">The quaternion to invert</param>
        /// <returns>The inverse of the given quaternion</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Invert(Quaternion q)
        {
            Invert(ref q, out var result);
            return result;
        }

        /// <summary>
        /// Get the inverse of the given quaternion
        /// </summary>
        /// <param name="q">The quaternion to invert</param>
        /// <param name="result">The inverse of the given quaternion</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Invert(ref Quaternion q, out Quaternion result)
        {
            var lengthSq = q.LengthSquared;
            if (lengthSq != 0.0)
            {
                var i = 1.0f / lengthSq;
                result = new Quaternion(q.Xyz * -i, q.W * i);
            }
            else
            {
                result = q;
            }
        }

        #endregion Invert

        #region Normalize

        /// <summary>
        /// Scale the given quaternion to unit length
        /// </summary>
        /// <param name="q">The quaternion to normalize</param>
        /// <returns>The normalized quaternion</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Normalize(Quaternion q)
        {
            Normalize(ref q, out var result);
            return result;
        }

        /// <summary>
        /// Scale the given quaternion to unit length
        /// </summary>
        /// <param name="q">The quaternion to normalize</param>
        /// <param name="result">The normalized quaternion</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Normalize(ref Quaternion q, out Quaternion result)
        {
            var scale = 1.0f / q.Length;
            result = new Quaternion(q.Xyz * scale, q.W * scale);
        }

        #endregion Normalize

        #region FromAxisAngle

        /// <summary>
        /// Build a quaternion from the given axis and angle
        /// </summary>
        /// <param name="axis">The axis to rotate about</param>
        /// <param name="angle">The rotation angle in radians</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion FromAxisAngle(Vector3 axis, float angle)
        {
            if (axis.LengthSquared == 0.0f)
                return Identity;

            var result = Identity;

            angle *= 0.5f;
            axis.Normalize();
            result.Xyz = axis * (float) Math.Sin(angle);
            result.W = (float) Math.Cos(angle);

            return Normalize(result);
        }

        #endregion FromAxisAngle

        #region Slerp

        /// <summary>
        /// Do Spherical linear interpolation between two quaternions
        /// </summary>
        /// <param name="q1">The first quaternion</param>
        /// <param name="q2">The second quaternion</param>
        /// <param name="blend">The blend factor</param>
        /// <returns>A smooth blend between the given quaternions</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Slerp(Quaternion q1, Quaternion q2, float blend)
        {
            // if either input is zero, return the other.
            if (q1.LengthSquared == 0.0f)
            {
                if (q2.LengthSquared == 0.0f)
                {
                    return Identity;
                }

                return q2;
            }

            if (q2.LengthSquared == 0.0f)
            {
                return q1;
            }

            var cosHalfAngle = q1.W * q2.W + Vector3.Dot(q1.Xyz, q2.Xyz);

            if (cosHalfAngle >= 1.0f || cosHalfAngle <= -1.0f)
            {
                // angle = 0.0f, so just return one input.
                return q1;
            }

            if (cosHalfAngle < 0.0f)
            {
                q2.Xyz = -q2.Xyz;
                q2.W = -q2.W;
                cosHalfAngle = -cosHalfAngle;
            }

            float blendA;
            float blendB;
            if (cosHalfAngle < 0.99f)
            {
                // do proper slerp for big angles
                var halfAngle = (float) Math.Acos(cosHalfAngle);
                var sinHalfAngle = (float) Math.Sin(halfAngle);
                var oneOverSinHalfAngle = 1.0f / sinHalfAngle;
                blendA = (float) Math.Sin(halfAngle * (1.0f - blend)) * oneOverSinHalfAngle;
                blendB = (float) Math.Sin(halfAngle * blend) * oneOverSinHalfAngle;
            }
            else
            {
                // do lerp if angle is really small.
                blendA = 1.0f - blend;
                blendB = blend;
            }

            var result = new Quaternion(blendA * q1.Xyz + blendB * q2.Xyz, blendA * q1.W + blendB * q2.W);
            if (result.LengthSquared > 0.0f)
                return Normalize(result);
            return Identity;
        }

        #endregion Slerp

        #region RotateTowards

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion RotateTowards(Quaternion from, Quaternion to, float maxDegreesDelta)
        {
            var num = Angle(from, to);
            if (num == 0f)
            {
                return to;
            }

            var t = MathF.Min(1f, maxDegreesDelta / num);
            return Slerp(from, to, t);
        }

        #endregion RotateTowards

        #region Angle

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Angle(Quaternion a, Quaternion b)
        {
            var f = Dot(a, b);
            return (float) (Math.Acos(Math.Min(Math.Abs(f), 1f)) * 2f * RadToDeg);
        }

        #endregion Angle

        #region LookRotation

        // from http://answers.unity3d.com/questions/467614/what-is-the-source-code-of-quaternionlookrotation.html
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion LookRotation(ref Vector3 forward, ref Vector3 up)
        {
            forward = Vector3.Normalize(forward);
            var right = Vector3.Normalize(Vector3.Cross(up, forward));
            up = Vector3.Cross(forward, right);
            var m00 = right.X;
            var m01 = right.Y;
            var m02 = right.Z;
            var m10 = up.X;
            var m11 = up.Y;
            var m12 = up.Z;
            var m20 = forward.X;
            var m21 = forward.Y;
            var m22 = forward.Z;

            var num8 = m00 + m11 + m22;
            var quaternion = new Quaternion();
            if (num8 > 0f)
            {
                var num = MathF.Sqrt(num8 + 1f);
                quaternion.w = num * 0.5f;
                num = 0.5f / num;
                quaternion.X = (m12 - m21) * num;
                quaternion.Y = (m20 - m02) * num;
                quaternion.Z = (m01 - m10) * num;
                return quaternion;
            }

            if (m00 >= m11 && m00 >= m22)
            {
                var num7 = MathF.Sqrt(1f + m00 - m11 - m22);
                var num4 = 0.5f / num7;
                quaternion.X = 0.5f * num7;
                quaternion.Y = (m01 + m10) * num4;
                quaternion.Z = (m02 + m20) * num4;
                quaternion.W = (m12 - m21) * num4;
                return quaternion;
            }

            if (m11 > m22)
            {
                var num6 = MathF.Sqrt(1f + m11 - m00 - m22);
                var num3 = 0.5f / num6;
                quaternion.X = (m10 + m01) * num3;
                quaternion.Y = 0.5f * num6;
                quaternion.Z = (m21 + m12) * num3;
                quaternion.W = (m20 - m02) * num3;
                return quaternion;
            }

            var num5 = MathF.Sqrt(1f + m22 - m00 - m11);
            var num2 = 0.5f / num5;
            quaternion.X = (m20 + m02) * num2;
            quaternion.Y = (m21 + m12) * num2;
            quaternion.Z = 0.5f * num5;
            quaternion.W = (m01 - m10) * num2;
            return quaternion;
        }

        #endregion LookRotation

        #region Euler Angles

        // from http://stackoverflow.com/questions/12088610/conversion-between-euler-quaternion-like-in-unity3d-engine
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToEulerRad(Quaternion rotation)
        {
            var sqw = rotation.w * rotation.w;
            var sqx = rotation.x * rotation.x;
            var sqy = rotation.y * rotation.y;
            var sqz = rotation.z * rotation.z;
            var unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
            var test = rotation.x * rotation.w - rotation.y * rotation.z;
            Vector3 v;

            if (test > 0.4995f * unit)
            {
                // singularity at north pole
                v.Y = 2f * MathF.Atan2(rotation.y, rotation.x);
                v.X = (float) (Math.PI / 2);
                v.Z = 0;
                return NormalizeAngles(v * RadToDeg);
            }

            if (test < -0.4995f * unit)
            {
                // singularity at south pole
                v.Y = -2f * MathF.Atan2(rotation.y, rotation.x);
                v.X = (float) (-Math.PI / 2);
                v.Z = 0;
                return NormalizeAngles(v * RadToDeg);
            }

            var q = new Quaternion(rotation.w, rotation.z, rotation.x, rotation.y);
            v.Y = MathF.Atan2(2f * q.x * q.w + 2f * q.y * q.z, 1 - 2f * (q.z * q.z + q.w * q.w)); // Yaw
            v.X = MathF.Asin(2f * (q.x * q.z - q.w * q.y)); // Pitch
            v.Z = MathF.Atan2(2f * q.x * q.y + 2f * q.z * q.w, 1 - 2f * (q.y * q.y + q.z * q.z)); // Roll
            return NormalizeAngles(v * RadToDeg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 NormalizeAngles(Vector3 angles)
        {
            angles.X = NormalizeAngle(angles.X);
            angles.Y = NormalizeAngle(angles.Y);
            angles.Z = NormalizeAngle(angles.Z);
            return angles;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float NormalizeAngle(float angle)
        {
            /*
            while (angle > 360)
                angle -= 360;
            while (angle < 0)
                angle += 360;
            return angle;

            asm:

    L0000: vzeroupper
    L0003: vucomiss xmm0, [fld 360f]
    L000b: jbe short L001f
    L000d: vsubss xmm0, xmm0, [fld 360f]
    L0015: vucomiss xmm0, [fld 0]
    L001d: ja short L000d
    L001f: vxorps xmm1, xmm1, xmm1
    L0023: vucomiss xmm1, xmm0
    L0027: jbe short L003b
    L0029: vaddss xmm0, xmm0, [fld 360f]
    L0031: vxorps xmm1, xmm1, xmm1
    L0035: vucomiss xmm1, xmm0
    L0039: ja short L0029
    L003b: ret

            */

            return angle - MathF.Floor(angle * (1/360f)) * 360f;
            /* asm:
    L0000: vzeroupper
    L0003: vmovaps xmm1, xmm0
    L0007: vmulss xmm1, xmm1, [fld 1/360f]
    L000f: vroundss xmm1, xmm1, xmm1, 9
    L0015: vmulss xmm1, xmm1, [fld 360f]
    L001d: vsubss xmm0, xmm0, xmm1
    L0021: ret
             */
        }

        #endregion Euler Angles

        #endregion Static

        #region Operators

        /// <summary>
        /// Adds two instances.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>The result of the calculation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion operator +(Quaternion left, Quaternion right)
        {
            left.Xyz += right.Xyz;
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
        public static Quaternion operator -(Quaternion left, Quaternion right)
        {
            left.Xyz -= right.Xyz;
            left.W -= right.W;
            return left;
        }

        /// <summary>
        /// Multiplies two instances.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>The result of the calculation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion operator *(Quaternion left, Quaternion right)
        {
            Multiply(ref left, ref right, out left);
            return left;
        }

        /// <summary>
        /// Multiplies an instance by a scalar.
        /// </summary>
        /// <param name="quaternion">The instance.</param>
        /// <param name="scale">The scalar.</param>
        /// <returns>A new instance containing the result of the calculation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion operator *(Quaternion quaternion, float scale)
        {
            Multiply(ref quaternion, scale, out quaternion);
            return quaternion;
        }

        /// <summary>
        /// Multiplies an instance by a scalar.
        /// </summary>
        /// <param name="quaternion">The instance.</param>
        /// <param name="scale">The scalar.</param>
        /// <returns>A new instance containing the result of the calculation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion operator *(float scale, Quaternion quaternion)
        {
            return new(quaternion.X * scale, quaternion.Y * scale, quaternion.Z * scale, quaternion.W * scale);
        }

        /// <summary>
        /// Compares two instances for equality.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>True, if left equals right; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Quaternion left, Quaternion right)
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
        public static bool operator !=(Quaternion left, Quaternion right)
        {
            return !left.Equals(right);
        }

        #endregion Operators

        #region Overrides

        #region public override string ToString()

        /// <summary>
        /// Returns a System.String that represents the current Quaternion.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"V: {Xyz}, W: {W}";
        }

        #endregion public override string ToString()

        #region public override bool Equals (object o)

        /// <summary>
        /// Compares this object instance to another object for equality.
        /// </summary>
        /// <param name="obj">The other object to be used in the comparison.</param>
        /// <returns>True if both objects are Quaternions of equal value. Otherwise it returns false.</returns>
        public override bool Equals(object? obj)
        {
            if (obj is Quaternion quaternion) return this == quaternion;
            return false;
        }

        #endregion public override bool Equals (object o)

        #region public override int GetHashCode ()

        /// <summary>
        /// Provides the hash code for this object.
        /// </summary>
        /// <returns>A hash code formed from the bitwise XOR of this objects members.</returns>
        public override int GetHashCode()
        {
            return Xyz.GetHashCode() ^ W.GetHashCode();
        }

        #endregion public override int GetHashCode ()

        #endregion Overrides

        #endregion Public Members

        #region IEquatable<Quaternion> Members

        /// <summary>
        /// Compares this Quaternion instance to another Quaternion for equality.
        /// </summary>
        /// <param name="other">The other Quaternion to be used in the comparison.</param>
        /// <returns>True if both instances are equal; false otherwise.</returns>
        public bool Equals(Quaternion other)
        {
            return Xyz == other.Xyz && W == other.W;
        }

        #endregion IEquatable<Quaternion> Members
    }
}
