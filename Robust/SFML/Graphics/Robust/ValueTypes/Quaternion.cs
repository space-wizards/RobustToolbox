using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using SFML.Graphics.Design;

namespace SFML.Graphics
{
    /// <summary>Defines a four-dimensional vector (x,y,z,w), which is used to efficiently rotate an object about the (x, y, z) vector by the angle theta, where w = cos(theta/2).</summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [TypeConverter(typeof(QuaternionConverter))]
    public struct Quaternion : IEquatable<Quaternion>
    {
        /// <summary>Specifies the x-value of the vector component of the quaternion.</summary>
        public float X;

        /// <summary>Specifies the y-value of the vector component of the quaternion.</summary>
        public float Y;

        /// <summary>Specifies the z-value of the vector component of the quaternion.</summary>
        public float Z;

        /// <summary>Specifies the rotation component of the quaternion.</summary>
        public float W;

        static readonly Quaternion _identity;

        /// <summary>Returns a Quaternion representing no rotation.</summary>
        public static Quaternion Identity
        {
            get { return _identity; }
        }

        /// <summary>Initializes a new instance of Quaternion.</summary>
        /// <param name="x">The x-value of the quaternion.</param>
        /// <param name="y">The y-value of the quaternion.</param>
        /// <param name="z">The z-value of the quaternion.</param>
        /// <param name="w">The w-value of the quaternion.</param>
        public Quaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        /// <summary>Initializes a new instance of Quaternion.</summary>
        /// <param name="vectorPart">The vector component of the quaternion.</param>
        /// <param name="scalarPart">The rotation component of the quaternion.</param>
        public Quaternion(Vector3 vectorPart, float scalarPart)
        {
            X = vectorPart.X;
            Y = vectorPart.Y;
            Z = vectorPart.Z;
            W = scalarPart;
        }

        /// <summary>Retireves a string representation of the current object.</summary>
        public override string ToString()
        {
            var currentCulture = CultureInfo.CurrentCulture;
            return string.Format(currentCulture, "{{X:{0} Y:{1} Z:{2} W:{3}}}",
                new object[]
                { X.ToString(currentCulture), Y.ToString(currentCulture), Z.ToString(currentCulture), W.ToString(currentCulture) });
        }

        /// <summary>Determines whether the specified Object is equal to the Quaternion.</summary>
        /// <param name="other">The Quaternion to compare with the current Quaternion.</param>
        public bool Equals(Quaternion other)
        {
            return ((((X == other.X) && (Y == other.Y)) && (Z == other.Z)) && (W == other.W));
        }

        /// <summary>Returns a value that indicates whether the current instance is equal to a specified object.</summary>
        /// <param name="obj">Object to make the comparison with.</param>
        public override bool Equals(object obj)
        {
            var flag = false;
            if (obj is Quaternion)
                flag = Equals((Quaternion)obj);
            return flag;
        }

        /// <summary>Get the hash code of this object.</summary>
        public override int GetHashCode()
        {
            return (((X.GetHashCode() + Y.GetHashCode()) + Z.GetHashCode()) + W.GetHashCode());
        }

        /// <summary>Calculates the length squared of a Quaternion.</summary>
        public float LengthSquared()
        {
            return ((((X * X) + (Y * Y)) + (Z * Z)) + (W * W));
        }

        /// <summary>Calculates the length of a Quaternion.</summary>
        public float Length()
        {
            var num = (((X * X) + (Y * Y)) + (Z * Z)) + (W * W);
            return (float)Math.Sqrt(num);
        }

        /// <summary>Divides each component of the quaternion by the length of the quaternion.</summary>
        public void Normalize()
        {
            var num2 = (((X * X) + (Y * Y)) + (Z * Z)) + (W * W);
            var num = 1f / ((float)Math.Sqrt(num2));
            X *= num;
            Y *= num;
            Z *= num;
            W *= num;
        }

        /// <summary>Divides each component of the quaternion by the length of the quaternion.</summary>
        /// <param name="quaternion">Source quaternion.</param>
        public static Quaternion Normalize(Quaternion quaternion)
        {
            Quaternion quaternion2;
            var num2 = (((quaternion.X * quaternion.X) + (quaternion.Y * quaternion.Y)) + (quaternion.Z * quaternion.Z)) +
                       (quaternion.W * quaternion.W);
            var num = 1f / ((float)Math.Sqrt(num2));
            quaternion2.X = quaternion.X * num;
            quaternion2.Y = quaternion.Y * num;
            quaternion2.Z = quaternion.Z * num;
            quaternion2.W = quaternion.W * num;
            return quaternion2;
        }

        /// <summary>Divides each component of the quaternion by the length of the quaternion.</summary>
        /// <param name="quaternion">Source quaternion.</param>
        /// <param name="result">[OutAttribute] Normalized quaternion.</param>
        public static void Normalize(ref Quaternion quaternion, out Quaternion result)
        {
            var num2 = (((quaternion.X * quaternion.X) + (quaternion.Y * quaternion.Y)) + (quaternion.Z * quaternion.Z)) +
                       (quaternion.W * quaternion.W);
            var num = 1f / ((float)Math.Sqrt(num2));
            result.X = quaternion.X * num;
            result.Y = quaternion.Y * num;
            result.Z = quaternion.Z * num;
            result.W = quaternion.W * num;
        }

