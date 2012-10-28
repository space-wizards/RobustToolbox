using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using SFML.Graphics.Design;

namespace SFML.Graphics
{
    /// <summary>Defines a vector with four components.</summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [TypeConverter(typeof(Vector4Converter))]
    public struct Vector4 : IEquatable<Vector4>
    {
        /// <summary>Gets or sets the x-component of the vector.</summary>
        public float X;

        /// <summary>Gets or sets the y-component of the vector.</summary>
        public float Y;

        /// <summary>Gets or sets the z-component of the vector.</summary>
        public float Z;

        /// <summary>Gets or sets the w-component of the vector.</summary>
        public float W;

        static readonly Vector4 _zero;
        static readonly Vector4 _one;
        static readonly Vector4 _unitX;
        static readonly Vector4 _unitY;
        static readonly Vector4 _unitZ;
        static readonly Vector4 _unitW;

        /// <summary>Returns a Vector4 with all of its components set to zero.</summary>
        public static Vector4 Zero
        {
            get { return _zero; }
        }

        /// <summary>Returns a Vector4 with all of its components set to one.</summary>
        public static Vector4 One
        {
            get { return _one; }
        }

        /// <summary>Returns the Vector4 (1, 0, 0, 0).</summary>
        public static Vector4 UnitX
        {
            get { return _unitX; }
        }

        /// <summary>Returns the Vector4 (0, 1, 0, 0).</summary>
        public static Vector4 UnitY
        {
            get { return _unitY; }
        }

        /// <summary>Returns the Vector4 (0, 0, 1, 0).</summary>
        public static Vector4 UnitZ
        {
            get { return _unitZ; }
        }

        /// <summary>Returns the Vector4 (0, 0, 0, 1).</summary>
        public static Vector4 UnitW
        {
            get { return _unitW; }
        }

        /// <summary>Initializes a new instance of Vector4.</summary>
        /// <param name="x">Initial value for the x-component of the vector.</param>
        /// <param name="y">Initial value for the y-component of the vector.</param>
        /// <param name="z">Initial value for the z-component of the vector.</param>
        /// <param name="w">Initial value for the w-component of the vector.</param>
        public Vector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        /// <summary>Initializes a new instance of Vector4.</summary>
        /// <param name="value">A vector containing the values to initialize x and y components with.</param>
        /// <param name="z">Initial value for the z-component of the vector.</param>
        /// <param name="w">Initial value for the w-component of the vector.</param>
        public Vector4(Vector2 value, float z, float w)
        {
            X = value.X;
            Y = value.Y;
            Z = z;
            W = w;
        }

        /// <summary>Initializes a new instance of Vector4.</summary>
        /// <param name="value">A vector containing the values to initialize x, y, and z components with.</param>
        /// <param name="w">Initial value for the w-component of the vector.</param>
        public Vector4(Vector3 value, float w)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
            W = w;
        }

        /// <summary>Creates a new instance of Vector4.</summary>
        /// <param name="value">Value to initialize each component to.</param>
        public Vector4(float value)
        {
            X = Y = Z = W = value;
        }

        /// <summary>Retrieves a string representation of the current object.</summary>
        public override string ToString()
        {
            var currentCulture = CultureInfo.CurrentCulture;
            return string.Format(currentCulture, "{{X:{0} Y:{1} Z:{2} W:{3}}}",
                new object[]
                { X.ToString(currentCulture), Y.ToString(currentCulture), Z.ToString(currentCulture), W.ToString(currentCulture) });
        }

        /// <summary>Determines whether the specified Object is equal to the Vector4.</summary>
        /// <param name="other">The Vector4 to compare with the current Vector4.</param>
        public bool Equals(Vector4 other)
        {
            return ((((X == other.X) && (Y == other.Y)) && (Z == other.Z)) && (W == other.W));
        }

        /// <summary>Returns a value that indicates whether the current instance is equal to a specified object.</summary>
        /// <param name="obj">Object with which to make the comparison.</param>
        public override bool Equals(object obj)
        {
            var flag = false;
            if (obj is Vector4)
                flag = Equals((Vector4)obj);
            return flag;
        }

        /// <summary>Gets the hash code of this object.</summary>
        public override int GetHashCode()
        {
            return (((X.GetHashCode() + Y.GetHashCode()) + Z.GetHashCode()) + W.GetHashCode());
        }

        /// <summary>Calculates the length of the vector.</summary>
        public float Length()
        {
            var num = (((X * X) + (Y * Y)) + (Z * Z)) + (W * W);
            return (float)Math.Sqrt(num);
        }

        /// <summary>Calculates the length of the vector squared.</summary>
        public float LengthSquared()
        {
            return ((((X * X) + (Y * Y)) + (Z * Z)) + (W * W));
        }

        /// <summary>Calculates the distance between two vectors.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static float Distance(Vector4 value1, Vector4 value2)
        {
            var num4 = value1.X - value2.X;
            var num3 = value1.Y - value2.Y;
            var num2 = value1.Z - value2.Z;
            var num = value1.W - value2.W;
            var num5 = (((num4 * num4) + (num3 * num3)) + (num2 * num2)) + (num * num);
            return (float)Math.Sqrt(num5);
        }

        /// <summary>Calculates the distance between two vectors.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="result">[OutAttribute] The distance between the vectors.</param>
        public static void Distance(ref Vector4 value1, ref Vector4 value2, out float result)
        {
            var num4 = value1.X - value2.X;
            var num3 = value1.Y - value2.Y;
            var num2 = value1.Z - value2.Z;
            var num = value1.W - value2.W;
            var num5 = (((num4 * num4) + (num3 * num3)) + (num2 * num2)) + (num * num);
            result = (float)Math.Sqrt(num5);
        }

        /// <summary>Calculates the distance between two vectors squared.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static float DistanceSquared(Vector4 value1, Vector4 value2)
        {
            var num4 = value1.X - value2.X;
            var num3 = value1.Y - value2.Y;
            var num2 = value1.Z - value2.Z;
            var num = value1.W - value2.W;
            return ((((num4 * num4) + (num3 * num3)) + (num2 * num2)) + (num * num));
        }

        /// <summary>Calculates the distance between two vectors squared.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="result">[OutAttribute] The distance between the two vectors squared.</param>
        public static void DistanceSquared(ref Vector4 value1, ref Vector4 value2, out float result)
        {
            var num4 = value1.X - value2.X;
            var num3 = value1.Y - value2.Y;
            var num2 = value1.Z - value2.Z;
            var num = value1.W - value2.W;
            result = (((num4 * num4) + (num3 * num3)) + (num2 * num2)) + (num * num);
        }

        /// <summary>Calculates the dot product of two vectors.</summary>
        /// <param name="vector1">Source vector.</param>
        /// <param name="vector2">Source vector.</param>
        public static float Dot(Vector4 vector1, Vector4 vector2)
        {
            return ((((vector1.X * vector2.X) + (vector1.Y * vector2.Y)) + (vector1.Z * vector2.Z)) + (vector1.W * vector2.W));
        }

