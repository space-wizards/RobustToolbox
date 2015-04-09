#region Imports

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using SVector2f = SFML.System.Vector2f;

// for various Xml attributes

#endregion

namespace SS14.Shared.Maths
{
    /// <summary>Defines a vector with two components.</summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Vector2 : IEquatable<Vector2>
    {
        #region Properties
        /// <summary>Gets or sets the x-component of the vector.</summary>
        public float X;

        /// <summary>Gets or sets the y-component of the vector.</summary>
        public float Y;
        #endregion //Properties

        #region Statics
        private static readonly Vector2 _zero = new Vector2(0, 0);
        private static readonly Vector2 _one = new Vector2(1.0f, 1.0f);
        private static readonly Vector2 _unitX = new Vector2(1.0f, 0);
        private static readonly Vector2 _unitY = new Vector2(0, 1.0f);

        /*/// <summary>Explicit Coversion operator for Points</summary>
        public static explicit operator Vector2(Point i)
        {
            return new Vector2(i.X, i.Y);
        }

        /// <summary>Implicit Coversion operator for Points</summary>
        public static implicit operator Vector2(Point i)
        {
            return new Vector2(i.X, i.Y);
        }*/

        /// <summary>Returns a Vector2 with all of its components set to zero.</summary>
        public static Vector2 Zero
        {
            get { return _zero; }
        }

        /// <summary>Returns a Vector2 with both of its components set to one.</summary>
        public static Vector2 One
        {
            get { return _one; }
        }

        /// <summary>Returns the unit vector for the x-axis.</summary>
        public static Vector2 UnitX
        {
            get { return _unitX; }
        }

        /// <summary>Returns the unit vector for the y-axis.</summary>
        public static Vector2 UnitY
        {
            get { return _unitY; }
        }
        #endregion // Statics

        #region Constructors
        /// <summary>Initializes a new instance of Vector2.</summary>
        /// <param name="x">Initial value for the x-component of the vector.</param>
        /// <param name="y">Initial value for the y-component of the vector.</param>
        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Creates a new instance of Vector2.</summary>
        /// <param name="value">Value to initialize both components to.</param>
        public Vector2(float value)
        {
            X = Y = value;
        }
        #endregion // Constructors

        #region Methods
        /// <summary>Retrieves a string representation of the current object.</summary>
        public override string ToString()
        {
            var currentCulture = CultureInfo.CurrentCulture;
            return string.Format(currentCulture, "{{X:{0} Y:{1}}}",
                new object[] { X.ToString(currentCulture), Y.ToString(currentCulture) });
        }

        /// <summary>Determines whether the specified Object is equal to the Vector2.</summary>
        /// <param name="other">The Object to compare with the current Vector2.</param>
        public bool Equals(Vector2 other)
        {
            return ((X == other.X) && (Y == other.Y));
        }

        /// <summary>Returns a value that indicates whether the current instance is equal to a specified object.</summary>
        /// <param name="obj">Object to make the comparison with.</param>
        public override bool Equals(object obj)
        {
            var flag = false;
            if (obj is Vector2)
                flag = Equals((Vector2)obj);
            return flag;
        }

        /// <summary>Gets the hash code of the vector object.</summary>
        public override int GetHashCode()
        {
            return (X.GetHashCode() + Y.GetHashCode());
        }

        /// <summary>Calculates the length of the vector.</summary>
        public float Length
        {
            get { return (float)Math.Sqrt((X * X) + (Y * Y)); }
        }

        /// <summary>
        /// Alias for Length property
        /// </summary>
        public float Magnitude
        {
            get { return Length; }
        }

        /// <summary>Calculates the length of the vector squared.</summary>
        public float LengthSquared()
        {
            return ((X * X) + (Y * Y));
        }

        /// <summary>Calculates the distance between two vectors.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static float Distance(Vector2 value1, Vector2 value2)
        {
            var num2 = value1.X - value2.X;
            var num = value1.Y - value2.Y;
            var num3 = (num2 * num2) + (num * num);
            return (float)Math.Sqrt(num3);
        }

        /// <summary>Calculates the distance between two vectors.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="result">[OutAttribute] The distance between the vectors.</param>
        public static void Distance(ref Vector2 value1, ref Vector2 value2, out float result)
        {
            var num2 = value1.X - value2.X;
            var num = value1.Y - value2.Y;
            var num3 = (num2 * num2) + (num * num);
            result = (float)Math.Sqrt(num3);
        }

        /// <summary>Calculates the distance between two vectors squared.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static float DistanceSquared(Vector2 value1, Vector2 value2)
        {
            var num2 = value1.X - value2.X;
            var num = value1.Y - value2.Y;
            return ((num2 * num2) + (num * num));
        }

        /// <summary>Calculates the distance between two vectors squared.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="result">[OutAttribute] The distance between the vectors squared.</param>
        public static void DistanceSquared(ref Vector2 value1, ref Vector2 value2, out float result)
        {
            var num2 = value1.X - value2.X;
            var num = value1.Y - value2.Y;
            result = (num2 * num2) + (num * num);
        }

        /// <summary>Calculates the dot product of two vectors. If the two vectors are unit vectors, the dot product returns a floating point value between -1 and 1 that can be used to determine some properties of the angle between two vectors. For example, it can show whether the vectors are orthogonal, parallel, or have an acute or obtuse angle between them.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static float Dot(Vector2 value1, Vector2 value2)
        {
            return ((value1.X * value2.X) + (value1.Y * value2.Y));
        }

        /// <summary>Calculates the dot product of two vectors and writes the result to a user-specified variable. If the two vectors are unit vectors, the dot product returns a floating point value between -1 and 1 that can be used to determine some properties of the angle between two vectors. For example, it can show whether the vectors are orthogonal, parallel, or have an acute or obtuse angle between them.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="result">[OutAttribute] The dot product of the two vectors.</param>
        public static void Dot(ref Vector2 value1, ref Vector2 value2, out float result)
        {
            result = (value1.X * value2.X) + (value1.Y * value2.Y);
        }

        /// <summary>
        /// Calculates the dot product of this and another vector.
        /// </summary>
        /// <param name="other">Other vector</param>
        /// <returns>float</returns>
        public float Dot(Vector2 other)
        {
            return Dot(this, other);
        }

        /// <summary>Turns the current vector into a unit vector. The result is a vector one unit in length pointing in the same direction as the original vector.</summary>
        public void Normalize()
        {
            var num2 = (X * X) + (Y * Y);
            var num = 1f / ((float)Math.Sqrt(num2));
            X *= num;
            Y *= num;
        }

        /// <summary>Creates a unit vector from the specified vector. The result is a vector one unit in length pointing in the same direction as the original vector.</summary>
        /// <param name="value">Source Vector2.</param>
        public static Vector2 Normalize(Vector2 value)
        {
            Vector2 vector;
            var num2 = (value.X * value.X) + (value.Y * value.Y);
            var num = 1f / ((float)Math.Sqrt(num2));
            vector.X = value.X * num;
            vector.Y = value.Y * num;
            return vector;
        }

        /// <summary>Creates a unit vector from the specified vector, writing the result to a user-specified variable. The result is a vector one unit in length pointing in the same direction as the original vector.</summary>
        /// <param name="value">Source vector.</param>
        /// <param name="result">[OutAttribute] Normalized vector.</param>
        public static void Normalize(ref Vector2 value, out Vector2 result)
        {
            var num2 = (value.X * value.X) + (value.Y * value.Y);
            var num = 1f / ((float)Math.Sqrt(num2));
            result.X = value.X * num;
            result.Y = value.Y * num;
        }

        /// <summary>Determines the reflect vector of the given vector and normal.</summary>
        /// <param name="vector">Source vector.</param>
        /// <param name="normal">Normal of vector.</param>
        public static Vector2 Reflect(Vector2 vector, Vector2 normal)
        {
            Vector2 vector2;
            var num = (vector.X * normal.X) + (vector.Y * normal.Y);
            vector2.X = vector.X - ((2f * num) * normal.X);
            vector2.Y = vector.Y - ((2f * num) * normal.Y);
            return vector2;
        }

        /// <summary>Determines the reflect vector of the given vector and normal.</summary>
        /// <param name="vector">Source vector.</param>
        /// <param name="normal">Normal of vector.</param>
        /// <param name="result">[OutAttribute] The created reflect vector.</param>
        public static void Reflect(ref Vector2 vector, ref Vector2 normal, out Vector2 result)
        {
            var num = (vector.X * normal.X) + (vector.Y * normal.Y);
            result.X = vector.X - ((2f * num) * normal.X);
            result.Y = vector.Y - ((2f * num) * normal.Y);
        }

        /// <summary>Returns a vector that contains the lowest value from each matching pair of components.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector2 Min(Vector2 value1, Vector2 value2)
        {
            Vector2 vector;
            vector.X = (value1.X < value2.X) ? value1.X : value2.X;
            vector.Y = (value1.Y < value2.Y) ? value1.Y : value2.Y;
            return vector;
        }

        /// <summary>Returns a vector that contains the lowest value from each matching pair of components.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="result">[OutAttribute] The minimized vector.</param>
        public static void Min(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
        {
            result.X = (value1.X < value2.X) ? value1.X : value2.X;
            result.Y = (value1.Y < value2.Y) ? value1.Y : value2.Y;
        }

        /// <summary>Returns a vector that contains the highest value from each matching pair of components.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector2 Max(Vector2 value1, Vector2 value2)
        {
            Vector2 vector;
            vector.X = (value1.X > value2.X) ? value1.X : value2.X;
            vector.Y = (value1.Y > value2.Y) ? value1.Y : value2.Y;
            return vector;
        }

        /// <summary>Returns a vector that contains the highest value from each matching pair of components.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="result">[OutAttribute] The maximized vector.</param>
        public static void Max(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
        {
            result.X = (value1.X > value2.X) ? value1.X : value2.X;
            result.Y = (value1.Y > value2.Y) ? value1.Y : value2.Y;
        }

        public Vector2 Ceiling()
        {
            return new Vector2((float)System.Math.Ceiling(X), (float)System.Math.Ceiling(Y));
        }

        public Vector2 Floor()
        {
            return new Vector2((float)System.Math.Floor(X), (float)System.Math.Floor(Y));
        }

        public Vector2 Abs()
        {
            return new Vector2(System.Math.Abs(X), System.Math.Abs(Y));
        }

        /// <summary>Restricts a value to be within a specified range.</summary>
        /// <param name="value1">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        public static Vector2 Clamp(Vector2 value1, Vector2 min, Vector2 max)
        {
            Vector2 vector;
            var x = value1.X;
            x = (x > max.X) ? max.X : x;
            x = (x < min.X) ? min.X : x;
            var y = value1.Y;
            y = (y > max.Y) ? max.Y : y;
            y = (y < min.Y) ? min.Y : y;
            vector.X = x;
            vector.Y = y;
            return vector;
        }

        /// <summary>Restricts a value to be within a specified range.</summary>
        /// <param name="value1">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="result">[OutAttribute] The clamped value.</param>
        public static void Clamp(ref Vector2 value1, ref Vector2 min, ref Vector2 max, out Vector2 result)
        {
            var x = value1.X;
            x = (x > max.X) ? max.X : x;
            x = (x < min.X) ? min.X : x;
            var y = value1.Y;
            y = (y > max.Y) ? max.Y : y;
            y = (y < min.Y) ? min.Y : y;
            result.X = x;
            result.Y = y;
        }

        /// <summary>Performs a linear interpolation between two vectors.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of value2.</param>
        public static Vector2 Lerp(Vector2 value1, Vector2 value2, float amount)
        {
            Vector2 vector;
            vector.X = value1.X + ((value2.X - value1.X) * amount);
            vector.Y = value1.Y + ((value2.Y - value1.Y) * amount);
            return vector;
        }

        /// <summary>Performs a linear interpolation between two vectors.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of value2.</param>
        /// <param name="result">[OutAttribute] The result of the interpolation.</param>
        public static void Lerp(ref Vector2 value1, ref Vector2 value2, float amount, out Vector2 result)
        {
            result.X = value1.X + ((value2.X - value1.X) * amount);
            result.Y = value1.Y + ((value2.Y - value1.Y) * amount);
        }

        /// <summary>Returns a Vector2 containing the 2D Cartesian coordinates of a point specified in barycentric (areal) coordinates relative to a 2D triangle.</summary>
        /// <param name="value1">A Vector2 containing the 2D Cartesian coordinates of vertex 1 of the triangle.</param>
        /// <param name="value2">A Vector2 containing the 2D Cartesian coordinates of vertex 2 of the triangle.</param>
        /// <param name="value3">A Vector2 containing the 2D Cartesian coordinates of vertex 3 of the triangle.</param>
        /// <param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in value2).</param>
        /// <param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in value3).</param>
        public static Vector2 Barycentric(Vector2 value1, Vector2 value2, Vector2 value3, float amount1, float amount2)
        {
            Vector2 vector;
            vector.X = (value1.X + (amount1 * (value2.X - value1.X))) + (amount2 * (value3.X - value1.X));
            vector.Y = (value1.Y + (amount1 * (value2.Y - value1.Y))) + (amount2 * (value3.Y - value1.Y));
            return vector;
        }

        /// <summary>Returns a Vector2 containing the 2D Cartesian coordinates of a point specified in barycentric (areal) coordinates relative to a 2D triangle.</summary>
        /// <param name="value1">A Vector2 containing the 2D Cartesian coordinates of vertex 1 of the triangle.</param>
        /// <param name="value2">A Vector2 containing the 2D Cartesian coordinates of vertex 2 of the triangle.</param>
        /// <param name="value3">A Vector2 containing the 2D Cartesian coordinates of vertex 3 of the triangle.</param>
        /// <param name="amount1">Barycentric coordinate b2, which expresses the weighting factor toward vertex 2 (specified in value2).</param>
        /// <param name="amount2">Barycentric coordinate b3, which expresses the weighting factor toward vertex 3 (specified in value3).</param>
        /// <param name="result">[OutAttribute] The 2D Cartesian coordinates of the specified point are placed in this Vector2 on exit.</param>
        public static void Barycentric(ref Vector2 value1, ref Vector2 value2, ref Vector2 value3, float amount1, float amount2,
                                       out Vector2 result)
        {
            result.X = (value1.X + (amount1 * (value2.X - value1.X))) + (amount2 * (value3.X - value1.X));
            result.Y = (value1.Y + (amount1 * (value2.Y - value1.Y))) + (amount2 * (value3.Y - value1.Y));
        }

        /// <summary>Interpolates between two values using a cubic equation.</summary>
        /// <param name="value1">Source value.</param>
        /// <param name="value2">Source value.</param>
        /// <param name="amount">Weighting value.</param>
        public static Vector2 SmoothStep(Vector2 value1, Vector2 value2, float amount)
        {
            Vector2 vector;
            amount = (amount > 1f) ? 1f : ((amount < 0f) ? 0f : amount);
            amount = (amount * amount) * (3f - (2f * amount));
            vector.X = value1.X + ((value2.X - value1.X) * amount);
            vector.Y = value1.Y + ((value2.Y - value1.Y) * amount);
            return vector;
        }

        /// <summary>Interpolates between two values using a cubic equation.</summary>
        /// <param name="value1">Source value.</param>
        /// <param name="value2">Source value.</param>
        /// <param name="amount">Weighting value.</param>
        /// <param name="result">[OutAttribute] The interpolated value.</param>
        public static void SmoothStep(ref Vector2 value1, ref Vector2 value2, float amount, out Vector2 result)
        {
            amount = (amount > 1f) ? 1f : ((amount < 0f) ? 0f : amount);
            amount = (amount * amount) * (3f - (2f * amount));
            result.X = value1.X + ((value2.X - value1.X) * amount);
            result.Y = value1.Y + ((value2.Y - value1.Y) * amount);
        }

        /// <summary>Performs a Catmull-Rom interpolation using the specified positions.</summary>
        /// <param name="value1">The first position in the interpolation.</param>
        /// <param name="value2">The second position in the interpolation.</param>
        /// <param name="value3">The third position in the interpolation.</param>
        /// <param name="value4">The fourth position in the interpolation.</param>
        /// <param name="amount">Weighting factor.</param>
        public static Vector2 CatmullRom(Vector2 value1, Vector2 value2, Vector2 value3, Vector2 value4, float amount)
        {
            Vector2 vector;
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
            return vector;
        }

        /// <summary>Performs a Catmull-Rom interpolation using the specified positions.</summary>
        /// <param name="value1">The first position in the interpolation.</param>
        /// <param name="value2">The second position in the interpolation.</param>
        /// <param name="value3">The third position in the interpolation.</param>
        /// <param name="value4">The fourth position in the interpolation.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <param name="result">[OutAttribute] A vector that is the result of the Catmull-Rom interpolation.</param>
        public static void CatmullRom(ref Vector2 value1, ref Vector2 value2, ref Vector2 value3, ref Vector2 value4, float amount,
                                      out Vector2 result)
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
        }

        /// <summary>Performs a Hermite spline interpolation.</summary>
        /// <param name="value1">Source position vector.</param>
        /// <param name="tangent1">Source tangent vector.</param>
        /// <param name="value2">Source position vector.</param>
        /// <param name="tangent2">Source tangent vector.</param>
        /// <param name="amount">Weighting factor.</param>
        public static Vector2 Hermite(Vector2 value1, Vector2 tangent1, Vector2 value2, Vector2 tangent2, float amount)
        {
            Vector2 vector;
            var num = amount * amount;
            var num2 = amount * num;
            var num6 = ((2f * num2) - (3f * num)) + 1f;
            var num5 = (-2f * num2) + (3f * num);
            var num4 = (num2 - (2f * num)) + amount;
            var num3 = num2 - num;
            vector.X = (((value1.X * num6) + (value2.X * num5)) + (tangent1.X * num4)) + (tangent2.X * num3);
            vector.Y = (((value1.Y * num6) + (value2.Y * num5)) + (tangent1.Y * num4)) + (tangent2.Y * num3);
            return vector;
        }

        /// <summary>Performs a Hermite spline interpolation.</summary>
        /// <param name="value1">Source position vector.</param>
        /// <param name="tangent1">Source tangent vector.</param>
        /// <param name="value2">Source position vector.</param>
        /// <param name="tangent2">Source tangent vector.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <param name="result">[OutAttribute] The result of the Hermite spline interpolation.</param>
        public static void Hermite(ref Vector2 value1, ref Vector2 tangent1, ref Vector2 value2, ref Vector2 tangent2,
                                   float amount, out Vector2 result)
        {
            var num = amount * amount;
            var num2 = amount * num;
            var num6 = ((2f * num2) - (3f * num)) + 1f;
            var num5 = (-2f * num2) + (3f * num);
            var num4 = (num2 - (2f * num)) + amount;
            var num3 = num2 - num;
            result.X = (((value1.X * num6) + (value2.X * num5)) + (tangent1.X * num4)) + (tangent2.X * num3);
            result.Y = (((value1.Y * num6) + (value2.Y * num5)) + (tangent1.Y * num4)) + (tangent2.Y * num3);
        }

        /// <summary>Returns a vector pointing in the opposite direction.</summary>
        /// <param name="value">Source vector.</param>
        public static Vector2 Negate(Vector2 value)
        {
            Vector2 vector;
            vector.X = -value.X;
            vector.Y = -value.Y;
            return vector;
        }

        /// <summary>Returns a vector pointing in the opposite direction.</summary>
        /// <param name="value">Source vector.</param>
        /// <param name="result">[OutAttribute] Vector pointing in the opposite direction.</param>
        public static void Negate(ref Vector2 value, out Vector2 result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
        }

        /// <summary>Adds two vectors.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector2 Add(Vector2 value1, Vector2 value2)
        {
            Vector2 vector;
            vector.X = value1.X + value2.X;
            vector.Y = value1.Y + value2.Y;
            return vector;
        }

        /// <summary>Adds two vectors.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="result">[OutAttribute] Sum of the source vectors.</param>
        public static void Add(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
        {
            result.X = value1.X + value2.X;
            result.Y = value1.Y + value2.Y;
        }

        /// <summary>Subtracts a vector from a vector.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector2 Subtract(Vector2 value1, Vector2 value2)
        {
            Vector2 vector;
            vector.X = value1.X - value2.X;
            vector.Y = value1.Y - value2.Y;
            return vector;
        }

        /// <summary>Subtracts a vector from a vector.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="result">[OutAttribute] The result of the subtraction.</param>
        public static void Subtract(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
        {
            result.X = value1.X - value2.X;
            result.Y = value1.Y - value2.Y;
        }

        /// <summary>Multiplies the components of two vectors by each other.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector2 Multiply(Vector2 value1, Vector2 value2)
        {
            Vector2 vector;
            vector.X = value1.X * value2.X;
            vector.Y = value1.Y * value2.Y;
            return vector;
        }

        /// <summary>Multiplies the components of two vectors by each other.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        /// <param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
        {
            result.X = value1.X * value2.X;
            result.Y = value1.Y * value2.Y;
        }

        /// <summary>Multiplies a vector by a scalar value.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        public static Vector2 Multiply(Vector2 value1, float scaleFactor)
        {
            Vector2 vector;
            vector.X = value1.X * scaleFactor;
            vector.Y = value1.Y * scaleFactor;
            return vector;
        }

        /// <summary>Multiplies a vector by a scalar value.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        /// <param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Vector2 value1, float scaleFactor, out Vector2 result)
        {
            result.X = value1.X * scaleFactor;
            result.Y = value1.Y * scaleFactor;
        }

        /// <summary>Divides the components of a vector by the components of another vector.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Divisor vector.</param>
        public static Vector2 Divide(Vector2 value1, Vector2 value2)
        {
            Vector2 vector;
            vector.X = value1.X / value2.X;
            vector.Y = value1.Y / value2.Y;
            return vector;
        }

        /// <summary>Divides the components of a vector by the components of another vector.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">The divisor.</param>
        /// <param name="result">[OutAttribute] The result of the division.</param>
        public static void Divide(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
        {
            result.X = value1.X / value2.X;
            result.Y = value1.Y / value2.Y;
        }

        /// <summary>Divides a vector by a scalar value.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="divider">The divisor.</param>
        public static Vector2 Divide(Vector2 value1, float divider)
        {
            Vector2 vector;
            var num = 1f / divider;
            vector.X = value1.X * num;
            vector.Y = value1.Y * num;
            return vector;
        }

        /// <summary>Divides a vector by a scalar value.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="divider">The divisor.</param>
        /// <param name="result">[OutAttribute] The result of the division.</param>
        public static void Divide(ref Vector2 value1, float divider, out Vector2 result)
        {
            var num = 1f / divider;
            result.X = value1.X * num;
            result.Y = value1.Y * num;
        }

        /// <summary>Returns a vector pointing in the opposite direction.</summary>
        /// <param name="value">Source vector.</param>
        public static Vector2 operator -(Vector2 value)
        {
            Vector2 vector;
            vector.X = -value.X;
            vector.Y = -value.Y;
            return vector;
        }

        /// <summary>Tests vectors for equality.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static bool operator ==(Vector2 value1, Vector2 value2)
        {
            return ((value1.X == value2.X) && (value1.Y == value2.Y));
        }

        /// <summary>Tests vectors for inequality.</summary>
        /// <param name="value1">Vector to compare.</param>
        /// <param name="value2">Vector to compare.</param>
        public static bool operator !=(Vector2 value1, Vector2 value2)
        {
            if (value1.X == value2.X)
                return (value1.Y != value2.Y);
            return true;
        }

        /// <summary>Adds two vectors.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector2 operator +(Vector2 value1, Vector2 value2)
        {
            Vector2 vector;
            vector.X = value1.X + value2.X;
            vector.Y = value1.Y + value2.Y;
            return vector;
        }

        /// <summary>Subtracts a vector from a vector.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">source vector.</param>
        public static Vector2 operator -(Vector2 value1, Vector2 value2)
        {
            Vector2 vector;
            vector.X = value1.X - value2.X;
            vector.Y = value1.Y - value2.Y;
            return vector;
        }

        /// <summary>Multiplies the components of two vectors by each other.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Source vector.</param>
        public static Vector2 operator *(Vector2 value1, Vector2 value2)
        {
            Vector2 vector;
            vector.X = value1.X * value2.X;
            vector.Y = value1.Y * value2.Y;
            return vector;
        }

        /// <summary>Multiplies a vector by a scalar value.</summary>
        /// <param name="value">Source vector.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        public static Vector2 operator *(Vector2 value, float scaleFactor)
        {
            Vector2 vector;
            vector.X = value.X * scaleFactor;
            vector.Y = value.Y * scaleFactor;
            return vector;
        }

        /// <summary>Multiplies a vector by a scalar value.</summary>
        /// <param name="scaleFactor">Scalar value.</param>
        /// <param name="value">Source vector.</param>
        public static Vector2 operator *(float scaleFactor, Vector2 value)
        {
            Vector2 vector;
            vector.X = value.X * scaleFactor;
            vector.Y = value.Y * scaleFactor;
            return vector;
        }

        /// <summary>Divides the components of a vector by the components of another vector.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="value2">Divisor vector.</param>
        public static Vector2 operator /(Vector2 value1, Vector2 value2)
        {
            Vector2 vector;
            vector.X = value1.X / value2.X;
            vector.Y = value1.Y / value2.Y;
            return vector;
        }

        /// <summary>Divides a vector by a scalar value.</summary>
        /// <param name="value1">Source vector.</param>
        /// <param name="divider">The divisor.</param>
        public static Vector2 operator /(Vector2 value1, float divider)
        {
            Vector2 vector;
            var num = 1f / divider;
            vector.X = value1.X * num;
            vector.Y = value1.Y * num;
            return vector;
        }

        /// <summary>
        /// Operator to convert a 3d vector into a 2d vector. 
        /// </summary>
        /// <param name="vector">3D Vector</param>
        /// <returns>A new 2D vector.</returns>
        public static explicit operator Vector2(Vector3 vector)
        {
            return new Vector2(vector.X, vector.Y);
        }

        /// <summary>
        /// Operator to convert a 4d vector into a 2d vector
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static explicit operator Vector2(Vector4 vector)
        {
            return new Vector2(vector.X, vector.Y);
        }

        /// <summary>
        /// Operator to convert a 2d vector into a 3d vector
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static implicit operator Vector3(Vector2 vector)
        {
            return new Vector3(vector.X, vector.Y, 0);
        }

        /// <summary>
        /// Operator to convert a 2d vector into a 4d vector
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static implicit operator Vector4(Vector2 vector)
        {
            return new Vector4(vector.X, vector.Y, 0, 0);
        }

        /// <summary>
        /// Operator to convert a System.Drawing.Point into a 2d vector
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static implicit operator Vector2(System.Drawing.Point point)
        {
            return new Vector2(point.X, point.Y);
        }

        /// <summary>
        /// Operator to convert a System.Drawing.PointF into a 2d vector
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static implicit operator Vector2(System.Drawing.PointF point)
        {
            return new Vector2(point.X, point.Y);
        }

        /// <summary>
        /// Operator to convert a System.Drawing.Size into a 2d vector
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static implicit operator Vector2(System.Drawing.Size point)
        {
            return new Vector2(point.Width, point.Height);
        }

        /// <summary>
        /// Operator to convert a System.Drawing.SizeF into a 2d vector
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static implicit operator Vector2(System.Drawing.SizeF point)
        {
            return new Vector2(point.Width, point.Height);
        }

        /// <summary>
        /// Operator to convert a 2d vector into a System.Drawing.Point
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static explicit operator System.Drawing.Point(Vector2 vector)
        {
            return new System.Drawing.Point((int)vector.X, (int)vector.Y);
        }

        /// <summary>
        /// Operator to convert a 2d vector into a System.Drawing.PointF
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static implicit operator System.Drawing.PointF(Vector2 vector)
        {
            return new System.Drawing.PointF(vector.X, vector.Y);
        }

        /// <summary>
        /// Operator to convert a 2d vector into a System.Drawing.Size
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static explicit operator System.Drawing.Size(Vector2 vector)
        {
            return new System.Drawing.Size((int)vector.X, (int)vector.Y);
        }

        /// <summary>
        /// Operator to convert a 2d vector into a System.Drawing.SizeF
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static implicit operator System.Drawing.SizeF(Vector2 vector)
        {
            return new System.Drawing.SizeF(vector.X, vector.Y);
        }

        /// <summary>
        /// Operator to convert a 2d vector into an SFML 2d vector
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static implicit operator SVector2f(Vector2 vector)
        {
            return new SVector2f(vector.X, vector.Y);
        }
        /// <summary>
        /// Operator to convert an SFML 2d vector into a 2d vector
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static implicit operator Vector2(SVector2f vector)
        {
            return new Vector2(vector.X, vector.Y);
        }

        private const string ZERO_VECTOR_ANGLE = "Cannot find an angle from a zero vector.";

        /// <summary>
        /// Find the angle in radians between two vectors
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns>float angle in radians</returns>
        public static float Angle(Vector2 v1, Vector2 v2)
        {
            if (v1.Magnitude == 0 || v2.Magnitude == 0)
                throw new ArgumentException(ZERO_VECTOR_ANGLE, "this");
            return
                (
                    (float)Math.Acos
                                (
                                    Normalize(v1).Dot(Normalize(v2))
                                )
                );
        }

        public float Angle(Vector2 other)
        {
            return Angle(this, other);
        }


        #endregion // Methods
    }
}