        /// <summary>Transforms this Quaternion into its conjugate.</summary>
        public void Conjugate()
        {
            X = -X;
            Y = -Y;
            Z = -Z;
        }

        /// <summary>Returns the conjugate of a specified Quaternion.</summary>
        /// <param name="value">The Quaternion of which to return the conjugate.</param>
        public static Quaternion Conjugate(Quaternion value)
        {
            Quaternion quaternion;
            quaternion.X = -value.X;
            quaternion.Y = -value.Y;
            quaternion.Z = -value.Z;
            quaternion.W = value.W;
            return quaternion;
        }

        /// <summary>Returns the conjugate of a specified Quaternion.</summary>
        /// <param name="value">The Quaternion of which to return the conjugate.</param>
        /// <param name="result">[OutAttribute] An existing Quaternion filled in to be the conjugate of the specified one.</param>
        public static void Conjugate(ref Quaternion value, out Quaternion result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
            result.Z = -value.Z;
            result.W = value.W;
        }

        /// <summary>Returns the inverse of a Quaternion.</summary>
        /// <param name="quaternion">Source Quaternion.</param>
        public static Quaternion Inverse(Quaternion quaternion)
        {
            Quaternion quaternion2;
            var num2 = (((quaternion.X * quaternion.X) + (quaternion.Y * quaternion.Y)) + (quaternion.Z * quaternion.Z)) +
                       (quaternion.W * quaternion.W);
            var num = 1f / num2;
            quaternion2.X = -quaternion.X * num;
            quaternion2.Y = -quaternion.Y * num;
            quaternion2.Z = -quaternion.Z * num;
            quaternion2.W = quaternion.W * num;
            return quaternion2;
        }

        /// <summary>Returns the inverse of a Quaternion.</summary>
        /// <param name="quaternion">Source Quaternion.</param>
        /// <param name="result">[OutAttribute] The inverse of the Quaternion.</param>
        public static void Inverse(ref Quaternion quaternion, out Quaternion result)
        {
            var num2 = (((quaternion.X * quaternion.X) + (quaternion.Y * quaternion.Y)) + (quaternion.Z * quaternion.Z)) +
                       (quaternion.W * quaternion.W);
            var num = 1f / num2;
            result.X = -quaternion.X * num;
            result.Y = -quaternion.Y * num;
            result.Z = -quaternion.Z * num;
            result.W = quaternion.W * num;
        }

        /// <summary>Creates a Quaternion from a vector and an angle to rotate about the vector.</summary>
        /// <param name="axis">The vector to rotate around.</param>
        /// <param name="angle">The angle to rotate around the vector.</param>
        public static Quaternion CreateFromAxisAngle(Vector3 axis, float angle)
        {
            Quaternion quaternion;
            var num2 = angle * 0.5f;
            var num = (float)Math.Sin(num2);
            var num3 = (float)Math.Cos(num2);
            quaternion.X = axis.X * num;
            quaternion.Y = axis.Y * num;
            quaternion.Z = axis.Z * num;
            quaternion.W = num3;
            return quaternion;
        }

        /// <summary>Creates a Quaternion from a vector and an angle to rotate about the vector.</summary>
        /// <param name="axis">The vector to rotate around.</param>
        /// <param name="angle">The angle to rotate around the vector.</param>
        /// <param name="result">[OutAttribute] The created Quaternion.</param>
        public static void CreateFromAxisAngle(ref Vector3 axis, float angle, out Quaternion result)
        {
            var num2 = angle * 0.5f;
            var num = (float)Math.Sin(num2);
            var num3 = (float)Math.Cos(num2);
            result.X = axis.X * num;
            result.Y = axis.Y * num;
            result.Z = axis.Z * num;
            result.W = num3;
        }

        /// <summary>Creates a new Quaternion from specified yaw, pitch, and roll angles.</summary>
        /// <param name="yaw">The yaw angle, in radians, around the y-axis.</param>
        /// <param name="pitch">The pitch angle, in radians, around the x-axis.</param>
        /// <param name="roll">The roll angle, in radians, around the z-axis.</param>
        public static Quaternion CreateFromYawPitchRoll(float yaw, float pitch, float roll)
        {
            Quaternion quaternion;
            var num9 = roll * 0.5f;
            var num6 = (float)Math.Sin(num9);
            var num5 = (float)Math.Cos(num9);
            var num8 = pitch * 0.5f;
            var num4 = (float)Math.Sin(num8);
            var num3 = (float)Math.Cos(num8);
            var num7 = yaw * 0.5f;
            var num2 = (float)Math.Sin(num7);
            var num = (float)Math.Cos(num7);
            quaternion.X = ((num * num4) * num5) + ((num2 * num3) * num6);
            quaternion.Y = ((num2 * num3) * num5) - ((num * num4) * num6);
            quaternion.Z = ((num * num3) * num6) - ((num2 * num4) * num5);
            quaternion.W = ((num * num3) * num5) + ((num2 * num4) * num6);
            return quaternion;
        }