        /// <summary>Calculates the dot product of two vectors.</summary>
        /// <param name="vector1">Source vector.</param>
        /// <param name="vector2">Source vector.</param>
        /// <param name="result">[OutAttribute] The dot product of the two vectors.</param>
        public static void Dot(ref Vector4 vector1, ref Vector4 vector2, out float result)
        {
            result = (((vector1.X * vector2.X) + (vector1.Y * vector2.Y)) + (vector1.Z * vector2.Z)) + (vector1.W * vector2.W);
        }

        /// <summary>Turns the current vector into a unit vector.</summary>
        public void Normalize()
        {
            var num2 = (((X * X) + (Y * Y)) + (Z * Z)) + (W * W);
            var num = 1f / ((float)Math.Sqrt(num2));
            X *= num;
            Y *= num;
            Z *= num;
            W *= num;
        }

        /// <summary>Creates a unit vector from the specified vector.</summary>
        /// <param name="vector">The source Vector4.</param>
        public static Vector4 Normalize(Vector4 vector)
        {
            Vector4 vector2;
            var num2 = (((vector.X * vector.X) + (vector.Y * vector.Y)) + (vector.Z * vector.Z)) + (vector.W * vector.W);
            var num = 1f / ((float)Math.Sqrt(num2));
            vector2.X = vector.X * num;
            vector2.Y = vector.Y * num;
            vector2.Z = vector.Z * num;
            vector2.W = vector.W * num;
            return vector2;
        }

        /// <summary>Returns a normalized version of the specified vector.</summary>
        /// <param name="vector">Source vector.</param>
        /// <param name="result">[OutAttribute] The normalized vector.</param>
        public static void Normalize(ref Vector4 vector, out Vector4 result)
        {
            var num2 = (((vector.X * vector.X) + (vector.Y * vector.Y)) + (vector.Z * vector.Z)) + (vector.W * vector.W);
            var num = 1f / ((float)Math.Sqrt(num2));
            result.X = vector.X * num;
            result.Y = vector.Y * num;
            result.Z = vector.Z * num;
            result.W = vector.W * num;
        }

        /// <summary>Returns a vector that contains the lowest value from each matching pair of components.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector4 Min(Vector4 value1, Vector4 value2)
        {
            Vector4 vector;
            vector.X = (value1.X < value2.X) ? value1.X : value2.X;
            vector.Y = (value1.Y < value2.Y) ? value1.Y : value2.Y;
            vector.Z = (value1.Z < value2.Z) ? value1.Z : value2.Z;
            vector.W = (value1.W < value2.W) ? value1.W : value2.W;
            return vector;
        }

        /// <summary>Returns a vector that contains the lowest value from each matching pair of components.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="result">[OutAttribute] The minimized vector.</param>
        public static void Min(ref Vector4 value1, ref Vector4 value2, out Vector4 result)
        {
            result.X = (value1.X < value2.X) ? value1.X : value2.X;
            result.Y = (value1.Y < value2.Y) ? value1.Y : value2.Y;
            result.Z = (value1.Z < value2.Z) ? value1.Z : value2.Z;
            result.W = (value1.W < value2.W) ? value1.W : value2.W;
        }

        /// <summary>Returns a vector that contains the highest value from each matching pair of components.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector4 Max(Vector4 value1, Vector4 value2)
        {
            Vector4 vector;
            vector.X = (value1.X > value2.X) ? value1.X : value2.X;
            vector.Y = (value1.Y > value2.Y) ? value1.Y : value2.Y;
            vector.Z = (value1.Z > value2.Z) ? value1.Z : value2.Z;
            vector.W = (value1.W > value2.W) ? value1.W : value2.W;
            return vector;
        }

        /// <summary>Returns a vector that contains the highest value from each matching pair of components.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="result">[OutAttribute] The maximized vector.</param>
        public static void Max(ref Vector4 value1, ref Vector4 value2, out Vector4 result)
        {
            result.X = (value1.X > value2.X) ? value1.X : value2.X;
            result.Y = (value1.Y > value2.Y) ? value1.Y : value2.Y;
            result.Z = (value1.Z > value2.Z) ? value1.Z : value2.Z;
            result.W = (value1.W > value2.W) ? value1.W : value2.W;
        }

        /// <summary>Restricts a value to be within a specified range.</summary>
        /// <param name="value1">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        public static Vector4 Clamp(Vector4 value1, Vector4 min, Vector4 max)
        {
            Vector4 vector;
            var x = value1.X;
            x = (x > max.X) ? max.X : x;
            x = (x < min.X) ? min.X : x;
            var y = value1.Y;
            y = (y > max.Y) ? max.Y : y;
            y = (y < min.Y) ? min.Y : y;
            var z = value1.Z;
            z = (z > max.Z) ? max.Z : z;
            z = (z < min.Z) ? min.Z : z;
            var w = value1.W;
            w = (w > max.W) ? max.W : w;
            w = (w < min.W) ? min.W : w;
            vector.X = x;
            vector.Y = y;
            vector.Z = z;
            vector.W = w;
            return vector;
        }

        /// <summary>Restricts a value to be within a specified range.</summary>
        /// <param name="value1">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="result">[OutAttribute] The clamped value.</param>
        public static void Clamp(ref Vector4 value1, ref Vector4 min, ref Vector4 max, out Vector4 result)
        {
            var x = value1.X;
            x = (x > max.X) ? max.X : x;
            x = (x < min.X) ? min.X : x;
            var y = value1.Y;
            y = (y > max.Y) ? max.Y : y;
            y = (y < min.Y) ? min.Y : y;
            var z = value1.Z;
            z = (z > max.Z) ? max.Z : z;
            z = (z < min.Z) ? min.Z : z;
            var w = value1.W;
            w = (w > max.W) ? max.W : w;
            w = (w < min.W) ? min.W : w;
            result.X = x;
            result.Y = y;
            result.Z = z;
            result.W = w;
        }

        /// <summary>Performs a linear interpolation between two vectors.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of value2.</param>
        public static Vector4 Lerp(Vector4 value1, Vector4 value2, float amount)
        {
            Vector4 vector;
            vector.X = value1.X + ((value2.X - value1.X) * amount);
            vector.Y = value1.Y + ((value2.Y - value1.Y) * amount);
            vector.Z = value1.Z + ((value2.Z - value1.Z) * amount);
            vector.W = value1.W + ((value2.W - value1.W) * amount);
            return vector;
        }

        /// <summary>Performs a linear interpolation between two vectors.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of value2.</param>
        /// <param name="result">[OutAttribute] The result of the interpolation.</param>
        public static void Lerp(ref Vector4 value1, ref Vector4 value2, float amount, out Vector4 result)
        {
            result.X = value1.X + ((value2.X - value1.X) * amount);
            result.Y = value1.Y + ((value2.Y - value1.Y) * amount);
            result.Z = value1.Z + ((value2.Z - value1.Z) * amount);
            result.W = value1.W + ((value2.W - value1.W) * amount);
        }

