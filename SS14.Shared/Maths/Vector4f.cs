using SFML.System;
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace SS14.Shared.Maths
{
    /// <summary>Defines a vector with four components.</summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector4f : IEquatable<Vector4f>
    {
        /// <summary>Gets or sets the x-component of the vector.</summary>
        public float X;

        /// <summary>Gets or sets the y-component of the vector.</summary>
        public float Y;

        /// <summary>Gets or sets the z-component of the vector.</summary>
        public float Z;

        /// <summary>Gets or sets the w-component of the vector.</summary>
        public float W;

        static readonly Vector4f _zero;
        static readonly Vector4f _one;
        static readonly Vector4f _unitX;
        static readonly Vector4f _unitY;
        static readonly Vector4f _unitZ;
        static readonly Vector4f _unitW;

        /// <summary>Returns a Vector4 with all of its components set to zero.</summary>
        public static Vector4f Zero
        {
            get { return _zero; }
        }

        /// <summary>Returns a Vector4 with all of its components set to one.</summary>
        public static Vector4f One
        {
            get { return _one; }
        }

        /// <summary>Returns the Vector4 (1, 0, 0, 0).</summary>
        public static Vector4f UnitX
        {
            get { return _unitX; }
        }

        /// <summary>Returns the Vector4 (0, 1, 0, 0).</summary>
        public static Vector4f UnitY
        {
            get { return _unitY; }
        }

        /// <summary>Returns the Vector4 (0, 0, 1, 0).</summary>
        public static Vector4f UnitZ
        {
            get { return _unitZ; }
        }

        /// <summary>Returns the Vector4 (0, 0, 0, 1).</summary>
        public static Vector4f UnitW
        {
            get { return _unitW; }
        }

        /// <summary>Initializes a new instance of Vector4.</summary>
        /// <param name="x">Initial value for the x-component of the vector.</param>
        /// <param name="y">Initial value for the y-component of the vector.</param>
        /// <param name="z">Initial value for the z-component of the vector.</param>
        /// <param name="w">Initial value for the w-component of the vector.</param>
        public Vector4f(float x, float y, float z, float w)
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
        public Vector4f(SFML.System.Vector2f value, float z, float w)
        {
            X = value.X;
            Y = value.Y;
            Z = z;
            W = w;
        }

        /// <summary>Initializes a new instance of Vector4.</summary>
        /// <param name="value">A vector containing the values to initialize x, y, and z components with.</param>
        /// <param name="w">Initial value for the w-component of the vector.</param>
        public Vector4f(Vector3f value, float w)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
            W = w;
        }

        /// <summary>Creates a new instance of Vector4.</summary>
        /// <param name="value">Value to initialize each component to.</param>
        public Vector4f(float value)
        {
            X = Y = Z = W = value;
        }

        /// <summary>Retrieves a string representation of the current object.</summary>
        public override string ToString()
        {
            var currentCulture = CultureInfo.CurrentCulture;
            return string.Format(currentCulture, "{{X:{0} Y:{1} Z:{2} W:{3}}}",
                new object[] { X.ToString(currentCulture), Y.ToString(currentCulture), Z.ToString(currentCulture), W.ToString(currentCulture) });
        }

        /// <summary>Determines whether the specified Object is equal to the Vector4.</summary>
        /// <param name="other">The Vector4 to compare with the current Vector4.</param>
        public bool Equals(Vector4f other)
        {
            return ((((X == other.X) && (Y == other.Y)) && (Z == other.Z)) && (W == other.W));
        }

        /// <summary>Returns a value that indicates whether the current instance is equal to a specified object.</summary>
        /// <param name="obj">Object with which to make the comparison.</param>
        public override bool Equals(object obj)
        {
            var flag = false;
            if (obj is Vector4f)
                flag = Equals((Vector4f)obj);
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
        public static float Distance(Vector4f value1, Vector4f value2)
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
        public static void Distance(ref Vector4f value1, ref Vector4f value2, out float result)
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
        public static float DistanceSquared(Vector4f value1, Vector4f value2)
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
        public static void DistanceSquared(ref Vector4f value1, ref Vector4f value2, out float result)
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
        public static float Dot(Vector4f vector1, Vector4f vector2)
        {
            return ((((vector1.X * vector2.X) + (vector1.Y * vector2.Y)) + (vector1.Z * vector2.Z)) + (vector1.W * vector2.W));
        }

        /// <summary>Calculates the dot product of two vectors.</summary>
        /// <param name="vector1">Source vector.</param>
        /// <param name="vector2">Source vector.</param>
        /// <param name="result">[OutAttribute] The dot product of the two vectors.</param>
        public static void Dot(ref Vector4f vector1, ref Vector4f vector2, out float result)
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
        public static Vector4f Normalize(Vector4f vector)
        {
            Vector4f vector2;
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
        public static void Normalize(ref Vector4f vector, out Vector4f result)
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
        public static Vector4f Min(Vector4f value1, Vector4f value2)
        {
            Vector4f vector;
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
        public static void Min(ref Vector4f value1, ref Vector4f value2, out Vector4f result)
        {
            result.X = (value1.X < value2.X) ? value1.X : value2.X;
            result.Y = (value1.Y < value2.Y) ? value1.Y : value2.Y;
            result.Z = (value1.Z < value2.Z) ? value1.Z : value2.Z;
            result.W = (value1.W < value2.W) ? value1.W : value2.W;
        }

        /// <summary>Returns a vector that contains the highest value from each matching pair of components.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector4f Max(Vector4f value1, Vector4f value2)
        {
            Vector4f vector;
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
        public static void Max(ref Vector4f value1, ref Vector4f value2, out Vector4f result)
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
        public static Vector4f Clamp(Vector4f value1, Vector4f min, Vector4f max)
        {
            Vector4f vector;
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
        public static void Clamp(ref Vector4f value1, ref Vector4f min, ref Vector4f max, out Vector4f result)
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
        public static Vector4f Lerp(Vector4f value1, Vector4f value2, float amount)
        {
            Vector4f vector;
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
        public static void Lerp(ref Vector4f value1, ref Vector4f value2, float amount, out Vector4f result)
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
        public static Vector4f Barycentric(Vector4f value1, Vector4f value2, Vector4f value3, float amount1, float amount2)
        {
            Vector4f vector;
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
        public static void Barycentric(ref Vector4f value1, ref Vector4f value2, ref Vector4f value3, float amount1, float amount2,
                                       out Vector4f result)
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
        public static Vector4f SmoothStep(Vector4f value1, Vector4f value2, float amount)
        {
            Vector4f vector;
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
        public static void SmoothStep(ref Vector4f value1, ref Vector4f value2, float amount, out Vector4f result)
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
        public static Vector4f CatmullRom(Vector4f value1, Vector4f value2, Vector4f value3, Vector4f value4, float amount)
        {
            Vector4f vector;
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
        public static void CatmullRom(ref Vector4f value1, ref Vector4f value2, ref Vector4f value3, ref Vector4f value4, float amount,
                                      out Vector4f result)
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
        public static Vector4f Hermite(Vector4f value1, Vector4f tangent1, Vector4f value2, Vector4f tangent2, float amount)
        {
            Vector4f vector;
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
        public static void Hermite(ref Vector4f value1, ref Vector4f tangent1, ref Vector4f value2, ref Vector4f tangent2,
                                   float amount, out Vector4f result)
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

        /// <summary>Returns a vector pointing in the opposite direction.</summary>
        /// <param name="value">Source vector.</param>
        public static Vector4f Negate(Vector4f value)
        {
            Vector4f vector;
            vector.X = -value.X;
            vector.Y = -value.Y;
            vector.Z = -value.Z;
            vector.W = -value.W;
            return vector;
        }

        /// <summary>Returns a vector pointing in the opposite direction.</summary>
        /// <param name="value">Source vector.</param>
        /// <param name="result">[OutAttribute] Vector pointing in the opposite direction.</param>
        public static void Negate(ref Vector4f value, out Vector4f result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
            result.Z = -value.Z;
            result.W = -value.W;
        }

        /// <summary>Adds two vectors.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector4f Add(Vector4f value1, Vector4f value2)
        {
            Vector4f vector;
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
        public static void Add(ref Vector4f value1, ref Vector4f value2, out Vector4f result)
        {
            result.X = value1.X + value2.X;
            result.Y = value1.Y + value2.Y;
            result.Z = value1.Z + value2.Z;
            result.W = value1.W + value2.W;
        }

        /// <summary>Subtracts a vector from a vector.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector4f Subtract(Vector4f value1, Vector4f value2)
        {
            Vector4f vector;
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
        public static void Subtract(ref Vector4f value1, ref Vector4f value2, out Vector4f result)
        {
            result.X = value1.X - value2.X;
            result.Y = value1.Y - value2.Y;
            result.Z = value1.Z - value2.Z;
            result.W = value1.W - value2.W;
        }

        /// <summary>Multiplies the components of two vectors by each other.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector4f Multiply(Vector4f value1, Vector4f value2)
        {
            Vector4f vector;
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
        public static void Multiply(ref Vector4f value1, ref Vector4f value2, out Vector4f result)
        {
            result.X = value1.X * value2.X;
            result.Y = value1.Y * value2.Y;
            result.Z = value1.Z * value2.Z;
            result.W = value1.W * value2.W;
        }

        /// <summary>Multiplies a vector by a scalar.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        public static Vector4f Multiply(Vector4f value1, float scaleFactor)
        {
            Vector4f vector;
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
        public static void Multiply(ref Vector4f value1, float scaleFactor, out Vector4f result)
        {
            result.X = value1.X * scaleFactor;
            result.Y = value1.Y * scaleFactor;
            result.Z = value1.Z * scaleFactor;
            result.W = value1.W * scaleFactor;
        }

        /// <summary>Divides the components of a vector by the components of another vector.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Divisor vector.</param>
        public static Vector4f Divide(Vector4f value1, Vector4f value2)
        {
            Vector4f vector;
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
        public static void Divide(ref Vector4f value1, ref Vector4f value2, out Vector4f result)
        {
            result.X = value1.X / value2.X;
            result.Y = value1.Y / value2.Y;
            result.Z = value1.Z / value2.Z;
            result.W = value1.W / value2.W;
        }

        /// <summary>Divides a vector by a scalar value.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="divider">The divisor.</param>
        public static Vector4f Divide(Vector4f value1, float divider)
        {
            Vector4f vector;
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
        public static void Divide(ref Vector4f value1, float divider, out Vector4f result)
        {
            var num = 1f / divider;
            result.X = value1.X * num;
            result.Y = value1.Y * num;
            result.Z = value1.Z * num;
            result.W = value1.W * num;
        }

        /// <summary>Returns a vector pointing in the opposite direction.</summary>
        /// <param name="value">Source vector.</param>
        public static Vector4f operator -(Vector4f value)
        {
            Vector4f vector;
            vector.X = -value.X;
            vector.Y = -value.Y;
            vector.Z = -value.Z;
            vector.W = -value.W;
            return vector;
        }

        /// <summary>Tests vectors for equality.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static bool operator ==(Vector4f value1, Vector4f value2)
        {
            return ((((value1.X == value2.X) && (value1.Y == value2.Y)) && (value1.Z == value2.Z)) && (value1.W == value2.W));
        }

        /// <summary>Tests vectors for inequality.</summary>
        /// <param name="value1">Vector to compare.</param>
        /// <param name="value2">Vector to compare.</param>
        public static bool operator !=(Vector4f value1, Vector4f value2)
        {
            if (((value1.X == value2.X) && (value1.Y == value2.Y)) && (value1.Z == value2.Z))
                return (value1.W != value2.W);
            return true;
        }

        /// <summary>Adds two vectors.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector4f operator +(Vector4f value1, Vector4f value2)
        {
            Vector4f vector;
            vector.X = value1.X + value2.X;
            vector.Y = value1.Y + value2.Y;
            vector.Z = value1.Z + value2.Z;
            vector.W = value1.W + value2.W;
            return vector;
        }

        /// <summary>Subtracts a vector from a vector.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector4f operator -(Vector4f value1, Vector4f value2)
        {
            Vector4f vector;
            vector.X = value1.X - value2.X;
            vector.Y = value1.Y - value2.Y;
            vector.Z = value1.Z - value2.Z;
            vector.W = value1.W - value2.W;
            return vector;
        }

        /// <summary>Multiplies the components of two vectors by each other.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector4f operator *(Vector4f value1, Vector4f value2)
        {
            Vector4f vector;
            vector.X = value1.X * value2.X;
            vector.Y = value1.Y * value2.Y;
            vector.Z = value1.Z * value2.Z;
            vector.W = value1.W * value2.W;
            return vector;
        }

        /// <summary>Multiplies a vector by a scalar.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        public static Vector4f operator *(Vector4f value1, float scaleFactor)
        {
            Vector4f vector;
            vector.X = value1.X * scaleFactor;
            vector.Y = value1.Y * scaleFactor;
            vector.Z = value1.Z * scaleFactor;
            vector.W = value1.W * scaleFactor;
            return vector;
        }

        /// <summary>Multiplies a vector by a scalar.</summary>
        /// <param name="scaleFactor">Scalar value.</param>
        /// <param name="value1">Source vector.</param>
        public static Vector4f operator *(float scaleFactor, Vector4f value1)
        {
            Vector4f vector;
            vector.X = value1.X * scaleFactor;
            vector.Y = value1.Y * scaleFactor;
            vector.Z = value1.Z * scaleFactor;
            vector.W = value1.W * scaleFactor;
            return vector;
        }

        /// <summary>Divides the components of a vector by the components of another vector.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Divisor vector.</param>
        public static Vector4f operator /(Vector4f value1, Vector4f value2)
        {
            Vector4f vector;
            vector.X = value1.X / value2.X;
            vector.Y = value1.Y / value2.Y;
            vector.Z = value1.Z / value2.Z;
            vector.W = value1.W / value2.W;
            return vector;
        }

        /// <summary>Divides a vector by a scalar value.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="divider">The divisor.</param>
        public static Vector4f operator /(Vector4f value1, float divider)
        {
            Vector4f vector;
            var num = 1f / divider;
            vector.X = value1.X * num;
            vector.Y = value1.Y * num;
            vector.Z = value1.Z * num;
            vector.W = value1.W * num;
            return vector;
        }

        static Vector4f()
        {
            _zero = new Vector4f();
            _one = new Vector4f(1f, 1f, 1f, 1f);
            _unitX = new Vector4f(1f, 0f, 0f, 0f);
            _unitY = new Vector4f(0f, 1f, 0f, 0f);
            _unitZ = new Vector4f(0f, 0f, 1f, 0f);
            _unitW = new Vector4f(0f, 0f, 0f, 1f);
        }
    }
}