        /// <summary>Creates a new Quaternion from specified yaw, pitch, and roll angles.</summary>
        /// <param name="yaw">The yaw angle, in radians, around the y-axis.</param>
        /// <param name="pitch">The pitch angle, in radians, around the x-axis.</param>
        /// <param name="roll">The roll angle, in radians, around the z-axis.</param>
        /// <param name="result">[OutAttribute] An existing Quaternion filled in to express the specified yaw, pitch, and roll angles.</param>
        public static void CreateFromYawPitchRoll(float yaw, float pitch, float roll, out Quaternion result)
        {
            var num9 = roll * 0.5f;
            var num6 = (float)Math.Sin(num9);
            var num5 = (float)Math.Cos(num9);
            var num8 = pitch * 0.5f;
            var num4 = (float)Math.Sin(num8);
            var num3 = (float)Math.Cos(num8);
            var num7 = yaw * 0.5f;
            var num2 = (float)Math.Sin(num7);
            var num = (float)Math.Cos(num7);
            result.X = ((num * num4) * num5) + ((num2 * num3) * num6);
            result.Y = ((num2 * num3) * num5) - ((num * num4) * num6);
            result.Z = ((num * num3) * num6) - ((num2 * num4) * num5);
            result.W = ((num * num3) * num5) + ((num2 * num4) * num6);
        }

        /// <summary>Creates a Quaternion from a rotation Matrix.</summary>
        /// <param name="matrix">The rotation Matrix to create the Quaternion from.</param>
        public static Quaternion CreateFromRotationMatrix(Matrix matrix)
        {
            var num8 = (matrix.M11 + matrix.M22) + matrix.M33;
            var quaternion = new Quaternion();
            if (num8 > 0f)
            {
                var num = (float)Math.Sqrt((num8 + 1f));
                quaternion.W = num * 0.5f;
                num = 0.5f / num;
                quaternion.X = (matrix.M23 - matrix.M32) * num;
                quaternion.Y = (matrix.M31 - matrix.M13) * num;
                quaternion.Z = (matrix.M12 - matrix.M21) * num;
                return quaternion;
            }
            if ((matrix.M11 >= matrix.M22) && (matrix.M11 >= matrix.M33))
            {
                var num7 = (float)Math.Sqrt((((1f + matrix.M11) - matrix.M22) - matrix.M33));
                var num4 = 0.5f / num7;
                quaternion.X = 0.5f * num7;
                quaternion.Y = (matrix.M12 + matrix.M21) * num4;
                quaternion.Z = (matrix.M13 + matrix.M31) * num4;
                quaternion.W = (matrix.M23 - matrix.M32) * num4;
                return quaternion;
            }
            if (matrix.M22 > matrix.M33)
            {
                var num6 = (float)Math.Sqrt((((1f + matrix.M22) - matrix.M11) - matrix.M33));
                var num3 = 0.5f / num6;
                quaternion.X = (matrix.M21 + matrix.M12) * num3;
                quaternion.Y = 0.5f * num6;
                quaternion.Z = (matrix.M32 + matrix.M23) * num3;
                quaternion.W = (matrix.M31 - matrix.M13) * num3;
                return quaternion;
            }
            var num5 = (float)Math.Sqrt((((1f + matrix.M33) - matrix.M11) - matrix.M22));
            var num2 = 0.5f / num5;
            quaternion.X = (matrix.M31 + matrix.M13) * num2;
            quaternion.Y = (matrix.M32 + matrix.M23) * num2;
            quaternion.Z = 0.5f * num5;
            quaternion.W = (matrix.M12 - matrix.M21) * num2;
            return quaternion;
        }

        /// <summary>Creates a Quaternion from a rotation Matrix.</summary>
        /// <param name="matrix">The rotation Matrix to create the Quaternion from.</param>
        /// <param name="result">[OutAttribute] The created Quaternion.</param>
        public static void CreateFromRotationMatrix(ref Matrix matrix, out Quaternion result)
        {
            var num8 = (matrix.M11 + matrix.M22) + matrix.M33;
            if (num8 > 0f)
            {
                var num = (float)Math.Sqrt((num8 + 1f));
                result.W = num * 0.5f;
                num = 0.5f / num;
                result.X = (matrix.M23 - matrix.M32) * num;
                result.Y = (matrix.M31 - matrix.M13) * num;
                result.Z = (matrix.M12 - matrix.M21) * num;
            }
            else if ((matrix.M11 >= matrix.M22) && (matrix.M11 >= matrix.M33))
            {
                var num7 = (float)Math.Sqrt((((1f + matrix.M11) - matrix.M22) - matrix.M33));
                var num4 = 0.5f / num7;
                result.X = 0.5f * num7;
                result.Y = (matrix.M12 + matrix.M21) * num4;
                result.Z = (matrix.M13 + matrix.M31) * num4;
                result.W = (matrix.M23 - matrix.M32) * num4;
            }
            else if (matrix.M22 > matrix.M33)
            {
                var num6 = (float)Math.Sqrt((((1f + matrix.M22) - matrix.M11) - matrix.M33));
                var num3 = 0.5f / num6;
                result.X = (matrix.M21 + matrix.M12) * num3;
                result.Y = 0.5f * num6;
                result.Z = (matrix.M32 + matrix.M23) * num3;
                result.W = (matrix.M31 - matrix.M13) * num3;
            }
            else
            {
                var num5 = (float)Math.Sqrt((((1f + matrix.M33) - matrix.M11) - matrix.M22));
                var num2 = 0.5f / num5;
                result.X = (matrix.M31 + matrix.M13) * num2;
                result.Y = (matrix.M32 + matrix.M23) * num2;
                result.Z = 0.5f * num5;
                result.W = (matrix.M12 - matrix.M21) * num2;
            }
        }