        /// <summary>Returns a Vector4 containing the 4D Cartesian coordinates of a point specified in barycentric (areal) coordinates relative to a 4D triangle.</summary>
        /// <param name="value1">A Vector4 containing the 4D Cartesian coordinates of vertex 1 of the triangle.</param>
        /// <param name="value2">A Vector4 containing the 4D Cartesian coordinates of vertex 2 of the triangle.</param>
        /// <param name="value3">A Vector4 containing the 4D Cartesian coordinates of vertex 3 of the triangle.</param>
        /// <param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in value2).</param>
        /// <param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in value3).</param>
        public static Vector4 Barycentric(Vector4 value1, Vector4 value2, Vector4 value3, float amount1, float amount2)
        {
            Vector4 vector;
            vector.X = (value1.X + (amount1 * (value2.X - value1.X))) + (amount2 * (value3.X - value1.X));
            vector.Y = (value1.Y + (amount1 * (value2.Y - value1.Y))) + (amount2 * (value3.Y - value1.Y));
            vector.Z = (value1.Z + (amount1 * (value2.Z - value1.Z))) + (amount2 * (value3.Z - value1.Z));
            vector.W = (value1.W + (amount1 * (value2.W - value1.W))) + (amount2 * (value3.W - value1.W));
            return vector;
        }

        /// <summary>Returns a Vector4 containing the 4D Cartesian coordinates of a point specified in Barycentric (areal) coordinates relative to a 4D triangle.</summary>
        /// <param name="value1">A Vector4 containing the 4D Cartesian coordinates of vertex 1 of the triangle.</param>
        /// <param name="value2">A Vector4 containing the 4D Cartesian coordinates of vertex 2 of the triangle.</param>
        /// <param name="value3">A Vector4 containing the 4D Cartesian coordinates of vertex 3 of the triangle.</param>
        /// <param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in value2).</param>
        /// <param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in value3).</param>
        /// <param name="result">[OutAttribute] The 4D Cartesian coordinates of the specified point are placed in this Vector4 on exit.</param>
        public static void Barycentric(ref Vector4 value1, ref Vector4 value2, ref Vector4 value3, float amount1, float amount2,
                                       out Vector4 result)
        {
            result.X = (value1.X + (amount1 * (value2.X - value1.X))) + (amount2 * (value3.X - value1.X));
            result.Y = (value1.Y + (amount1 * (value2.Y - value1.Y))) + (amount2 * (value3.Y - value1.Y));
            result.Z = (value1.Z + (amount1 * (value2.Z - value1.Z))) + (amount2 * (value3.Z - value1.Z));
            result.W = (value1.W + (amount1 * (value2.W - value1.W))) + (amount2 * (value3.W - value1.W));
        }

        /// <summary>Interpolates between two values using a cubic equation.</summary>
        /// <param name="value1">Source value.</param>
        /// <param name="value2">Source value.</param>
        /// <param name="amount">Weighting value.</param>
        public static Vector4 SmoothStep(Vector4 value1, Vector4 value2, float amount)
        {
            Vector4 vector;
            amount = (amount > 1f) ? 1f : ((amount < 0f) ? 0f : amount);
            amount = (amount * amount) * (3f - (2f * amount));
            vector.X = value1.X + ((value2.X - value1.X) * amount);
            vector.Y = value1.Y + ((value2.Y - value1.Y) * amount);
            vector.Z = value1.Z + ((value2.Z - value1.Z) * amount);
            vector.W = value1.W + ((value2.W - value1.W) * amount);
            return vector;
        }

        /// <summary>Interpolates between two values using a cubic equation.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <param name="result">[OutAttribute] The interpolated value.</param>
        public static void SmoothStep(ref Vector4 value1, ref Vector4 value2, float amount, out Vector4 result)
        {
            amount = (amount > 1f) ? 1f : ((amount < 0f) ? 0f : amount);
            amount = (amount * amount) * (3f - (2f * amount));
            result.X = value1.X + ((value2.X - value1.X) * amount);
            result.Y = value1.Y + ((value2.Y - value1.Y) * amount);
            result.Z = value1.Z + ((value2.Z - value1.Z) * amount);
            result.W = value1.W + ((value2.W - value1.W) * amount);
        }

        /// <summary>Performs a Catmull-Rom interpolation using the specified positions.</summary>
        /// <param name="value1">The first position in the interpolation.</param>
        /// <param name="value2">The second position in the interpolation.</param>
        /// <param name="value3">The third position in the interpolation.</param>
        /// <param name="value4">The fourth position in the interpolation.</param>
        /// <param name="amount">Weighting factor.</param>
        public static Vector4 CatmullRom(Vector4 value1, Vector4 value2, Vector4 value3, Vector4 value4, float amount)
        {
            Vector4 vector;
            var num = amount * amount;
            var num2 = amount * num;
            vector.X = 0.5f *
                       ((((2f * value2.X) + ((-value1.X + value3.X) * amount)) +
                         (((((2f * value1.X) - (5f * value2.X)) + (4f * value3.X)) - value4.X) * num)) +
                        ((((-value1.X + (3f * value2.X)) - (3f * value3.X)) + value4.X) * num2));
            vector.Y = 0.5f *
                       ((((2f * value2.Y) + ((-value1.Y + value3.Y) * amount)) +
                         (((((2f * value1.Y) - (5f * value2.Y)) + (4f * value3.Y)) - value4.Y) * num)) +
                        ((((-value1.Y + (3f * value2.Y)) - (3f * value3.Y)) + value4.Y) * num2));
            vector.Z = 0.5f *
                       ((((2f * value2.Z) + ((-value1.Z + value3.Z) * amount)) +
                         (((((2f * value1.Z) - (5f * value2.Z)) + (4f * value3.Z)) - value4.Z) * num)) +
                        ((((-value1.Z + (3f * value2.Z)) - (3f * value3.Z)) + value4.Z) * num2));
            vector.W = 0.5f *
                       ((((2f * value2.W) + ((-value1.W + value3.W) * amount)) +
                         (((((2f * value1.W) - (5f * value2.W)) + (4f * value3.W)) - value4.W) * num)) +
                        ((((-value1.W + (3f * value2.W)) - (3f * value3.W)) + value4.W) * num2));
            return vector;
        }

        /// <summary>Performs a Catmull-Rom interpolation using the specified positions.</summary>
        /// <param name="value1">The first position in the interpolation.</param>
        /// <param name="value2">The second position in the interpolation.</param>
        /// <param name="value3">The third position in the interpolation.</param>
        /// <param name="value4">The fourth position in the interpolation.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <param name="result">[OutAttribute] A vector that is the result of the Catmull-Rom interpolation.</param>
        public static void CatmullRom(ref Vector4 value1, ref Vector4 value2, ref Vector4 value3, ref Vector4 value4, float amount,
                                      out Vector4 result)
        {
            var num = amount * amount;
            var num2 = amount * num;
            result.X = 0.5f *
                       ((((2f * value2.X) + ((-value1.X + value3.X) * amount)) +
                         (((((2f * value1.X) - (5f * value2.X)) + (4f * value3.X)) - value4.X) * num)) +
                        ((((-value1.X + (3f * value2.X)) - (3f * value3.X)) + value4.X) * num2));
            result.Y = 0.5f *
                       ((((2f * value2.Y) + ((-value1.Y + value3.Y) * amount)) +
                         (((((2f * value1.Y) - (5f * value2.Y)) + (4f * value3.Y)) - value4.Y) * num)) +
                        ((((-value1.Y + (3f * value2.Y)) - (3f * value3.Y)) + value4.Y) * num2));
            result.Z = 0.5f *
                       ((((2f * value2.Z) + ((-value1.Z + value3.Z) * amount)) +
                         (((((2f * value1.Z) - (5f * value2.Z)) + (4f * value3.Z)) - value4.Z) * num)) +
                        ((((-value1.Z + (3f * value2.Z)) - (3f * value3.Z)) + value4.Z) * num2));
            result.W = 0.5f *
                       ((((2f * value2.W) + ((-value1.W + value3.W) * amount)) +
                         (((((2f * value1.W) - (5f * value2.W)) + (4f * value3.W)) - value4.W) * num)) +
                        ((((-value1.W + (3f * value2.W)) - (3f * value3.W)) + value4.W) * num2));
        }