        /// <summary>Calculates the dot product of two Quaternions.</summary>
        /// <param name="quaternion1">Source Quaternion.</param>
        /// <param name="quaternion2">Source Quaternion.</param>
        public static float Dot(Quaternion quaternion1, Quaternion quaternion2)
        {
            return ((((quaternion1.X * quaternion2.X) + (quaternion1.Y * quaternion2.Y)) + (quaternion1.Z * quaternion2.Z)) +
                    (quaternion1.W * quaternion2.W));
        }

        /// <summary>Calculates the dot product of two Quaternions.</summary>
        /// <param name="quaternion1">Source Quaternion.</param>
        /// <param name="quaternion2">Source Quaternion.</param>
        /// <param name="result">[OutAttribute] Dot product of the Quaternions.</param>
        public static void Dot(ref Quaternion quaternion1, ref Quaternion quaternion2, out float result)
        {
            result = (((quaternion1.X * quaternion2.X) + (quaternion1.Y * quaternion2.Y)) + (quaternion1.Z * quaternion2.Z)) +
                     (quaternion1.W * quaternion2.W);
        }

        /// <summary>Interpolates between two quaternions, using spherical linear interpolation.</summary>
        /// <param name="quaternion1">Source quaternion.</param>
        /// <param name="quaternion2">Source quaternion.</param>
        /// <param name="amount">Value that indicates how far to interpolate between the quaternions.</param>
        public static Quaternion Slerp(Quaternion quaternion1, Quaternion quaternion2, float amount)
        {
            float num2;
            float num3;
            Quaternion quaternion;
            var num = amount;
            var num4 = (((quaternion1.X * quaternion2.X) + (quaternion1.Y * quaternion2.Y)) + (quaternion1.Z * quaternion2.Z)) +
                       (quaternion1.W * quaternion2.W);
            var flag = false;
            if (num4 < 0f)
            {
                flag = true;
                num4 = -num4;
            }
            if (num4 > 0.999999f)
            {
                num3 = 1f - num;
                num2 = flag ? -num : num;
            }
            else
            {
                var num5 = (float)Math.Acos(num4);
                var num6 = (float)(1.0 / Math.Sin(num5));
                num3 = ((float)Math.Sin(((1f - num) * num5))) * num6;
                num2 = flag ? (((float)-Math.Sin((num * num5))) * num6) : (((float)Math.Sin((num * num5))) * num6);
            }
            quaternion.X = (num3 * quaternion1.X) + (num2 * quaternion2.X);
            quaternion.Y = (num3 * quaternion1.Y) + (num2 * quaternion2.Y);
            quaternion.Z = (num3 * quaternion1.Z) + (num2 * quaternion2.Z);
            quaternion.W = (num3 * quaternion1.W) + (num2 * quaternion2.W);
            return quaternion;
        }

        /// <summary>Interpolates between two quaternions, using spherical linear interpolation.</summary>
        /// <param name="quaternion1">Source quaternion.</param>
        /// <param name="quaternion2">Source quaternion.</param>
        /// <param name="amount">Value that indicates how far to interpolate between the quaternions.</param>
        /// <param name="result">[OutAttribute] Result of the interpolation.</param>
        public static void Slerp(ref Quaternion quaternion1, ref Quaternion quaternion2, float amount, out Quaternion result)
        {
            float num2;
            float num3;
            var num = amount;
            var num4 = (((quaternion1.X * quaternion2.X) + (quaternion1.Y * quaternion2.Y)) + (quaternion1.Z * quaternion2.Z)) +
                       (quaternion1.W * quaternion2.W);
            var flag = false;
            if (num4 < 0f)
            {
                flag = true;
                num4 = -num4;
            }
            if (num4 > 0.999999f)
            {
                num3 = 1f - num;
                num2 = flag ? -num : num;
            }
            else
            {
                var num5 = (float)Math.Acos(num4);
                var num6 = (float)(1.0 / Math.Sin(num5));
                num3 = ((float)Math.Sin(((1f - num) * num5))) * num6;
                num2 = flag ? (((float)-Math.Sin((num * num5))) * num6) : (((float)Math.Sin((num * num5))) * num6);
            }
            result.X = (num3 * quaternion1.X) + (num2 * quaternion2.X);
            result.Y = (num3 * quaternion1.Y) + (num2 * quaternion2.Y);
            result.Z = (num3 * quaternion1.Z) + (num2 * quaternion2.Z);
            result.W = (num3 * quaternion1.W) + (num2 * quaternion2.W);
        }

        /// <summary>Linearly interpolates between two quaternions.</summary>
        /// <param name="quaternion1">Source quaternion.</param>
        /// <param name="quaternion2">Source quaternion.</param>
        /// <param name="amount">Value indicating how far to interpolate between the quaternions.</param>
        public static Quaternion Lerp(Quaternion quaternion1, Quaternion quaternion2, float amount)
        {
            var num = amount;
            var num2 = 1f - num;
            var quaternion = new Quaternion();
            var num5 = (((quaternion1.X * quaternion2.X) + (quaternion1.Y * quaternion2.Y)) + (quaternion1.Z * quaternion2.Z)) +
                       (quaternion1.W * quaternion2.W);
            if (num5 >= 0f)
            {
                quaternion.X = (num2 * quaternion1.X) + (num * quaternion2.X);
                quaternion.Y = (num2 * quaternion1.Y) + (num * quaternion2.Y);
                quaternion.Z = (num2 * quaternion1.Z) + (num * quaternion2.Z);
                quaternion.W = (num2 * quaternion1.W) + (num * quaternion2.W);
            }
            else
            {
                quaternion.X = (num2 * quaternion1.X) - (num * quaternion2.X);
                quaternion.Y = (num2 * quaternion1.Y) - (num * quaternion2.Y);
                quaternion.Z = (num2 * quaternion1.Z) - (num * quaternion2.Z);
                quaternion.W = (num2 * quaternion1.W) - (num * quaternion2.W);
            }
            var num4 = (((quaternion.X * quaternion.X) + (quaternion.Y * quaternion.Y)) + (quaternion.Z * quaternion.Z)) +
                       (quaternion.W * quaternion.W);
            var num3 = 1f / ((float)Math.Sqrt(num4));
            quaternion.X *= num3;
            quaternion.Y *= num3;
            quaternion.Z *= num3;
            quaternion.W *= num3;
            return quaternion;
        }

        /// <summary>Linearly interpolates between two quaternions.</summary>
        /// <param name="quaternion1">Source quaternion.</param>
        /// <param name="quaternion2">Source quaternion.</param>
        /// <param name="amount">Value indicating how far to interpolate between the quaternions.</param>
        /// <param name="result">[OutAttribute] The resulting quaternion.</param>
        public static void Lerp(ref Quaternion quaternion1, ref Quaternion quaternion2, float amount, out Quaternion result)
        {
            var num = amount;
            var num2 = 1f - num;
            var num5 = (((quaternion1.X * quaternion2.X) + (quaternion1.Y * quaternion2.Y)) + (quaternion1.Z * quaternion2.Z)) +
                       (quaternion1.W * quaternion2.W);
            if (num5 >= 0f)
            {
                result.X = (num2 * quaternion1.X) + (num * quaternion2.X);
                result.Y = (num2 * quaternion1.Y) + (num * quaternion2.Y);
                result.Z = (num2 * quaternion1.Z) + (num * quaternion2.Z);
                result.W = (num2 * quaternion1.W) + (num * quaternion2.W);
            }
            else
            {
                result.X = (num2 * quaternion1.X) - (num * quaternion2.X);
                result.Y = (num2 * quaternion1.Y) - (num * quaternion2.Y);
                result.Z = (num2 * quaternion1.Z) - (num * quaternion2.Z);
                result.W = (num2 * quaternion1.W) - (num * quaternion2.W);
            }
            var num4 = (((result.X * result.X) + (result.Y * result.Y)) + (result.Z * result.Z)) + (result.W * result.W);
            var num3 = 1f / ((float)Math.Sqrt(num4));
            result.X *= num3;
            result.Y *= num3;
            result.Z *= num3;
            result.W *= num3;
        }

        /// <summary>Concatenates two Quaternions; the result represents the value1 rotation followed by the value2 rotation.</summary>
        /// <param name="value1">The first Quaternion rotation in the series.</param>
        /// <param name="value2">The second Quaternion rotation in the series.</param>
        public static Quaternion Concatenate(Quaternion value1, Quaternion value2)
        {
            Quaternion quaternion;
            var x = value2.X;
            var y = value2.Y;
            var z = value2.Z;
            var w = value2.W;
            var num4 = value1.X;
            var num3 = value1.Y;
            var num2 = value1.Z;
            var num = value1.W;
            var num12 = (y * num2) - (z * num3);
            var num11 = (z * num4) - (x * num2);
            var num10 = (x * num3) - (y * num4);
            var num9 = ((x * num4) + (y * num3)) + (z * num2);
            quaternion.X = ((x * num) + (num4 * w)) + num12;
            quaternion.Y = ((y * num) + (num3 * w)) + num11;
            quaternion.Z = ((z * num) + (num2 * w)) + num10;
            quaternion.W = (w * num) - num9;
            return quaternion;
        }