        /// <summary>Performs a Hermite spline interpolation.</summary>
        /// <param name="value1">Source position vector.</param>
        /// <param name="tangent1">Source tangent vector.</param>
        /// <param name="value2">Source position vector.</param>
        /// <param name="tangent2">Source tangent vector.</param>
        /// <param name="amount">Weighting factor.</param>
        public static Vector4 Hermite(Vector4 value1, Vector4 tangent1, Vector4 value2, Vector4 tangent2, float amount)
        {
            Vector4 vector;
            var num = amount * amount;
            var num6 = amount * num;
            var num5 = ((2f * num6) - (3f * num)) + 1f;
            var num4 = (-2f * num6) + (3f * num);
            var num3 = (num6 - (2f * num)) + amount;
            var num2 = num6 - num;
            vector.X = (((value1.X * num5) + (value2.X * num4)) + (tangent1.X * num3)) + (tangent2.X * num2);
            vector.Y = (((value1.Y * num5) + (value2.Y * num4)) + (tangent1.Y * num3)) + (tangent2.Y * num2);
            vector.Z = (((value1.Z * num5) + (value2.Z * num4)) + (tangent1.Z * num3)) + (tangent2.Z * num2);
            vector.W = (((value1.W * num5) + (value2.W * num4)) + (tangent1.W * num3)) + (tangent2.W * num2);
            return vector;
        }

        /// <summary>Performs a Hermite spline interpolation.</summary>
        /// <param name="value1">Source position vector.</param>
        /// <param name="tangent1">Source tangent vector.</param>
        /// <param name="value2">Source position vector.</param>
        /// <param name="tangent2">Source tangent vector.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <param name="result">[OutAttribute] The result of the Hermite spline interpolation.</param>
        public static void Hermite(ref Vector4 value1, ref Vector4 tangent1, ref Vector4 value2, ref Vector4 tangent2,
                                   float amount, out Vector4 result)
        {
            var num = amount * amount;
            var num6 = amount * num;
            var num5 = ((2f * num6) - (3f * num)) + 1f;
            var num4 = (-2f * num6) + (3f * num);
            var num3 = (num6 - (2f * num)) + amount;
            var num2 = num6 - num;
            result.X = (((value1.X * num5) + (value2.X * num4)) + (tangent1.X * num3)) + (tangent2.X * num2);
            result.Y = (((value1.Y * num5) + (value2.Y * num4)) + (tangent1.Y * num3)) + (tangent2.Y * num2);
            result.Z = (((value1.Z * num5) + (value2.Z * num4)) + (tangent1.Z * num3)) + (tangent2.Z * num2);
            result.W = (((value1.W * num5) + (value2.W * num4)) + (tangent1.W * num3)) + (tangent2.W * num2);
        }

        /// <summary>Transforms a Vector2 by the given Matrix.</summary>
        /// <param name="position">The source Vector2.</param>
        /// <param name="matrix">The transformation Matrix.</param>
        public static Vector4 Transform(Vector2 position, Matrix matrix)
        {
            Vector4 vector;
            var num4 = ((position.X * matrix.M11) + (position.Y * matrix.M21)) + matrix.M41;
            var num3 = ((position.X * matrix.M12) + (position.Y * matrix.M22)) + matrix.M42;
            var num2 = ((position.X * matrix.M13) + (position.Y * matrix.M23)) + matrix.M43;
            var num = ((position.X * matrix.M14) + (position.Y * matrix.M24)) + matrix.M44;
            vector.X = num4;
            vector.Y = num3;
            vector.Z = num2;
            vector.W = num;
            return vector;
        }

        /// <summary>Transforms a Vector2 by the given Matrix.</summary>
        /// <param name="position">The source Vector2.</param>
        /// <param name="matrix">The transformation Matrix.</param>
        /// <param name="result">[OutAttribute] The Vector4 resulting from the transformation.</param>
        public static void Transform(ref Vector2 position, ref Matrix matrix, out Vector4 result)
        {
            var num4 = ((position.X * matrix.M11) + (position.Y * matrix.M21)) + matrix.M41;
            var num3 = ((position.X * matrix.M12) + (position.Y * matrix.M22)) + matrix.M42;
            var num2 = ((position.X * matrix.M13) + (position.Y * matrix.M23)) + matrix.M43;
            var num = ((position.X * matrix.M14) + (position.Y * matrix.M24)) + matrix.M44;
            result.X = num4;
            result.Y = num3;
            result.Z = num2;
            result.W = num;
        }

        /// <summary>Transforms a Vector3 by the given Matrix.</summary>
        /// <param name="position">The source Vector3.</param>
        /// <param name="matrix">The transformation Matrix.</param>
        public static Vector4 Transform(Vector3 position, Matrix matrix)
        {
            Vector4 vector;
            var num4 = (((position.X * matrix.M11) + (position.Y * matrix.M21)) + (position.Z * matrix.M31)) + matrix.M41;
            var num3 = (((position.X * matrix.M12) + (position.Y * matrix.M22)) + (position.Z * matrix.M32)) + matrix.M42;
            var num2 = (((position.X * matrix.M13) + (position.Y * matrix.M23)) + (position.Z * matrix.M33)) + matrix.M43;
            var num = (((position.X * matrix.M14) + (position.Y * matrix.M24)) + (position.Z * matrix.M34)) + matrix.M44;
            vector.X = num4;
            vector.Y = num3;
            vector.Z = num2;
            vector.W = num;
            return vector;
        }

        /// <summary>Transforms a Vector3 by the given Matrix.</summary>
        /// <param name="position">The source Vector3.</param>
        /// <param name="matrix">The transformation Matrix.</param>
        /// <param name="result">[OutAttribute] The Vector4 resulting from the transformation.</param>
        public static void Transform(ref Vector3 position, ref Matrix matrix, out Vector4 result)
        {
            var num4 = (((position.X * matrix.M11) + (position.Y * matrix.M21)) + (position.Z * matrix.M31)) + matrix.M41;
            var num3 = (((position.X * matrix.M12) + (position.Y * matrix.M22)) + (position.Z * matrix.M32)) + matrix.M42;
            var num2 = (((position.X * matrix.M13) + (position.Y * matrix.M23)) + (position.Z * matrix.M33)) + matrix.M43;
            var num = (((position.X * matrix.M14) + (position.Y * matrix.M24)) + (position.Z * matrix.M34)) + matrix.M44;
            result.X = num4;
            result.Y = num3;
            result.Z = num2;
            result.W = num;
        }