        /// <summary>Concatenates two Quaternions; the result represents the value1 rotation followed by the value2 rotation.</summary>
        /// <param name="value1">The first Quaternion rotation in the series.</param>
        /// <param name="value2">The second Quaternion rotation in the series.</param>
        /// <param name="result">[OutAttribute] The Quaternion rotation representing the concatenation of value1 followed by value2.</param>
        public static void Concatenate(ref Quaternion value1, ref Quaternion value2, out Quaternion result)
        {
            var x = value2.X;
            var y = value2.Y;
            var z = value2.Z;
            var w = value2.W;
            var num4 = value1.X;
            var num3 = value1.Y;
            var num2 = value1.Z;
            var num = value1.W;
            var num12 = (y * num2) - (z * num3);
            var num11 = (z * num4) - (x * num2);
            var num10 = (x * num3) - (y * num4);
            var num9 = ((x * num4) + (y * num3)) + (z * num2);
            result.X = ((x * num) + (num4 * w)) + num12;
            result.Y = ((y * num) + (num3 * w)) + num11;
            result.Z = ((z * num) + (num2 * w)) + num10;
            result.W = (w * num) - num9;
        }

        /// <summary>Flips the sign of each component of the quaternion.</summary>
        /// <param name="quaternion">Source quaternion.</param>
        public static Quaternion Negate(Quaternion quaternion)
        {
            Quaternion quaternion2;
            quaternion2.X = -quaternion.X;
            quaternion2.Y = -quaternion.Y;
            quaternion2.Z = -quaternion.Z;
            quaternion2.W = -quaternion.W;
            return quaternion2;
        }

        /// <summary>Flips the sign of each component of the quaternion.</summary>
        /// <param name="quaternion">Source quaternion.</param>
        /// <param name="result">[OutAttribute] Negated quaternion.</param>
        public static void Negate(ref Quaternion quaternion, out Quaternion result)
        {
            result.X = -quaternion.X;
            result.Y = -quaternion.Y;
            result.Z = -quaternion.Z;
            result.W = -quaternion.W;
        }

        /// <summary>Adds two Quaternions.</summary>
        /// <param name="quaternion1">Quaternion to add.</param>
        /// <param name="quaternion2">Quaternion to add.</param>
        public static Quaternion Add(Quaternion quaternion1, Quaternion quaternion2)
        {
            Quaternion quaternion;
            quaternion.X = quaternion1.X + quaternion2.X;
            quaternion.Y = quaternion1.Y + quaternion2.Y;
            quaternion.Z = quaternion1.Z + quaternion2.Z;
            quaternion.W = quaternion1.W + quaternion2.W;
            return quaternion;
        }

        /// <summary>Adds two Quaternions.</summary>
        /// <param name="quaternion1">Quaternion to add.</param>
        /// <param name="quaternion2">Quaternion to add.</param>
        /// <param name="result">[OutAttribute] Result of adding the Quaternions.</param>
        public static void Add(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
        {
            result.X = quaternion1.X + quaternion2.X;
            result.Y = quaternion1.Y + quaternion2.Y;
            result.Z = quaternion1.Z + quaternion2.Z;
            result.W = quaternion1.W + quaternion2.W;
        }

        /// <summary>Subtracts a quaternion from another quaternion.</summary>
        /// <param name="quaternion1">Source quaternion.</param>
        /// <param name="quaternion2">Source quaternion.</param>
        public static Quaternion Subtract(Quaternion quaternion1, Quaternion quaternion2)
        {
            Quaternion quaternion;
            quaternion.X = quaternion1.X - quaternion2.X;
            quaternion.Y = quaternion1.Y - quaternion2.Y;
            quaternion.Z = quaternion1.Z - quaternion2.Z;
            quaternion.W = quaternion1.W - quaternion2.W;
            return quaternion;
        }

        /// <summary>Subtracts a quaternion from another quaternion.</summary>
        /// <param name="quaternion1">Source quaternion.</param>
        /// <param name="quaternion2">Source quaternion.</param>
        /// <param name="result">[OutAttribute] Result of the subtraction.</param>
        public static void Subtract(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
        {
            result.X = quaternion1.X - quaternion2.X;
            result.Y = quaternion1.Y - quaternion2.Y;
            result.Z = quaternion1.Z - quaternion2.Z;
            result.W = quaternion1.W - quaternion2.W;
        }

        /// <summary>Multiplies two quaternions.</summary>
        /// <param name="quaternion1">The quaternion on the left of the multiplication.</param>
        /// <param name="quaternion2">The quaternion on the right of the multiplication.</param>
        public static Quaternion Multiply(Quaternion quaternion1, Quaternion quaternion2)
        {
            Quaternion quaternion;
            var x = quaternion1.X;
            var y = quaternion1.Y;
            var z = quaternion1.Z;
            var w = quaternion1.W;
            var num4 = quaternion2.X;
            var num3 = quaternion2.Y;
            var num2 = quaternion2.Z;
            var num = quaternion2.W;
            var num12 = (y * num2) - (z * num3);
            var num11 = (z * num4) - (x * num2);
            var num10 = (x * num3) - (y * num4);
            var num9 = ((x * num4) + (y * num3)) + (z * num2);
            quaternion.X = ((x * num) + (num4 * w)) + num12;
            quaternion.Y = ((y * num) + (num3 * w)) + num11;
            quaternion.Z = ((z * num) + (num2 * w)) + num10;
            quaternion.W = (w * num) - num9;
            return quaternion;
        }

        /// <summary>Multiplies two quaternions.</summary>
        /// <param name="quaternion1">The quaternion on the left of the multiplication.</param>
        /// <param name="quaternion2">The quaternion on the right of the multiplication.</param>
        /// <param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
        {
            var x = quaternion1.X;
            var y = quaternion1.Y;
            var z = quaternion1.Z;
            var w = quaternion1.W;
            var num4 = quaternion2.X;
            var num3 = quaternion2.Y;
            var num2 = quaternion2.Z;
            var num = quaternion2.W;
            var num12 = (y * num2) - (z * num3);
            var num11 = (z * num4) - (x * num2);
            var num10 = (x * num3) - (y * num4);
            var num9 = ((x * num4) + (y * num3)) + (z * num2);
            result.X = ((x * num) + (num4 * w)) + num12;
            result.Y = ((y * num) + (num3 * w)) + num11;
            result.Z = ((z * num) + (num2 * w)) + num10;
            result.W = (w * num) - num9;
        }

        /// <summary>Multiplies a quaternion by a scalar value.</summary>
        /// <param name="quaternion1">Source quaternion.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        public static Quaternion Multiply(Quaternion quaternion1, float scaleFactor)
        {
            Quaternion quaternion;
            quaternion.X = quaternion1.X * scaleFactor;
            quaternion.Y = quaternion1.Y * scaleFactor;
            quaternion.Z = quaternion1.Z * scaleFactor;
            quaternion.W = quaternion1.W * scaleFactor;
            return quaternion;
        }

        /// <summary>Multiplies a quaternion by a scalar value.</summary>
        /// <param name="quaternion1">Source quaternion.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        /// <param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Quaternion quaternion1, float scaleFactor, out Quaternion result)
        {
            result.X = quaternion1.X * scaleFactor;
            result.Y = quaternion1.Y * scaleFactor;
            result.Z = quaternion1.Z * scaleFactor;
            result.W = quaternion1.W * scaleFactor;
        }

        /// <summary>Divides a Quaternion by another Quaternion.</summary>
        /// <param name="quaternion1">Source Quaternion.</param>
        /// <param name="quaternion2">The divisor.</param>
        public static Quaternion Divide(Quaternion quaternion1, Quaternion quaternion2)
        {
            Quaternion quaternion;
            var x = quaternion1.X;
            var y = quaternion1.Y;
            var z = quaternion1.Z;
            var w = quaternion1.W;
            var num14 = (((quaternion2.X * quaternion2.X) + (quaternion2.Y * quaternion2.Y)) + (quaternion2.Z * quaternion2.Z)) +
                        (quaternion2.W * quaternion2.W);
            var num5 = 1f / num14;
            var num4 = -quaternion2.X * num5;
            var num3 = -quaternion2.Y * num5;
            var num2 = -quaternion2.Z * num5;
            var num = quaternion2.W * num5;
            var num13 = (y * num2) - (z * num3);
            var num12 = (z * num4) - (x * num2);
            var num11 = (x * num3) - (y * num4);
            var num10 = ((x * num4) + (y * num3)) + (z * num2);
            quaternion.X = ((x * num) + (num4 * w)) + num13;
            quaternion.Y = ((y * num) + (num3 * w)) + num12;
            quaternion.Z = ((z * num) + (num2 * w)) + num11;
            quaternion.W = (w * num) - num10;
            return quaternion;
        }

        /// <summary>Divides a Quaternion by another Quaternion.</summary>
        /// <param name="quaternion1">Source Quaternion.</param>
        /// <param name="quaternion2">The divisor.</param>
        /// <param name="result">[OutAttribute] Result of the division.</param>
        public static void Divide(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
        {
            var x = quaternion1.X;
            var y = quaternion1.Y;
            var z = quaternion1.Z;
            var w = quaternion1.W;
            var num14 = (((quaternion2.X * quaternion2.X) + (quaternion2.Y * quaternion2.Y)) + (quaternion2.Z * quaternion2.Z)) +
                        (quaternion2.W * quaternion2.W);
            var num5 = 1f / num14;
            var num4 = -quaternion2.X * num5;
            var num3 = -quaternion2.Y * num5;
            var num2 = -quaternion2.Z * num5;
            var num = quaternion2.W * num5;
            var num13 = (y * num2) - (z * num3);
            var num12 = (z * num4) - (x * num2);
            var num11 = (x * num3) - (y * num4);
            var num10 = ((x * num4) + (y * num3)) + (z * num2);
            result.X = ((x * num) + (num4 * w)) + num13;
            result.Y = ((y * num) + (num3 * w)) + num12;
            result.Z = ((z * num) + (num2 * w)) + num11;
            result.W = (w * num) - num10;
        }

        /// <summary>Flips the sign of each component of the quaternion.</summary>
        /// <param name="quaternion">Source quaternion.</param>
        public static Quaternion operator -(Quaternion quaternion)
        {
            Quaternion quaternion2;
            quaternion2.X = -quaternion.X;
            quaternion2.Y = -quaternion.Y;
            quaternion2.Z = -quaternion.Z;
            quaternion2.W = -quaternion.W;
            return quaternion2;
        }

        /// <summary>Compares two Quaternions for equality.</summary>
        /// <param name="quaternion1">Source Quaternion.</param>
        /// <param name="quaternion2">Source Quaternion.</param>
        public static bool operator ==(Quaternion quaternion1, Quaternion quaternion2)
        {
            return ((((quaternion1.X == quaternion2.X) && (quaternion1.Y == quaternion2.Y)) && (quaternion1.Z == quaternion2.Z)) &&
                    (quaternion1.W == quaternion2.W));
        }

        /// <summary>Compare two Quaternions for inequality.</summary>
        /// <param name="quaternion1">Source Quaternion.</param>
        /// <param name="quaternion2">Source Quaternion.</param>
        public static bool operator !=(Quaternion quaternion1, Quaternion quaternion2)
        {
            if (((quaternion1.X == quaternion2.X) && (quaternion1.Y == quaternion2.Y)) && (quaternion1.Z == quaternion2.Z))
                return (quaternion1.W != quaternion2.W);
            return true;
        }

        /// <summary>Adds two Quaternions.</summary>
        /// <param name="quaternion1">Quaternion to add.</param>
        /// <param name="quaternion2">Quaternion to add.</param>
        public static Quaternion operator +(Quaternion quaternion1, Quaternion quaternion2)
        {
            Quaternion quaternion;
            quaternion.X = quaternion1.X + quaternion2.X;
            quaternion.Y = quaternion1.Y + quaternion2.Y;
            quaternion.Z = quaternion1.Z + quaternion2.Z;
            quaternion.W = quaternion1.W + quaternion2.W;
            return quaternion;
        }

        /// <summary>Subtracts a quaternion from another quaternion.</summary>
        /// <param name="quaternion1">Source quaternion.</param>
        /// <param name="quaternion2">Source quaternion.</param>
        public static Quaternion operator -(Quaternion quaternion1, Quaternion quaternion2)
        {
            Quaternion quaternion;
            quaternion.X = quaternion1.X - quaternion2.X;
            quaternion.Y = quaternion1.Y - quaternion2.Y;
            quaternion.Z = quaternion1.Z - quaternion2.Z;
            quaternion.W = quaternion1.W - quaternion2.W;
            return quaternion;
        }

        /// <summary>Multiplies two quaternions.</summary>
        /// <param name="quaternion1">Source quaternion.</param>
        /// <param name="quaternion2">Source quaternion.</param>
        public static Quaternion operator *(Quaternion quaternion1, Quaternion quaternion2)
        {
            Quaternion quaternion;
            var x = quaternion1.X;
            var y = quaternion1.Y;
            var z = quaternion1.Z;
            var w = quaternion1.W;
            var num4 = quaternion2.X;
            var num3 = quaternion2.Y;
            var num2 = quaternion2.Z;
            var num = quaternion2.W;
            var num12 = (y * num2) - (z * num3);
            var num11 = (z * num4) - (x * num2);
            var num10 = (x * num3) - (y * num4);
            var num9 = ((x * num4) + (y * num3)) + (z * num2);
            quaternion.X = ((x * num) + (num4 * w)) + num12;
            quaternion.Y = ((y * num) + (num3 * w)) + num11;
            quaternion.Z = ((z * num) + (num2 * w)) + num10;
            quaternion.W = (w * num) - num9;
            return quaternion;
        }

        /// <summary>Multiplies a quaternion by a scalar value.</summary>
        /// <param name="quaternion1">Source quaternion.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        public static Quaternion operator *(Quaternion quaternion1, float scaleFactor)
        {
            Quaternion quaternion;
            quaternion.X = quaternion1.X * scaleFactor;
            quaternion.Y = quaternion1.Y * scaleFactor;
            quaternion.Z = quaternion1.Z * scaleFactor;
            quaternion.W = quaternion1.W * scaleFactor;
            return quaternion;
        }

        /// <summary>Divides a Quaternion by another Quaternion.</summary>
        /// <param name="quaternion1">Source Quaternion.</param>
        /// <param name="quaternion2">The divisor.</param>
        public static Quaternion operator /(Quaternion quaternion1, Quaternion quaternion2)
        {
            Quaternion quaternion;
            var x = quaternion1.X;
            var y = quaternion1.Y;
            var z = quaternion1.Z;
            var w = quaternion1.W;
            var num14 = (((quaternion2.X * quaternion2.X) + (quaternion2.Y * quaternion2.Y)) + (quaternion2.Z * quaternion2.Z)) +
                        (quaternion2.W * quaternion2.W);
            var num5 = 1f / num14;
            var num4 = -quaternion2.X * num5;
            var num3 = -quaternion2.Y * num5;
            var num2 = -quaternion2.Z * num5;
            var num = quaternion2.W * num5;
            var num13 = (y * num2) - (z * num3);
            var num12 = (z * num4) - (x * num2);
            var num11 = (x * num3) - (y * num4);
            var num10 = ((x * num4) + (y * num3)) + (z * num2);
            quaternion.X = ((x * num) + (num4 * w)) + num13;
            quaternion.Y = ((y * num) + (num3 * w)) + num12;
            quaternion.Z = ((z * num) + (num2 * w)) + num11;
            quaternion.W = (w * num) - num10;
            return quaternion;
        }

        static Quaternion()
        {
            _identity = new Quaternion(0f, 0f, 0f, 1f);
        }
    }
}