        /// <summary>Transforms a Vector4 by the specified Matrix.</summary>
        /// <param name="vector">The source Vector4.</param>
        /// <param name="matrix">The transformation Matrix.</param>
        public static Vector4 Transform(Vector4 vector, Matrix matrix)
        {
            Vector4 vector2;
            var num4 = (((vector.X * matrix.M11) + (vector.Y * matrix.M21)) + (vector.Z * matrix.M31)) + (vector.W * matrix.M41);
            var num3 = (((vector.X * matrix.M12) + (vector.Y * matrix.M22)) + (vector.Z * matrix.M32)) + (vector.W * matrix.M42);
            var num2 = (((vector.X * matrix.M13) + (vector.Y * matrix.M23)) + (vector.Z * matrix.M33)) + (vector.W * matrix.M43);
            var num = (((vector.X * matrix.M14) + (vector.Y * matrix.M24)) + (vector.Z * matrix.M34)) + (vector.W * matrix.M44);
            vector2.X = num4;
            vector2.Y = num3;
            vector2.Z = num2;
            vector2.W = num;
            return vector2;
        }

        /// <summary>Transforms a Vector4 by the given Matrix.</summary>
        /// <param name="vector">The source Vector4.</param>
        /// <param name="matrix">The transformation Matrix.</param>
        /// <param name="result">[OutAttribute] The Vector4 resulting from the transformation.</param>
        public static void Transform(ref Vector4 vector, ref Matrix matrix, out Vector4 result)
        {
            var num4 = (((vector.X * matrix.M11) + (vector.Y * matrix.M21)) + (vector.Z * matrix.M31)) + (vector.W * matrix.M41);
            var num3 = (((vector.X * matrix.M12) + (vector.Y * matrix.M22)) + (vector.Z * matrix.M32)) + (vector.W * matrix.M42);
            var num2 = (((vector.X * matrix.M13) + (vector.Y * matrix.M23)) + (vector.Z * matrix.M33)) + (vector.W * matrix.M43);
            var num = (((vector.X * matrix.M14) + (vector.Y * matrix.M24)) + (vector.Z * matrix.M34)) + (vector.W * matrix.M44);
            result.X = num4;
            result.Y = num3;
            result.Z = num2;
            result.W = num;
        }

        /// <summary>Transforms a Vector2 by a specified Quaternion into a Vector4.</summary>
        /// <param name="value">The Vector2 to transform.</param>
        /// <param name="rotation">The Quaternion rotation to apply.</param>
        public static Vector4 Transform(Vector2 value, Quaternion rotation)
        {
            Vector4 vector;
            var num6 = rotation.X + rotation.X;
            var num2 = rotation.Y + rotation.Y;
            var num = rotation.Z + rotation.Z;
            var num15 = rotation.W * num6;
            var num14 = rotation.W * num2;
            var num5 = rotation.W * num;
            var num13 = rotation.X * num6;
            var num4 = rotation.X * num2;
            var num12 = rotation.X * num;
            var num11 = rotation.Y * num2;
            var num10 = rotation.Y * num;
            var num3 = rotation.Z * num;
            var num9 = (value.X * ((1f - num11) - num3)) + (value.Y * (num4 - num5));
            var num8 = (value.X * (num4 + num5)) + (value.Y * ((1f - num13) - num3));
            var num7 = (value.X * (num12 - num14)) + (value.Y * (num10 + num15));
            vector.X = num9;
            vector.Y = num8;
            vector.Z = num7;
            vector.W = 1f;
            return vector;
        }

        /// <summary>Transforms a Vector2 by a specified Quaternion into a Vector4.</summary>
        /// <param name="value">The Vector2 to transform.</param>
        /// <param name="rotation">The Quaternion rotation to apply.</param>
        /// <param name="result">[OutAttribute] The Vector4 resulting from the transformation.</param>
        public static void Transform(ref Vector2 value, ref Quaternion rotation, out Vector4 result)
        {
            var num6 = rotation.X + rotation.X;
            var num2 = rotation.Y + rotation.Y;
            var num = rotation.Z + rotation.Z;
            var num15 = rotation.W * num6;
            var num14 = rotation.W * num2;
            var num5 = rotation.W * num;
            var num13 = rotation.X * num6;
            var num4 = rotation.X * num2;
            var num12 = rotation.X * num;
            var num11 = rotation.Y * num2;
            var num10 = rotation.Y * num;
            var num3 = rotation.Z * num;
            var num9 = (value.X * ((1f - num11) - num3)) + (value.Y * (num4 - num5));
            var num8 = (value.X * (num4 + num5)) + (value.Y * ((1f - num13) - num3));
            var num7 = (value.X * (num12 - num14)) + (value.Y * (num10 + num15));
            result.X = num9;
            result.Y = num8;
            result.Z = num7;
            result.W = 1f;
        }

        /// <summary>Transforms a Vector3 by a specified Quaternion into a Vector4.</summary>
        /// <param name="value">The Vector3 to transform.</param>
        /// <param name="rotation">The Quaternion rotation to apply.</param>
        public static Vector4 Transform(Vector3 value, Quaternion rotation)
        {
            Vector4 vector;
            var num12 = rotation.X + rotation.X;
            var num2 = rotation.Y + rotation.Y;
            var num = rotation.Z + rotation.Z;
            var num11 = rotation.W * num12;
            var num10 = rotation.W * num2;
            var num9 = rotation.W * num;
            var num8 = rotation.X * num12;
            var num7 = rotation.X * num2;
            var num6 = rotation.X * num;
            var num5 = rotation.Y * num2;
            var num4 = rotation.Y * num;
            var num3 = rotation.Z * num;
            var num15 = ((value.X * ((1f - num5) - num3)) + (value.Y * (num7 - num9))) + (value.Z * (num6 + num10));
            var num14 = ((value.X * (num7 + num9)) + (value.Y * ((1f - num8) - num3))) + (value.Z * (num4 - num11));
            var num13 = ((value.X * (num6 - num10)) + (value.Y * (num4 + num11))) + (value.Z * ((1f - num8) - num5));
            vector.X = num15;
            vector.Y = num14;
            vector.Z = num13;
            vector.W = 1f;
            return vector;
        }

        /// <summary>Transforms a Vector3 by a specified Quaternion into a Vector4.</summary>
        /// <param name="value">The Vector3 to transform.</param>
        /// <param name="rotation">The Quaternion rotation to apply.</param>
        /// <param name="result">[OutAttribute] The Vector4 resulting from the transformation.</param>
        public static void Transform(ref Vector3 value, ref Quaternion rotation, out Vector4 result)
        {
            var num12 = rotation.X + rotation.X;
            var num2 = rotation.Y + rotation.Y;
            var num = rotation.Z + rotation.Z;
            var num11 = rotation.W * num12;
            var num10 = rotation.W * num2;
            var num9 = rotation.W * num;
            var num8 = rotation.X * num12;
            var num7 = rotation.X * num2;
            var num6 = rotation.X * num;
            var num5 = rotation.Y * num2;
            var num4 = rotation.Y * num;
            var num3 = rotation.Z * num;
            var num15 = ((value.X * ((1f - num5) - num3)) + (value.Y * (num7 - num9))) + (value.Z * (num6 + num10));
            var num14 = ((value.X * (num7 + num9)) + (value.Y * ((1f - num8) - num3))) + (value.Z * (num4 - num11));
            var num13 = ((value.X * (num6 - num10)) + (value.Y * (num4 + num11))) + (value.Z * ((1f - num8) - num5));
            result.X = num15;
            result.Y = num14;
            result.Z = num13;
            result.W = 1f;
        }

        /// <summary>Transforms a Vector4 by a specified Quaternion.</summary>
        /// <param name="value">The Vector4 to transform.</param>
        /// <param name="rotation">The Quaternion rotation to apply.</param>
        public static Vector4 Transform(Vector4 value, Quaternion rotation)
        {
            Vector4 vector;
            var num12 = rotation.X + rotation.X;
            var num2 = rotation.Y + rotation.Y;
            var num = rotation.Z + rotation.Z;
            var num11 = rotation.W * num12;
            var num10 = rotation.W * num2;
            var num9 = rotation.W * num;
            var num8 = rotation.X * num12;
            var num7 = rotation.X * num2;
            var num6 = rotation.X * num;
            var num5 = rotation.Y * num2;
            var num4 = rotation.Y * num;
            var num3 = rotation.Z * num;
            var num15 = ((value.X * ((1f - num5) - num3)) + (value.Y * (num7 - num9))) + (value.Z * (num6 + num10));
            var num14 = ((value.X * (num7 + num9)) + (value.Y * ((1f - num8) - num3))) + (value.Z * (num4 - num11));
            var num13 = ((value.X * (num6 - num10)) + (value.Y * (num4 + num11))) + (value.Z * ((1f - num8) - num5));
            vector.X = num15;
            vector.Y = num14;
            vector.Z = num13;
            vector.W = value.W;
            return vector;
        }

        /// <summary>Transforms a Vector4 by a specified Quaternion.</summary>
        /// <param name="value">The Vector4 to transform.</param>
        /// <param name="rotation">The Quaternion rotation to apply.</param>
        /// <param name="result">[OutAttribute] The Vector4 resulting from the transformation.</param>
        public static void Transform(ref Vector4 value, ref Quaternion rotation, out Vector4 result)
        {
            var num12 = rotation.X + rotation.X;
            var num2 = rotation.Y + rotation.Y;
            var num = rotation.Z + rotation.Z;
            var num11 = rotation.W * num12;
            var num10 = rotation.W * num2;
            var num9 = rotation.W * num;
            var num8 = rotation.X * num12;
            var num7 = rotation.X * num2;
            var num6 = rotation.X * num;
            var num5 = rotation.Y * num2;
            var num4 = rotation.Y * num;
            var num3 = rotation.Z * num;
            var num15 = ((value.X * ((1f - num5) - num3)) + (value.Y * (num7 - num9))) + (value.Z * (num6 + num10));
            var num14 = ((value.X * (num7 + num9)) + (value.Y * ((1f - num8) - num3))) + (value.Z * (num4 - num11));
            var num13 = ((value.X * (num6 - num10)) + (value.Y * (num4 + num11))) + (value.Z * ((1f - num8) - num5));
            result.X = num15;
            result.Y = num14;
            result.Z = num13;
            result.W = value.W;
        }

        /// <summary>Transforms an array of Vector4s by a specified Matrix.</summary>
        /// <param name="sourceArray">The array of Vector4s to transform.</param>
        /// <param name="matrix">The transform Matrix to apply.</param>
        /// <param name="destinationArray">The existing destination array into which the transformed Vector4s are written.</param>
        public static void Transform(Vector4[] sourceArray, ref Matrix matrix, Vector4[] destinationArray)
        {
            if (sourceArray == null)
                throw new ArgumentNullException("sourceArray");
            if (destinationArray == null)
                throw new ArgumentNullException("destinationArray");
            if (destinationArray.Length < sourceArray.Length)
                throw new ArgumentException(FrameworkMessages.NotEnoughTargetSize);

            for (var i = 0; i < sourceArray.Length; i++)
            {
                var x = sourceArray[i].X;
                var y = sourceArray[i].Y;
                var z = sourceArray[i].Z;
                var w = sourceArray[i].W;
                destinationArray[i].X = (((x * matrix.M11) + (y * matrix.M21)) + (z * matrix.M31)) + (w * matrix.M41);
                destinationArray[i].Y = (((x * matrix.M12) + (y * matrix.M22)) + (z * matrix.M32)) + (w * matrix.M42);
                destinationArray[i].Z = (((x * matrix.M13) + (y * matrix.M23)) + (z * matrix.M33)) + (w * matrix.M43);
                destinationArray[i].W = (((x * matrix.M14) + (y * matrix.M24)) + (z * matrix.M34)) + (w * matrix.M44);
            }
        }

        /// <summary>Transforms a specified range in an array of Vector4s by a specified Matrix into a specified range in a destination array.</summary>
        /// <param name="sourceArray">The array of Vector4s containing the range to transform.</param>
        /// <param name="sourceIndex">The index in the source array of the first Vector4 to transform.</param>
        /// <param name="matrix">The transform Matrix to apply.</param>
        /// <param name="destinationArray">The existing destination array of Vector4s into which to write the results.</param>
        /// <param name="destinationIndex">The index in the destination array of the first result Vector4 to write.</param>
        /// <param name="length">The number of Vector4s to transform.</param>
        public static void Transform(Vector4[] sourceArray, int sourceIndex, ref Matrix matrix, Vector4[] destinationArray,
                                     int destinationIndex, int length)
        {
            if (sourceArray == null)
                throw new ArgumentNullException("sourceArray");
            if (destinationArray == null)
                throw new ArgumentNullException("destinationArray");
            if (sourceArray.Length < (sourceIndex + length))
                throw new ArgumentException(FrameworkMessages.NotEnoughSourceSize);
            if (destinationArray.Length < (destinationIndex + length))
                throw new ArgumentException(FrameworkMessages.NotEnoughTargetSize);

            while (length > 0)
            {
                var x = sourceArray[sourceIndex].X;
                var y = sourceArray[sourceIndex].Y;
                var z = sourceArray[sourceIndex].Z;
                var w = sourceArray[sourceIndex].W;
                destinationArray[destinationIndex].X = (((x * matrix.M11) + (y * matrix.M21)) + (z * matrix.M31)) +
                                                       (w * matrix.M41);
                destinationArray[destinationIndex].Y = (((x * matrix.M12) + (y * matrix.M22)) + (z * matrix.M32)) +
                                                       (w * matrix.M42);
                destinationArray[destinationIndex].Z = (((x * matrix.M13) + (y * matrix.M23)) + (z * matrix.M33)) +
                                                       (w * matrix.M43);
                destinationArray[destinationIndex].W = (((x * matrix.M14) + (y * matrix.M24)) + (z * matrix.M34)) +
                                                       (w * matrix.M44);
                sourceIndex++;
                destinationIndex++;
                length--;
            }
        }

        /// <summary>Transforms an array of Vector4s by a specified Quaternion.</summary>
        /// <param name="sourceArray">The array of Vector4s to transform.</param>
        /// <param name="rotation">The Quaternion rotation to apply.</param>
        /// <param name="destinationArray">The existing destination array into which the transformed Vector4s are written.</param>
        public static void Transform(Vector4[] sourceArray, ref Quaternion rotation, Vector4[] destinationArray)
        {
            if (sourceArray == null)
                throw new ArgumentNullException("sourceArray");
            if (destinationArray == null)
                throw new ArgumentNullException("destinationArray");
            if (destinationArray.Length < sourceArray.Length)
                throw new ArgumentException(FrameworkMessages.NotEnoughTargetSize);

            var num16 = rotation.X + rotation.X;
            var num6 = rotation.Y + rotation.Y;
            var num2 = rotation.Z + rotation.Z;
            var num15 = rotation.W * num16;
            var num14 = rotation.W * num6;
            var num13 = rotation.W * num2;
            var num12 = rotation.X * num16;
            var num11 = rotation.X * num6;
            var num10 = rotation.X * num2;
            var num9 = rotation.Y * num6;
            var num8 = rotation.Y * num2;
            var num7 = rotation.Z * num2;
            var num25 = (1f - num9) - num7;
            var num24 = num11 - num13;
            var num23 = num10 + num14;
            var num22 = num11 + num13;
            var num21 = (1f - num12) - num7;
            var num20 = num8 - num15;
            var num19 = num10 - num14;
            var num18 = num8 + num15;
            var num17 = (1f - num12) - num9;
            for (var i = 0; i < sourceArray.Length; i++)
            {
                var x = sourceArray[i].X;
                var y = sourceArray[i].Y;
                var z = sourceArray[i].Z;
                destinationArray[i].X = ((x * num25) + (y * num24)) + (z * num23);
                destinationArray[i].Y = ((x * num22) + (y * num21)) + (z * num20);
                destinationArray[i].Z = ((x * num19) + (y * num18)) + (z * num17);
                destinationArray[i].W = sourceArray[i].W;
            }
        }

        /// <summary>Transforms a specified range in an array of Vector4s by a specified Quaternion into a specified range in a destination array.</summary>
        /// <param name="sourceArray">The array of Vector4s containing the range to transform.</param>
        /// <param name="sourceIndex">The index in the source array of the first Vector4 to transform.</param>
        /// <param name="rotation">The Quaternion rotation to apply.</param>
        /// <param name="destinationArray">The existing destination array of Vector4s into which to write the results.</param>
        /// <param name="destinationIndex">The index in the destination array of the first result Vector4 to write.</param>
        /// <param name="length">The number of Vector4s to transform.</param>
        public static void Transform(Vector4[] sourceArray, int sourceIndex, ref Quaternion rotation, Vector4[] destinationArray,
                                     int destinationIndex, int length)
        {
            if (sourceArray == null)
                throw new ArgumentNullException("sourceArray");
            if (destinationArray == null)
                throw new ArgumentNullException("destinationArray");
            if (sourceArray.Length < (sourceIndex + length))
                throw new ArgumentException(FrameworkMessages.NotEnoughSourceSize);
            if (destinationArray.Length < (destinationIndex + length))
                throw new ArgumentException(FrameworkMessages.NotEnoughTargetSize);

            var num15 = rotation.X + rotation.X;
            var num5 = rotation.Y + rotation.Y;
            var num = rotation.Z + rotation.Z;
            var num14 = rotation.W * num15;
            var num13 = rotation.W * num5;
            var num12 = rotation.W * num;
            var num11 = rotation.X * num15;
            var num10 = rotation.X * num5;
            var num9 = rotation.X * num;
            var num8 = rotation.Y * num5;
            var num7 = rotation.Y * num;
            var num6 = rotation.Z * num;
            var num25 = (1f - num8) - num6;
            var num24 = num10 - num12;
            var num23 = num9 + num13;
            var num22 = num10 + num12;
            var num21 = (1f - num11) - num6;
            var num20 = num7 - num14;
            var num19 = num9 - num13;
            var num18 = num7 + num14;
            var num17 = (1f - num11) - num8;

            while (length > 0)
            {
                var x = sourceArray[sourceIndex].X;
                var y = sourceArray[sourceIndex].Y;
                var z = sourceArray[sourceIndex].Z;
                var w = sourceArray[sourceIndex].W;
                destinationArray[destinationIndex].X = ((x * num25) + (y * num24)) + (z * num23);
                destinationArray[destinationIndex].Y = ((x * num22) + (y * num21)) + (z * num20);
                destinationArray[destinationIndex].Z = ((x * num19) + (y * num18)) + (z * num17);
                destinationArray[destinationIndex].W = w;
                sourceIndex++;
                destinationIndex++;
                length--;
            }
        }

        /// <summary>Returns a vector pointing in the opposite direction.</summary>
        /// <param name="value">Source vector.</param>
        public static Vector4 Negate(Vector4 value)
        {
            Vector4 vector;
            vector.X = -value.X;
            vector.Y = -value.Y;
            vector.Z = -value.Z;
            vector.W = -value.W;
            return vector;
        }

        /// <summary>Returns a vector pointing in the opposite direction.</summary>
        /// <param name="value">Source vector.</param>
        /// <param name="result">[OutAttribute] Vector pointing in the opposite direction.</param>
        public static void Negate(ref Vector4 value, out Vector4 result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
            result.Z = -value.Z;
            result.W = -value.W;
        }

        /// <summary>Adds two vectors.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector4 Add(Vector4 value1, Vector4 value2)
        {
            Vector4 vector;
            vector.X = value1.X + value2.X;
            vector.Y = value1.Y + value2.Y;
            vector.Z = value1.Z + value2.Z;
            vector.W = value1.W + value2.W;
            return vector;
        }

        /// <summary>Adds two vectors.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="result">[OutAttribute] Sum of the source vectors.</param>
        public static void Add(ref Vector4 value1, ref Vector4 value2, out Vector4 result)
        {
            result.X = value1.X + value2.X;
            result.Y = value1.Y + value2.Y;
            result.Z = value1.Z + value2.Z;
            result.W = value1.W + value2.W;
        }

        /// <summary>Subtracts a vector from a vector.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector4 Subtract(Vector4 value1, Vector4 value2)
        {
            Vector4 vector;
            vector.X = value1.X - value2.X;
            vector.Y = value1.Y - value2.Y;
            vector.Z = value1.Z - value2.Z;
            vector.W = value1.W - value2.W;
            return vector;
        }

        /// <summary>Subtracts a vector from a vector.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="result">[OutAttribute] The result of the subtraction.</param>
        public static void Subtract(ref Vector4 value1, ref Vector4 value2, out Vector4 result)
        {
            result.X = value1.X - value2.X;
            result.Y = value1.Y - value2.Y;
            result.Z = value1.Z - value2.Z;
            result.W = value1.W - value2.W;
        }

        /// <summary>Multiplies the components of two vectors by each other.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector4 Multiply(Vector4 value1, Vector4 value2)
        {
            Vector4 vector;
            vector.X = value1.X * value2.X;
            vector.Y = value1.Y * value2.Y;
            vector.Z = value1.Z * value2.Z;
            vector.W = value1.W * value2.W;
            return vector;
        }

        /// <summary>Multiplies the components of two vectors by each other.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Vector4 value1, ref Vector4 value2, out Vector4 result)
        {
            result.X = value1.X * value2.X;
            result.Y = value1.Y * value2.Y;
            result.Z = value1.Z * value2.Z;
            result.W = value1.W * value2.W;
        }

        /// <summary>Multiplies a vector by a scalar.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        public static Vector4 Multiply(Vector4 value1, float scaleFactor)
        {
            Vector4 vector;
            vector.X = value1.X * scaleFactor;
            vector.Y = value1.Y * scaleFactor;
            vector.Z = value1.Z * scaleFactor;
            vector.W = value1.W * scaleFactor;
            return vector;
        }

        /// <summary>Multiplies a vector by a scalar value.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        /// <param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Vector4 value1, float scaleFactor, out Vector4 result)
        {
            result.X = value1.X * scaleFactor;
            result.Y = value1.Y * scaleFactor;
            result.Z = value1.Z * scaleFactor;
            result.W = value1.W * scaleFactor;
        }

        /// <summary>Divides the components of a vector by the components of another vector.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Divisor vector.</param>
        public static Vector4 Divide(Vector4 value1, Vector4 value2)
        {
            Vector4 vector;
            vector.X = value1.X / value2.X;
            vector.Y = value1.Y / value2.Y;
            vector.Z = value1.Z / value2.Z;
            vector.W = value1.W / value2.W;
            return vector;
        }

        /// <summary>Divides the components of a vector by the components of another vector.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">The divisor.</param>
        /// <param name="result">[OutAttribute] The result of the division.</param>
        public static void Divide(ref Vector4 value1, ref Vector4 value2, out Vector4 result)
        {
            result.X = value1.X / value2.X;
            result.Y = value1.Y / value2.Y;
            result.Z = value1.Z / value2.Z;
            result.W = value1.W / value2.W;
        }

        /// <summary>Divides a vector by a scalar value.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="divider">The divisor.</param>
        public static Vector4 Divide(Vector4 value1, float divider)
        {
            Vector4 vector;
            var num = 1f / divider;
            vector.X = value1.X * num;
            vector.Y = value1.Y * num;
            vector.Z = value1.Z * num;
            vector.W = value1.W * num;
            return vector;
        }

        /// <summary>Divides a vector by a scalar value.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="divider">The divisor.</param>
        /// <param name="result">[OutAttribute] The result of the division.</param>
        public static void Divide(ref Vector4 value1, float divider, out Vector4 result)
        {
            var num = 1f / divider;
            result.X = value1.X * num;
            result.Y = value1.Y * num;
            result.Z = value1.Z * num;
            result.W = value1.W * num;
        }

        /// <summary>Returns a vector pointing in the opposite direction.</summary>
        /// <param name="value">Source vector.</param>
        public static Vector4 operator -(Vector4 value)
        {
            Vector4 vector;
            vector.X = -value.X;
            vector.Y = -value.Y;
            vector.Z = -value.Z;
            vector.W = -value.W;
            return vector;
        }

        /// <summary>Tests vectors for equality.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static bool operator ==(Vector4 value1, Vector4 value2)
        {
            return ((((value1.X == value2.X) && (value1.Y == value2.Y)) && (value1.Z == value2.Z)) && (value1.W == value2.W));
        }

        /// <summary>Tests vectors for inequality.</summary>
        /// <param name="value1">Vector to compare.</param>
        /// <param name="value2">Vector to compare.</param>
        public static bool operator !=(Vector4 value1, Vector4 value2)
        {
            if (((value1.X == value2.X) && (value1.Y == value2.Y)) && (value1.Z == value2.Z))
                return (value1.W != value2.W);
            return true;
        }

        /// <summary>Adds two vectors.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector4 operator +(Vector4 value1, Vector4 value2)
        {
            Vector4 vector;
            vector.X = value1.X + value2.X;
            vector.Y = value1.Y + value2.Y;
            vector.Z = value1.Z + value2.Z;
            vector.W = value1.W + value2.W;
            return vector;
        }

        /// <summary>Subtracts a vector from a vector.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector4 operator -(Vector4 value1, Vector4 value2)
        {
            Vector4 vector;
            vector.X = value1.X - value2.X;
            vector.Y = value1.Y - value2.Y;
            vector.Z = value1.Z - value2.Z;
            vector.W = value1.W - value2.W;
            return vector;
        }

        /// <summary>Multiplies the components of two vectors by each other.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector4 operator *(Vector4 value1, Vector4 value2)
        {
            Vector4 vector;
            vector.X = value1.X * value2.X;
            vector.Y = value1.Y * value2.Y;
            vector.Z = value1.Z * value2.Z;
            vector.W = value1.W * value2.W;
            return vector;
        }

        /// <summary>Multiplies a vector by a scalar.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        public static Vector4 operator *(Vector4 value1, float scaleFactor)
        {
            Vector4 vector;
            vector.X = value1.X * scaleFactor;
            vector.Y = value1.Y * scaleFactor;
            vector.Z = value1.Z * scaleFactor;
            vector.W = value1.W * scaleFactor;
            return vector;
        }

        /// <summary>Multiplies a vector by a scalar.</summary>
        /// <param name="scaleFactor">Scalar value.</param>
        /// <param name="value1">Source vector.</param>
        public static Vector4 operator *(float scaleFactor, Vector4 value1)
        {
            Vector4 vector;
            vector.X = value1.X * scaleFactor;
            vector.Y = value1.Y * scaleFactor;
            vector.Z = value1.Z * scaleFactor;
            vector.W = value1.W * scaleFactor;
            return vector;
        }

        /// <summary>Divides the components of a vector by the components of another vector.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Divisor vector.</param>
        public static Vector4 operator /(Vector4 value1, Vector4 value2)
        {
            Vector4 vector;
            vector.X = value1.X / value2.X;
            vector.Y = value1.Y / value2.Y;
            vector.Z = value1.Z / value2.Z;
            vector.W = value1.W / value2.W;
            return vector;
        }

        /// <summary>Divides a vector by a scalar value.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="divider">The divisor.</param>
        public static Vector4 operator /(Vector4 value1, float divider)
        {
            Vector4 vector;
            var num = 1f / divider;
            vector.X = value1.X * num;
            vector.Y = value1.Y * num;
            vector.Z = value1.Z * num;
            vector.W = value1.W * num;
            return vector;
        }

        static Vector4()
        {
            _zero = new Vector4();
            _one = new Vector4(1f, 1f, 1f, 1f);
            _unitX = new Vector4(1f, 0f, 0f, 0f);
            _unitY = new Vector4(0f, 1f, 0f, 0f);
            _unitZ = new Vector4(0f, 0f, 1f, 0f);
            _unitW = new Vector4(0f, 0f, 0f, 1f);
        }
    }
}