using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using SFML.Graphics.Design;

namespace SFML.Graphics
{
    /// <summary>Defines a matrix.</summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [TypeConverter(typeof(MatrixConverter))]
    public struct Matrix : IEquatable<Matrix>
    {
        /// <summary>Value at row 1 column 1 of the matrix.</summary>
        public float M11;

        /// <summary>Value at row 1 column 2 of the matrix.</summary>
        public float M12;

        /// <summary>Value at row 1 column 3 of the matrix.</summary>
        public float M13;

        /// <summary>Value at row 1 column 4 of the matrix.</summary>
        public float M14;

        /// <summary>Value at row 2 column 1 of the matrix.</summary>
        public float M21;

        /// <summary>Value at row 2 column 2 of the matrix.</summary>
        public float M22;

        /// <summary>Value at row 2 column 3 of the matrix.</summary>
        public float M23;

        /// <summary>Value at row 2 column 4 of the matrix.</summary>
        public float M24;

        /// <summary>Value at row 3 column 1 of the matrix.</summary>
        public float M31;

        /// <summary>Value at row 3 column 2 of the matrix.</summary>
        public float M32;

        /// <summary>Value at row 3 column 3 of the matrix.</summary>
        public float M33;

        /// <summary>Value at row 3 column 4 of the matrix.</summary>
        public float M34;

        /// <summary>Value at row 4 column 1 of the matrix.</summary>
        public float M41;

        /// <summary>Value at row 4 column 2 of the matrix.</summary>
        public float M42;

        /// <summary>Value at row 4 column 3 of the matrix.</summary>
        public float M43;

        /// <summary>Value at row 4 column 4 of the matrix.</summary>
        public float M44;

        static readonly Matrix _identity;

        /// <summary>Returns an instance of the identity matrix.</summary>
        public static Matrix Identity
        {
            get { return _identity; }
        }

        /// <summary>Gets and sets the up vector of the Matrix.</summary>
        public Vector3 Up
        {
            get
            {
                Vector3 vector;
                vector.X = M21;
                vector.Y = M22;
                vector.Z = M23;
                return vector;
            }
            set
            {
                M21 = value.X;
                M22 = value.Y;
                M23 = value.Z;
            }
        }

        /// <summary>Gets and sets the down vector of the Matrix.</summary>
        public Vector3 Down
        {
            get
            {
                Vector3 vector;
                vector.X = -M21;
                vector.Y = -M22;
                vector.Z = -M23;
                return vector;
            }
            set
            {
                M21 = -value.X;
                M22 = -value.Y;
                M23 = -value.Z;
            }
        }

        /// <summary>Gets and sets the right vector of the Matrix.</summary>
        public Vector3 Right
        {
            get
            {
                Vector3 vector;
                vector.X = M11;
                vector.Y = M12;
                vector.Z = M13;
                return vector;
            }
            set
            {
                M11 = value.X;
                M12 = value.Y;
                M13 = value.Z;
            }
        }

        /// <summary>Gets and sets the left vector of the Matrix.</summary>
        public Vector3 Left
        {
            get
            {
                Vector3 vector;
                vector.X = -M11;
                vector.Y = -M12;
                vector.Z = -M13;
                return vector;
            }
            set
            {
                M11 = -value.X;
                M12 = -value.Y;
                M13 = -value.Z;
            }
        }

        /// <summary>Gets and sets the forward vector of the Matrix.</summary>
        public Vector3 Forward
        {
            get
            {
                Vector3 vector;
                vector.X = -M31;
                vector.Y = -M32;
                vector.Z = -M33;
                return vector;
            }
            set
            {
                M31 = -value.X;
                M32 = -value.Y;
                M33 = -value.Z;
            }
        }

        /// <summary>Gets and sets the backward vector of the Matrix.</summary>
        public Vector3 Backward
        {
            get
            {
                Vector3 vector;
                vector.X = M31;
                vector.Y = M32;
                vector.Z = M33;
                return vector;
            }
            set
            {
                M31 = value.X;
                M32 = value.Y;
                M33 = value.Z;
            }
        }

        /// <summary>Gets and sets the translation vector of the Matrix.</summary>
        public Vector3 Translation
        {
            get
            {
                Vector3 vector;
                vector.X = M41;
                vector.Y = M42;
                vector.Z = M43;
                return vector;
            }
            set
            {
                M41 = value.X;
                M42 = value.Y;
                M43 = value.Z;
            }
        }

        /// <summary>Initializes a new instance of Matrix.</summary>
        /// <param name="m11">Value to initialize m11 to.</param>
        /// <param name="m12">Value to initialize m12 to.</param>
        /// <param name="m13">Value to initialize m13 to.</param>
        /// <param name="m14">Value to initialize m14 to.</param>
        /// <param name="m21">Value to initialize m21 to.</param>
        /// <param name="m22">Value to initialize m22 to.</param>
        /// <param name="m23">Value to initialize m23 to.</param>
        /// <param name="m24">Value to initialize m24 to.</param>
        /// <param name="m31">Value to initialize m31 to.</param>
        /// <param name="m32">Value to initialize m32 to.</param>
        /// <param name="m33">Value to initialize m33 to.</param>
        /// <param name="m34">Value to initialize m34 to.</param>
        /// <param name="m41">Value to initialize m41 to.</param>
        /// <param name="m42">Value to initialize m42 to.</param>
        /// <param name="m43">Value to initialize m43 to.</param>
        /// <param name="m44">Value to initialize m44 to.</param>
        [SuppressMessage("Microsoft.Design", "CA1025:ReplaceRepetitiveArgumentsWithParamsArray")]
        public Matrix(float m11, float m12, float m13, float m14, float m21, float m22, float m23, float m24, float m31, float m32,
                      float m33, float m34, float m41, float m42, float m43, float m44)
        {
            M11 = m11;
            M12 = m12;
            M13 = m13;
            M14 = m14;
            M21 = m21;
            M22 = m22;
            M23 = m23;
            M24 = m24;
            M31 = m31;
            M32 = m32;
            M33 = m33;
            M34 = m34;
            M41 = m41;
            M42 = m42;
            M43 = m43;
            M44 = m44;
        }

        /// <summary>Creates a spherical billboard that rotates around a specified object position.</summary>
        /// <param name="objectPosition">Position of the object the billboard will rotate around.</param>
        /// <param name="cameraPosition">Position of the camera.</param>
        /// <param name="cameraUpVector">The up vector of the camera.</param>
        /// <param name="cameraForwardVector">Optional forward vector of the camera.</param>
        public static Matrix CreateBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 cameraUpVector,
                                             Vector3? cameraForwardVector)
        {
            Matrix matrix;
            Vector3 vector;
            Vector3 vector2;
            Vector3 vector3;
            vector.X = objectPosition.X - cameraPosition.X;
            vector.Y = objectPosition.Y - cameraPosition.Y;
            vector.Z = objectPosition.Z - cameraPosition.Z;
            var num = vector.LengthSquared();
            if (num < 0.0001f)
                vector = cameraForwardVector.HasValue ? -(cameraForwardVector.Value) : Vector3.Forward;
            else
                Vector3.Multiply(ref vector, (1f / ((float)Math.Sqrt(num))), out vector);
            Vector3.Cross(ref cameraUpVector, ref vector, out vector3);
            vector3.Normalize();
            Vector3.Cross(ref vector, ref vector3, out vector2);
            matrix.M11 = vector3.X;
            matrix.M12 = vector3.Y;
            matrix.M13 = vector3.Z;
            matrix.M14 = 0f;
            matrix.M21 = vector2.X;
            matrix.M22 = vector2.Y;
            matrix.M23 = vector2.Z;
            matrix.M24 = 0f;
            matrix.M31 = vector.X;
            matrix.M32 = vector.Y;
            matrix.M33 = vector.Z;
            matrix.M34 = 0f;
            matrix.M41 = objectPosition.X;
            matrix.M42 = objectPosition.Y;
            matrix.M43 = objectPosition.Z;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>Creates a spherical billboard that rotates around a specified object position.</summary>
        /// <param name="objectPosition">Position of the object the billboard will rotate around.</param>
        /// <param name="cameraPosition">Position of the camera.</param>
        /// <param name="cameraUpVector">The up vector of the camera.</param>
        /// <param name="cameraForwardVector">Optional forward vector of the camera.</param>
        /// <param name="result">[OutAttribute] The created billboard matrix.</param>
        public static void CreateBillboard(ref Vector3 objectPosition, ref Vector3 cameraPosition, ref Vector3 cameraUpVector,
                                           Vector3? cameraForwardVector, out Matrix result)
        {
            Vector3 vector;
            Vector3 vector2;
            Vector3 vector3;
            vector.X = objectPosition.X - cameraPosition.X;
            vector.Y = objectPosition.Y - cameraPosition.Y;
            vector.Z = objectPosition.Z - cameraPosition.Z;
            var num = vector.LengthSquared();
            if (num < 0.0001f)
                vector = cameraForwardVector.HasValue ? -(cameraForwardVector.Value) : Vector3.Forward;
            else
                Vector3.Multiply(ref vector, (1f / ((float)Math.Sqrt(num))), out vector);
            Vector3.Cross(ref cameraUpVector, ref vector, out vector3);
            vector3.Normalize();
            Vector3.Cross(ref vector, ref vector3, out vector2);
            result.M11 = vector3.X;
            result.M12 = vector3.Y;
            result.M13 = vector3.Z;
            result.M14 = 0f;
            result.M21 = vector2.X;
            result.M22 = vector2.Y;
            result.M23 = vector2.Z;
            result.M24 = 0f;
            result.M31 = vector.X;
            result.M32 = vector.Y;
            result.M33 = vector.Z;
            result.M34 = 0f;
            result.M41 = objectPosition.X;
            result.M42 = objectPosition.Y;
            result.M43 = objectPosition.Z;
            result.M44 = 1f;
        }

        /// <summary>Creates a cylindrical billboard that rotates around a specified axis.</summary>
        /// <param name="objectPosition">Position of the object the billboard will rotate around.</param>
        /// <param name="cameraPosition">Position of the camera.</param>
        /// <param name="rotateAxis">Axis to rotate the billboard around.</param>
        /// <param name="cameraForwardVector">Optional forward vector of the camera.</param>
        /// <param name="objectForwardVector">Optional forward vector of the object.</param>
        public static Matrix CreateConstrainedBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 rotateAxis,
                                                        Vector3? cameraForwardVector, Vector3? objectForwardVector)
        {
            float num;
            Vector3 vector;
            Matrix matrix;
            Vector3 vector2;
            Vector3 vector3;
            vector2.X = objectPosition.X - cameraPosition.X;
            vector2.Y = objectPosition.Y - cameraPosition.Y;
            vector2.Z = objectPosition.Z - cameraPosition.Z;
            var num2 = vector2.LengthSquared();
            if (num2 < 0.0001f)
                vector2 = cameraForwardVector.HasValue ? -(cameraForwardVector.Value) : Vector3.Forward;
            else
                Vector3.Multiply(ref vector2, (1f / ((float)Math.Sqrt(num2))), out vector2);
            var vector4 = rotateAxis;
            Vector3.Dot(ref rotateAxis, ref vector2, out num);
            if (Math.Abs(num) > 0.9982547f)
            {
                if (objectForwardVector.HasValue)
                {
                    vector = objectForwardVector.Value;
                    Vector3.Dot(ref rotateAxis, ref vector, out num);
                    if (Math.Abs(num) > 0.9982547f)
                    {
                        num = ((rotateAxis.X * Vector3.Forward.X) + (rotateAxis.Y * Vector3.Forward.Y)) +
                              (rotateAxis.Z * Vector3.Forward.Z);
                        vector = (Math.Abs(num) > 0.9982547f) ? Vector3.Right : Vector3.Forward;
                    }
                }
                else
                {
                    num = ((rotateAxis.X * Vector3.Forward.X) + (rotateAxis.Y * Vector3.Forward.Y)) +
                          (rotateAxis.Z * Vector3.Forward.Z);
                    vector = (Math.Abs(num) > 0.9982547f) ? Vector3.Right : Vector3.Forward;
                }
                Vector3.Cross(ref rotateAxis, ref vector, out vector3);
                vector3.Normalize();
                Vector3.Cross(ref vector3, ref rotateAxis, out vector);
                vector.Normalize();
            }
            else
            {
                Vector3.Cross(ref rotateAxis, ref vector2, out vector3);
                vector3.Normalize();
                Vector3.Cross(ref vector3, ref vector4, out vector);
                vector.Normalize();
            }
            matrix.M11 = vector3.X;
            matrix.M12 = vector3.Y;
            matrix.M13 = vector3.Z;
            matrix.M14 = 0f;
            matrix.M21 = vector4.X;
            matrix.M22 = vector4.Y;
            matrix.M23 = vector4.Z;
            matrix.M24 = 0f;
            matrix.M31 = vector.X;
            matrix.M32 = vector.Y;
            matrix.M33 = vector.Z;
            matrix.M34 = 0f;
            matrix.M41 = objectPosition.X;
            matrix.M42 = objectPosition.Y;
            matrix.M43 = objectPosition.Z;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>Creates a cylindrical billboard that rotates around a specified axis.</summary>
        /// <param name="objectPosition">Position of the object the billboard will rotate around.</param>
        /// <param name="cameraPosition">Position of the camera.</param>
        /// <param name="rotateAxis">Axis to rotate the billboard around.</param>
        /// <param name="cameraForwardVector">Optional forward vector of the camera.</param>
        /// <param name="objectForwardVector">Optional forward vector of the object.</param>
        /// <param name="result">[OutAttribute] The created billboard matrix.</param>
        public static void CreateConstrainedBillboard(ref Vector3 objectPosition, ref Vector3 cameraPosition,
                                                      ref Vector3 rotateAxis, Vector3? cameraForwardVector,
                                                      Vector3? objectForwardVector, out Matrix result)
        {
            float num;
            Vector3 vector;
            Vector3 vector2;
            Vector3 vector3;
            vector2.X = objectPosition.X - cameraPosition.X;
            vector2.Y = objectPosition.Y - cameraPosition.Y;
            vector2.Z = objectPosition.Z - cameraPosition.Z;
            var num2 = vector2.LengthSquared();
            if (num2 < 0.0001f)
                vector2 = cameraForwardVector.HasValue ? -(cameraForwardVector.Value) : Vector3.Forward;
            else
                Vector3.Multiply(ref vector2, (1f / ((float)Math.Sqrt(num2))), out vector2);
            var vector4 = rotateAxis;
            Vector3.Dot(ref rotateAxis, ref vector2, out num);
            if (Math.Abs(num) > 0.9982547f)
            {
                if (objectForwardVector.HasValue)
                {
                    vector = objectForwardVector.Value;
                    Vector3.Dot(ref rotateAxis, ref vector, out num);
                    if (Math.Abs(num) > 0.9982547f)
                    {
                        num = ((rotateAxis.X * Vector3.Forward.X) + (rotateAxis.Y * Vector3.Forward.Y)) +
                              (rotateAxis.Z * Vector3.Forward.Z);
                        vector = (Math.Abs(num) > 0.9982547f) ? Vector3.Right : Vector3.Forward;
                    }
                }
                else
                {
                    num = ((rotateAxis.X * Vector3.Forward.X) + (rotateAxis.Y * Vector3.Forward.Y)) +
                          (rotateAxis.Z * Vector3.Forward.Z);
                    vector = (Math.Abs(num) > 0.9982547f) ? Vector3.Right : Vector3.Forward;
                }
                Vector3.Cross(ref rotateAxis, ref vector, out vector3);
                vector3.Normalize();
                Vector3.Cross(ref vector3, ref rotateAxis, out vector);
                vector.Normalize();
            }
            else
            {
                Vector3.Cross(ref rotateAxis, ref vector2, out vector3);
                vector3.Normalize();
                Vector3.Cross(ref vector3, ref vector4, out vector);
                vector.Normalize();
            }
            result.M11 = vector3.X;
            result.M12 = vector3.Y;
            result.M13 = vector3.Z;
            result.M14 = 0f;
            result.M21 = vector4.X;
            result.M22 = vector4.Y;
            result.M23 = vector4.Z;
            result.M24 = 0f;
            result.M31 = vector.X;
            result.M32 = vector.Y;
            result.M33 = vector.Z;
            result.M34 = 0f;
            result.M41 = objectPosition.X;
            result.M42 = objectPosition.Y;
            result.M43 = objectPosition.Z;
            result.M44 = 1f;
        }

        /// <summary>Creates a translation Matrix. Reference page contains links to related code samples.</summary>
        /// <param name="position">Amounts to translate by on the x, y, and z axes.</param>
        public static Matrix CreateTranslation(Vector3 position)
        {
            Matrix matrix;
            matrix.M11 = 1f;
            matrix.M12 = 0f;
            matrix.M13 = 0f;
            matrix.M14 = 0f;
            matrix.M21 = 0f;
            matrix.M22 = 1f;
            matrix.M23 = 0f;
            matrix.M24 = 0f;
            matrix.M31 = 0f;
            matrix.M32 = 0f;
            matrix.M33 = 1f;
            matrix.M34 = 0f;
            matrix.M41 = position.X;
            matrix.M42 = position.Y;
            matrix.M43 = position.Z;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>Creates a translation Matrix. Reference page contains links to related code samples.</summary>
        /// <param name="position">Amounts to translate by on the x, y, and z axes.</param>
        /// <param name="result">[OutAttribute] The created translation Matrix.</param>
        public static void CreateTranslation(ref Vector3 position, out Matrix result)
        {
            result.M11 = 1f;
            result.M12 = 0f;
            result.M13 = 0f;
            result.M14 = 0f;
            result.M21 = 0f;
            result.M22 = 1f;
            result.M23 = 0f;
            result.M24 = 0f;
            result.M31 = 0f;
            result.M32 = 0f;
            result.M33 = 1f;
            result.M34 = 0f;
            result.M41 = position.X;
            result.M42 = position.Y;
            result.M43 = position.Z;
            result.M44 = 1f;
        }

        /// <summary>Creates a translation Matrix. Reference page contains links to related code samples.</summary>
        /// <param name="xPosition">Value to translate by on the x-axis.</param>
        /// <param name="yPosition">Value to translate by on the y-axis.</param>
        /// <param name="zPosition">Value to translate by on the z-axis.</param>
        public static Matrix CreateTranslation(float xPosition, float yPosition, float zPosition)
        {
            Matrix matrix;
            matrix.M11 = 1f;
            matrix.M12 = 0f;
            matrix.M13 = 0f;
            matrix.M14 = 0f;
            matrix.M21 = 0f;
            matrix.M22 = 1f;
            matrix.M23 = 0f;
            matrix.M24 = 0f;
            matrix.M31 = 0f;
            matrix.M32 = 0f;
            matrix.M33 = 1f;
            matrix.M34 = 0f;
            matrix.M41 = xPosition;
            matrix.M42 = yPosition;
            matrix.M43 = zPosition;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>Creates a translation Matrix. Reference page contains links to related code samples.</summary>
        /// <param name="xPosition">Value to translate by on the x-axis.</param>
        /// <param name="yPosition">Value to translate by on the y-axis.</param>
        /// <param name="zPosition">Value to translate by on the z-axis.</param>
        /// <param name="result">[OutAttribute] The created translation Matrix.</param>
        public static void CreateTranslation(float xPosition, float yPosition, float zPosition, out Matrix result)
        {
            result.M11 = 1f;
            result.M12 = 0f;
            result.M13 = 0f;
            result.M14 = 0f;
            result.M21 = 0f;
            result.M22 = 1f;
            result.M23 = 0f;
            result.M24 = 0f;
            result.M31 = 0f;
            result.M32 = 0f;
            result.M33 = 1f;
            result.M34 = 0f;
            result.M41 = xPosition;
            result.M42 = yPosition;
            result.M43 = zPosition;
            result.M44 = 1f;
        }

        /// <summary>Creates a scaling Matrix.</summary>
        /// <param name="xScale">Value to scale by on the x-axis.</param>
        /// <param name="yScale">Value to scale by on the y-axis.</param>
        /// <param name="zScale">Value to scale by on the z-axis.</param>
        public static Matrix CreateScale(float xScale, float yScale, float zScale)
        {
            Matrix matrix;
            var num3 = xScale;
            var num2 = yScale;
            var num = zScale;
            matrix.M11 = num3;
            matrix.M12 = 0f;
            matrix.M13 = 0f;
            matrix.M14 = 0f;
            matrix.M21 = 0f;
            matrix.M22 = num2;
            matrix.M23 = 0f;
            matrix.M24 = 0f;
            matrix.M31 = 0f;
            matrix.M32 = 0f;
            matrix.M33 = num;
            matrix.M34 = 0f;
            matrix.M41 = 0f;
            matrix.M42 = 0f;
            matrix.M43 = 0f;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>Creates a scaling Matrix.</summary>
        /// <param name="xScale">Value to scale by on the x-axis.</param>
        /// <param name="yScale">Value to scale by on the y-axis.</param>
        /// <param name="zScale">Value to scale by on the z-axis.</param>
        /// <param name="result">[OutAttribute] The created scaling Matrix.</param>
        public static void CreateScale(float xScale, float yScale, float zScale, out Matrix result)
        {
            var num3 = xScale;
            var num2 = yScale;
            var num = zScale;
            result.M11 = num3;
            result.M12 = 0f;
            result.M13 = 0f;
            result.M14 = 0f;
            result.M21 = 0f;
            result.M22 = num2;
            result.M23 = 0f;
            result.M24 = 0f;
            result.M31 = 0f;
            result.M32 = 0f;
            result.M33 = num;
            result.M34 = 0f;
            result.M41 = 0f;
            result.M42 = 0f;
            result.M43 = 0f;
            result.M44 = 1f;
        }

        /// <summary>Creates a scaling Matrix.</summary>
        /// <param name="scales">Amounts to scale by on the x, y, and z axes.</param>
        public static Matrix CreateScale(Vector3 scales)
        {
            Matrix matrix;
            var x = scales.X;
            var y = scales.Y;
            var z = scales.Z;
            matrix.M11 = x;
            matrix.M12 = 0f;
            matrix.M13 = 0f;
            matrix.M14 = 0f;
            matrix.M21 = 0f;
            matrix.M22 = y;
            matrix.M23 = 0f;
            matrix.M24 = 0f;
            matrix.M31 = 0f;
            matrix.M32 = 0f;
            matrix.M33 = z;
            matrix.M34 = 0f;
            matrix.M41 = 0f;
            matrix.M42 = 0f;
            matrix.M43 = 0f;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>Creates a scaling Matrix.</summary>
        /// <param name="scales">Amounts to scale by on the x, y, and z axes.</param>
        /// <param name="result">[OutAttribute] The created scaling Matrix.</param>
        public static void CreateScale(ref Vector3 scales, out Matrix result)
        {
            var x = scales.X;
            var y = scales.Y;
            var z = scales.Z;
            result.M11 = x;
            result.M12 = 0f;
            result.M13 = 0f;
            result.M14 = 0f;
            result.M21 = 0f;
            result.M22 = y;
            result.M23 = 0f;
            result.M24 = 0f;
            result.M31 = 0f;
            result.M32 = 0f;
            result.M33 = z;
            result.M34 = 0f;
            result.M41 = 0f;
            result.M42 = 0f;
            result.M43 = 0f;
            result.M44 = 1f;
        }

        /// <summary>Creates a scaling Matrix.</summary>
        /// <param name="scale">Amount to scale by.</param>
        public static Matrix CreateScale(float scale)
        {
            Matrix matrix;
            var num = scale;
            matrix.M11 = num;
            matrix.M12 = 0f;
            matrix.M13 = 0f;
            matrix.M14 = 0f;
            matrix.M21 = 0f;
            matrix.M22 = num;
            matrix.M23 = 0f;
            matrix.M24 = 0f;
            matrix.M31 = 0f;
            matrix.M32 = 0f;
            matrix.M33 = num;
            matrix.M34 = 0f;
            matrix.M41 = 0f;
            matrix.M42 = 0f;
            matrix.M43 = 0f;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>Creates a scaling Matrix.</summary>
        /// <param name="scale">Value to scale by.</param>
        /// <param name="result">[OutAttribute] The created scaling Matrix.</param>
        public static void CreateScale(float scale, out Matrix result)
        {
            var num = scale;
            result.M11 = num;
            result.M12 = 0f;
            result.M13 = 0f;
            result.M14 = 0f;
            result.M21 = 0f;
            result.M22 = num;
            result.M23 = 0f;
            result.M24 = 0f;
            result.M31 = 0f;
            result.M32 = 0f;
            result.M33 = num;
            result.M34 = 0f;
            result.M41 = 0f;
            result.M42 = 0f;
            result.M43 = 0f;
            result.M44 = 1f;
        }

        /// <summary>Returns a matrix that can be used to rotate a set of vertices around the x-axis.</summary>
        /// <param name="radians">The amount, in radians, in which to rotate around the x-axis. Note that you can use ToRadians to convert degrees to radians.</param>
        public static Matrix CreateRotationX(float radians)
        {
            Matrix matrix;
            var num2 = (float)Math.Cos(radians);
            var num = (float)Math.Sin(radians);
            matrix.M11 = 1f;
            matrix.M12 = 0f;
            matrix.M13 = 0f;
            matrix.M14 = 0f;
            matrix.M21 = 0f;
            matrix.M22 = num2;
            matrix.M23 = num;
            matrix.M24 = 0f;
            matrix.M31 = 0f;
            matrix.M32 = -num;
            matrix.M33 = num2;
            matrix.M34 = 0f;
            matrix.M41 = 0f;
            matrix.M42 = 0f;
            matrix.M43 = 0f;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>Populates data into a user-specified matrix that can be used to rotate a set of vertices around the x-axis.</summary>
        /// <param name="radians">The amount, in radians, in which to rotate around the x-axis. Note that you can use ToRadians to convert degrees to radians.</param>
        /// <param name="result">[OutAttribute] The matrix in which to place the calculated data.</param>
        public static void CreateRotationX(float radians, out Matrix result)
        {
            var num2 = (float)Math.Cos(radians);
            var num = (float)Math.Sin(radians);
            result.M11 = 1f;
            result.M12 = 0f;
            result.M13 = 0f;
            result.M14 = 0f;
            result.M21 = 0f;
            result.M22 = num2;
            result.M23 = num;
            result.M24 = 0f;
            result.M31 = 0f;
            result.M32 = -num;
            result.M33 = num2;
            result.M34 = 0f;
            result.M41 = 0f;
            result.M42 = 0f;
            result.M43 = 0f;
            result.M44 = 1f;
        }

        /// <summary>Returns a matrix that can be used to rotate a set of vertices around the y-axis.</summary>
        /// <param name="radians">The amount, in radians, in which to rotate around the y-axis. Note that you can use ToRadians to convert degrees to radians.</param>
        public static Matrix CreateRotationY(float radians)
        {
            Matrix matrix;
            var num2 = (float)Math.Cos(radians);
            var num = (float)Math.Sin(radians);
            matrix.M11 = num2;
            matrix.M12 = 0f;
            matrix.M13 = -num;
            matrix.M14 = 0f;
            matrix.M21 = 0f;
            matrix.M22 = 1f;
            matrix.M23 = 0f;
            matrix.M24 = 0f;
            matrix.M31 = num;
            matrix.M32 = 0f;
            matrix.M33 = num2;
            matrix.M34 = 0f;
            matrix.M41 = 0f;
            matrix.M42 = 0f;
            matrix.M43 = 0f;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>Populates data into a user-specified matrix that can be used to rotate a set of vertices around the y-axis.</summary>
        /// <param name="radians">The amount, in radians, in which to rotate around the y-axis. Note that you can use ToRadians to convert degrees to radians.</param>
        /// <param name="result">[OutAttribute] The matrix in which to place the calculated data.</param>
        public static void CreateRotationY(float radians, out Matrix result)
        {
            var num2 = (float)Math.Cos(radians);
            var num = (float)Math.Sin(radians);
            result.M11 = num2;
            result.M12 = 0f;
            result.M13 = -num;
            result.M14 = 0f;
            result.M21 = 0f;
            result.M22 = 1f;
            result.M23 = 0f;
            result.M24 = 0f;
            result.M31 = num;
            result.M32 = 0f;
            result.M33 = num2;
            result.M34 = 0f;
            result.M41 = 0f;
            result.M42 = 0f;
            result.M43 = 0f;
            result.M44 = 1f;
        }

        /// <summary>Returns a matrix that can be used to rotate a set of vertices around the z-axis.</summary>
        /// <param name="radians">The amount, in radians, in which to rotate around the z-axis. Note that you can use ToRadians to convert degrees to radians.</param>
        public static Matrix CreateRotationZ(float radians)
        {
            Matrix matrix;
            var num2 = (float)Math.Cos(radians);
            var num = (float)Math.Sin(radians);
            matrix.M11 = num2;
            matrix.M12 = num;
            matrix.M13 = 0f;
            matrix.M14 = 0f;
            matrix.M21 = -num;
            matrix.M22 = num2;
            matrix.M23 = 0f;
            matrix.M24 = 0f;
            matrix.M31 = 0f;
            matrix.M32 = 0f;
            matrix.M33 = 1f;
            matrix.M34 = 0f;
            matrix.M41 = 0f;
            matrix.M42 = 0f;
            matrix.M43 = 0f;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>Populates data into a user-specified matrix that can be used to rotate a set of vertices around the z-axis.</summary>
        /// <param name="radians">The amount, in radians, in which to rotate around the z-axis. Note that you can use ToRadians to convert degrees to radians.</param>
        /// <param name="result">[OutAttribute] The rotation matrix.</param>
        public static void CreateRotationZ(float radians, out Matrix result)
        {
            var num2 = (float)Math.Cos(radians);
            var num = (float)Math.Sin(radians);
            result.M11 = num2;
            result.M12 = num;
            result.M13 = 0f;
            result.M14 = 0f;
            result.M21 = -num;
            result.M22 = num2;
            result.M23 = 0f;
            result.M24 = 0f;
            result.M31 = 0f;
            result.M32 = 0f;
            result.M33 = 1f;
            result.M34 = 0f;
            result.M41 = 0f;
            result.M42 = 0f;
            result.M43 = 0f;
            result.M44 = 1f;
        }

        /// <summary>Creates a new Matrix that rotates around an arbitrary vector.</summary>
        /// <param name="axis">The axis to rotate around.</param>
        /// <param name="angle">The angle to rotate around the vector.</param>
        public static Matrix CreateFromAxisAngle(Vector3 axis, float angle)
        {
            Matrix matrix;
            var x = axis.X;
            var y = axis.Y;
            var z = axis.Z;
            var num2 = (float)Math.Sin(angle);
            var num = (float)Math.Cos(angle);
            var num11 = x * x;
            var num10 = y * y;
            var num9 = z * z;
            var num8 = x * y;
            var num7 = x * z;
            var num6 = y * z;
            matrix.M11 = num11 + (num * (1f - num11));
            matrix.M12 = (num8 - (num * num8)) + (num2 * z);
            matrix.M13 = (num7 - (num * num7)) - (num2 * y);
            matrix.M14 = 0f;
            matrix.M21 = (num8 - (num * num8)) - (num2 * z);
            matrix.M22 = num10 + (num * (1f - num10));
            matrix.M23 = (num6 - (num * num6)) + (num2 * x);
            matrix.M24 = 0f;
            matrix.M31 = (num7 - (num * num7)) + (num2 * y);
            matrix.M32 = (num6 - (num * num6)) - (num2 * x);
            matrix.M33 = num9 + (num * (1f - num9));
            matrix.M34 = 0f;
            matrix.M41 = 0f;
            matrix.M42 = 0f;
            matrix.M43 = 0f;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>Creates a new Matrix that rotates around an arbitrary vector.</summary>
        /// <param name="axis">The axis to rotate around.</param>
        /// <param name="angle">The angle to rotate around the vector.</param>
        /// <param name="result">[OutAttribute] The created Matrix.</param>
        public static void CreateFromAxisAngle(ref Vector3 axis, float angle, out Matrix result)
        {
            var x = axis.X;
            var y = axis.Y;
            var z = axis.Z;
            var num2 = (float)Math.Sin(angle);
            var num = (float)Math.Cos(angle);
            var num11 = x * x;
            var num10 = y * y;
            var num9 = z * z;
            var num8 = x * y;
            var num7 = x * z;
            var num6 = y * z;
            result.M11 = num11 + (num * (1f - num11));
            result.M12 = (num8 - (num * num8)) + (num2 * z);
            result.M13 = (num7 - (num * num7)) - (num2 * y);
            result.M14 = 0f;
            result.M21 = (num8 - (num * num8)) - (num2 * z);
            result.M22 = num10 + (num * (1f - num10));
            result.M23 = (num6 - (num * num6)) + (num2 * x);
            result.M24 = 0f;
            result.M31 = (num7 - (num * num7)) + (num2 * y);
            result.M32 = (num6 - (num * num6)) - (num2 * x);
            result.M33 = num9 + (num * (1f - num9));
            result.M34 = 0f;
            result.M41 = 0f;
            result.M42 = 0f;
            result.M43 = 0f;
            result.M44 = 1f;
        }

        /// <summary>Builds a perspective projection matrix based on a field of view. Reference page contains links to related code samples.</summary>
        /// <param name="fieldOfView">Field of view in radians.</param>
        /// <param name="aspectRatio">Aspect ratio, defined as view space width divided by height.</param>
        /// <param name="nearPlaneDistance">Distance to the near view plane.</param>
        /// <param name="farPlaneDistance">Distance to the far view plane.</param>
        public static Matrix CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance,
                                                          float farPlaneDistance)
        {
            Matrix matrix;
            if ((fieldOfView <= 0f) || (fieldOfView >= 3.141593f))
            {
                throw new ArgumentOutOfRangeException("fieldOfView",
                    string.Format(CultureInfo.CurrentCulture, FrameworkMessages.OutRangeFieldOfView,
                        new object[] { "fieldOfView" }));
            }
            if (nearPlaneDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException("nearPlaneDistance",
                    string.Format(CultureInfo.CurrentCulture, FrameworkMessages.NegativePlaneDistance,
                        new object[] { "nearPlaneDistance" }));
            }
            if (farPlaneDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException("farPlaneDistance",
                    string.Format(CultureInfo.CurrentCulture, FrameworkMessages.NegativePlaneDistance,
                        new object[] { "farPlaneDistance" }));
            }
            if (nearPlaneDistance >= farPlaneDistance)
                throw new ArgumentOutOfRangeException("nearPlaneDistance", FrameworkMessages.OppositePlanes);
            var num = 1f / ((float)Math.Tan((fieldOfView * 0.5f)));
            var num9 = num / aspectRatio;
            matrix.M11 = num9;
            matrix.M12 = matrix.M13 = matrix.M14 = 0f;
            matrix.M22 = num;
            matrix.M21 = matrix.M23 = matrix.M24 = 0f;
            matrix.M31 = matrix.M32 = 0f;
            matrix.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
            matrix.M34 = -1f;
            matrix.M41 = matrix.M42 = matrix.M44 = 0f;
            matrix.M43 = (nearPlaneDistance * farPlaneDistance) / (nearPlaneDistance - farPlaneDistance);
            return matrix;
        }

        /// <summary>Builds a perspective projection matrix based on a field of view. Reference page contains links to related code samples.</summary>
        /// <param name="fieldOfView">Field of view in radians.</param>
        /// <param name="aspectRatio">Aspect ratio, defined as view space width divided by height.</param>
        /// <param name="nearPlaneDistance">Distance to the near view plane.</param>
        /// <param name="farPlaneDistance">Distance to the far view plane.</param>
        /// <param name="result">[OutAttribute] The perspective projection matrix.</param>
        public static void CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance,
                                                        float farPlaneDistance, out Matrix result)
        {
            if ((fieldOfView <= 0f) || (fieldOfView >= 3.141593f))
            {
                throw new ArgumentOutOfRangeException("fieldOfView",
                    string.Format(CultureInfo.CurrentCulture, FrameworkMessages.OutRangeFieldOfView,
                        new object[] { "fieldOfView" }));
            }
            if (nearPlaneDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException("nearPlaneDistance",
                    string.Format(CultureInfo.CurrentCulture, FrameworkMessages.NegativePlaneDistance,
                        new object[] { "nearPlaneDistance" }));
            }
            if (farPlaneDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException("farPlaneDistance",
                    string.Format(CultureInfo.CurrentCulture, FrameworkMessages.NegativePlaneDistance,
                        new object[] { "farPlaneDistance" }));
            }
            if (nearPlaneDistance >= farPlaneDistance)
                throw new ArgumentOutOfRangeException("nearPlaneDistance", FrameworkMessages.OppositePlanes);
            var num = 1f / ((float)Math.Tan((fieldOfView * 0.5f)));
            var num9 = num / aspectRatio;
            result.M11 = num9;
            result.M12 = result.M13 = result.M14 = 0f;
            result.M22 = num;
            result.M21 = result.M23 = result.M24 = 0f;
            result.M31 = result.M32 = 0f;
            result.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
            result.M34 = -1f;
            result.M41 = result.M42 = result.M44 = 0f;
            result.M43 = (nearPlaneDistance * farPlaneDistance) / (nearPlaneDistance - farPlaneDistance);
        }

        /// <summary>Builds a perspective projection matrix. Reference page contains links to related code samples.</summary>
        /// <param name="width">Width of the view volume at the near view plane.</param>
        /// <param name="height">Height of the view volume at the near view plane.</param>
        /// <param name="nearPlaneDistance">Distance to the near view plane.</param>
        /// <param name="farPlaneDistance">Distance to the far view plane.</param>
        public static Matrix CreatePerspective(float width, float height, float nearPlaneDistance, float farPlaneDistance)
        {
            Matrix matrix;
            if (nearPlaneDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException("nearPlaneDistance",
                    string.Format(CultureInfo.CurrentCulture, FrameworkMessages.NegativePlaneDistance,
                        new object[] { "nearPlaneDistance" }));
            }
            if (farPlaneDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException("farPlaneDistance",
                    string.Format(CultureInfo.CurrentCulture, FrameworkMessages.NegativePlaneDistance,
                        new object[] { "farPlaneDistance" }));
            }
            if (nearPlaneDistance >= farPlaneDistance)
                throw new ArgumentOutOfRangeException("nearPlaneDistance", FrameworkMessages.OppositePlanes);
            matrix.M11 = (2f * nearPlaneDistance) / width;
            matrix.M12 = matrix.M13 = matrix.M14 = 0f;
            matrix.M22 = (2f * nearPlaneDistance) / height;
            matrix.M21 = matrix.M23 = matrix.M24 = 0f;
            matrix.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
            matrix.M31 = matrix.M32 = 0f;
            matrix.M34 = -1f;
            matrix.M41 = matrix.M42 = matrix.M44 = 0f;
            matrix.M43 = (nearPlaneDistance * farPlaneDistance) / (nearPlaneDistance - farPlaneDistance);
            return matrix;
        }

        /// <summary>Builds a perspective projection matrix. Reference page contains links to related code samples.</summary>
        /// <param name="width">Width of the view volume at the near view plane.</param>
        /// <param name="height">Height of the view volume at the near view plane.</param>
        /// <param name="nearPlaneDistance">Distance to the near view plane.</param>
        /// <param name="farPlaneDistance">Distance to the far view plane.</param>
        /// <param name="result">[OutAttribute] The projection matrix.</param>
        public static void CreatePerspective(float width, float height, float nearPlaneDistance, float farPlaneDistance,
                                             out Matrix result)
        {
            if (nearPlaneDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException("nearPlaneDistance",
                    string.Format(CultureInfo.CurrentCulture, FrameworkMessages.NegativePlaneDistance,
                        new object[] { "nearPlaneDistance" }));
            }
            if (farPlaneDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException("farPlaneDistance",
                    string.Format(CultureInfo.CurrentCulture, FrameworkMessages.NegativePlaneDistance,
                        new object[] { "farPlaneDistance" }));
            }
            if (nearPlaneDistance >= farPlaneDistance)
                throw new ArgumentOutOfRangeException("nearPlaneDistance", FrameworkMessages.OppositePlanes);
            result.M11 = (2f * nearPlaneDistance) / width;
            result.M12 = result.M13 = result.M14 = 0f;
            result.M22 = (2f * nearPlaneDistance) / height;
            result.M21 = result.M23 = result.M24 = 0f;
            result.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
            result.M31 = result.M32 = 0f;
            result.M34 = -1f;
            result.M41 = result.M42 = result.M44 = 0f;
            result.M43 = (nearPlaneDistance * farPlaneDistance) / (nearPlaneDistance - farPlaneDistance);
        }

        /// <summary>Builds a customized, perspective projection matrix. Reference page contains links to related code samples.</summary>
        /// <param name="left">Minimum x-value of the view volume at the near view plane.</param>
        /// <param name="right">Maximum x-value of the view volume at the near view plane.</param>
        /// <param name="bottom">Minimum y-value of the view volume at the near view plane.</param>
        /// <param name="top">Maximum y-value of the view volume at the near view plane.</param>
        /// <param name="nearPlaneDistance">Distance to the near view plane.</param>
        /// <param name="farPlaneDistance">Distance to of the far view plane.</param>
        public static Matrix CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float nearPlaneDistance,
                                                        float farPlaneDistance)
        {
            Matrix matrix;
            if (nearPlaneDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException("nearPlaneDistance",
                    string.Format(CultureInfo.CurrentCulture, FrameworkMessages.NegativePlaneDistance,
                        new object[] { "nearPlaneDistance" }));
            }
            if (farPlaneDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException("farPlaneDistance",
                    string.Format(CultureInfo.CurrentCulture, FrameworkMessages.NegativePlaneDistance,
                        new object[] { "farPlaneDistance" }));
            }
            if (nearPlaneDistance >= farPlaneDistance)
                throw new ArgumentOutOfRangeException("nearPlaneDistance", FrameworkMessages.OppositePlanes);
            matrix.M11 = (2f * nearPlaneDistance) / (right - left);
            matrix.M12 = matrix.M13 = matrix.M14 = 0f;
            matrix.M22 = (2f * nearPlaneDistance) / (top - bottom);
            matrix.M21 = matrix.M23 = matrix.M24 = 0f;
            matrix.M31 = (left + right) / (right - left);
            matrix.M32 = (top + bottom) / (top - bottom);
            matrix.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
            matrix.M34 = -1f;
            matrix.M43 = (nearPlaneDistance * farPlaneDistance) / (nearPlaneDistance - farPlaneDistance);
            matrix.M41 = matrix.M42 = matrix.M44 = 0f;
            return matrix;
        }

        /// <summary>Builds a customized, perspective projection matrix. Reference page contains links to related code samples.</summary>
        /// <param name="left">Minimum x-value of the view volume at the near view plane.</param>
        /// <param name="right">Maximum x-value of the view volume at the near view plane.</param>
        /// <param name="bottom">Minimum y-value of the view volume at the near view plane.</param>
        /// <param name="top">Maximum y-value of the view volume at the near view plane.</param>
        /// <param name="nearPlaneDistance">Distance to the near view plane.</param>
        /// <param name="farPlaneDistance">Distance to of the far view plane.</param>
        /// <param name="result">[OutAttribute] The created projection matrix.</param>
        public static void CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float nearPlaneDistance,
                                                      float farPlaneDistance, out Matrix result)
        {
            if (nearPlaneDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException("nearPlaneDistance",
                    string.Format(CultureInfo.CurrentCulture, FrameworkMessages.NegativePlaneDistance,
                        new object[] { "nearPlaneDistance" }));
            }
            if (farPlaneDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException("farPlaneDistance",
                    string.Format(CultureInfo.CurrentCulture, FrameworkMessages.NegativePlaneDistance,
                        new object[] { "farPlaneDistance" }));
            }
            if (nearPlaneDistance >= farPlaneDistance)
                throw new ArgumentOutOfRangeException("nearPlaneDistance", FrameworkMessages.OppositePlanes);
            result.M11 = (2f * nearPlaneDistance) / (right - left);
            result.M12 = result.M13 = result.M14 = 0f;
            result.M22 = (2f * nearPlaneDistance) / (top - bottom);
            result.M21 = result.M23 = result.M24 = 0f;
            result.M31 = (left + right) / (right - left);
            result.M32 = (top + bottom) / (top - bottom);
            result.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
            result.M34 = -1f;
            result.M43 = (nearPlaneDistance * farPlaneDistance) / (nearPlaneDistance - farPlaneDistance);
            result.M41 = result.M42 = result.M44 = 0f;
        }

        /// <summary>Builds an orthogonal projection matrix. Reference page contains links to related code samples.</summary>
        /// <param name="width">Width of the view volume.</param>
        /// <param name="height">Height of the view volume.</param>
        /// <param name="zNearPlane">Minimum z-value of the view volume.</param>
        /// <param name="zFarPlane">Maximum z-value of the view volume.</param>
        public static Matrix CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane)
        {
            Matrix matrix;
            matrix.M11 = 2f / width;
            matrix.M12 = matrix.M13 = matrix.M14 = 0f;
            matrix.M22 = 2f / height;
            matrix.M21 = matrix.M23 = matrix.M24 = 0f;
            matrix.M33 = 1f / (zNearPlane - zFarPlane);
            matrix.M31 = matrix.M32 = matrix.M34 = 0f;
            matrix.M41 = matrix.M42 = 0f;
            matrix.M43 = zNearPlane / (zNearPlane - zFarPlane);
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>Builds an orthogonal projection matrix. Reference page contains links to related code samples.</summary>
        /// <param name="width">Width of the view volume.</param>
        /// <param name="height">Height of the view volume.</param>
        /// <param name="zNearPlane">Minimum z-value of the view volume.</param>
        /// <param name="zFarPlane">Maximum z-value of the view volume.</param>
        /// <param name="result">[OutAttribute] The projection matrix.</param>
        public static void CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane, out Matrix result)
        {
            result.M11 = 2f / width;
            result.M12 = result.M13 = result.M14 = 0f;
            result.M22 = 2f / height;
            result.M21 = result.M23 = result.M24 = 0f;
            result.M33 = 1f / (zNearPlane - zFarPlane);
            result.M31 = result.M32 = result.M34 = 0f;
            result.M41 = result.M42 = 0f;
            result.M43 = zNearPlane / (zNearPlane - zFarPlane);
            result.M44 = 1f;
        }

        /// <summary>Builds a customized, orthogonal projection matrix. Reference page contains links to related code samples.</summary>
        /// <param name="left">Minimum x-value of the view volume.</param>
        /// <param name="right">Maximum x-value of the view volume.</param>
        /// <param name="bottom">Minimum y-value of the view volume.</param>
        /// <param name="top">Maximum y-value of the view volume.</param>
        /// <param name="zNearPlane">Minimum z-value of the view volume.</param>
        /// <param name="zFarPlane">Maximum z-value of the view volume.</param>
        public static Matrix CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane,
                                                         float zFarPlane)
        {
            Matrix matrix;
            matrix.M11 = 2f / (right - left);
            matrix.M12 = matrix.M13 = matrix.M14 = 0f;
            matrix.M22 = 2f / (top - bottom);
            matrix.M21 = matrix.M23 = matrix.M24 = 0f;
            matrix.M33 = 1f / (zNearPlane - zFarPlane);
            matrix.M31 = matrix.M32 = matrix.M34 = 0f;
            matrix.M41 = (left + right) / (left - right);
            matrix.M42 = (top + bottom) / (bottom - top);
            matrix.M43 = zNearPlane / (zNearPlane - zFarPlane);
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>Builds a customized, orthogonal projection matrix. Reference page contains links to related code samples.</summary>
        /// <param name="left">Minimum x-value of the view volume.</param>
        /// <param name="right">Maximum x-value of the view volume.</param>
        /// <param name="bottom">Minimum y-value of the view volume.</param>
        /// <param name="top">Maximum y-value of the view volume.</param>
        /// <param name="zNearPlane">Minimum z-value of the view volume.</param>
        /// <param name="zFarPlane">Maximum z-value of the view volume.</param>
        /// <param name="result">[OutAttribute] The projection matrix.</param>
        public static void CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane,
                                                       float zFarPlane, out Matrix result)
        {
            result.M11 = 2f / (right - left);
            result.M12 = result.M13 = result.M14 = 0f;
            result.M22 = 2f / (top - bottom);
            result.M21 = result.M23 = result.M24 = 0f;
            result.M33 = 1f / (zNearPlane - zFarPlane);
            result.M31 = result.M32 = result.M34 = 0f;
            result.M41 = (left + right) / (left - right);
            result.M42 = (top + bottom) / (bottom - top);
            result.M43 = zNearPlane / (zNearPlane - zFarPlane);
            result.M44 = 1f;
        }

        /// <summary>Creates a view matrix. Reference page contains links to related code samples.</summary>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraTarget">The target towards which the camera is pointing.</param>
        /// <param name="cameraUpVector">The direction that is "up" from the camera's point of view.</param>
        public static Matrix CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
        {
            Matrix matrix;
            var vector = Vector3.Normalize(cameraPosition - cameraTarget);
            var vector2 = Vector3.Normalize(Vector3.Cross(cameraUpVector, vector));
            var vector3 = Vector3.Cross(vector, vector2);
            matrix.M11 = vector2.X;
            matrix.M12 = vector3.X;
            matrix.M13 = vector.X;
            matrix.M14 = 0f;
            matrix.M21 = vector2.Y;
            matrix.M22 = vector3.Y;
            matrix.M23 = vector.Y;
            matrix.M24 = 0f;
            matrix.M31 = vector2.Z;
            matrix.M32 = vector3.Z;
            matrix.M33 = vector.Z;
            matrix.M34 = 0f;
            matrix.M41 = -Vector3.Dot(vector2, cameraPosition);
            matrix.M42 = -Vector3.Dot(vector3, cameraPosition);
            matrix.M43 = -Vector3.Dot(vector, cameraPosition);
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>Creates a view matrix. Reference page contains links to related code samples.</summary>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraTarget">The target towards which the camera is pointing.</param>
        /// <param name="cameraUpVector">The direction that is "up" from the camera's point of view.</param>
        /// <param name="result">[OutAttribute] The created view matrix.</param>
        public static void CreateLookAt(ref Vector3 cameraPosition, ref Vector3 cameraTarget, ref Vector3 cameraUpVector,
                                        out Matrix result)
        {
            var vector = Vector3.Normalize(cameraPosition - cameraTarget);
            var vector2 = Vector3.Normalize(Vector3.Cross(cameraUpVector, vector));
            var vector3 = Vector3.Cross(vector, vector2);
            result.M11 = vector2.X;
            result.M12 = vector3.X;
            result.M13 = vector.X;
            result.M14 = 0f;
            result.M21 = vector2.Y;
            result.M22 = vector3.Y;
            result.M23 = vector.Y;
            result.M24 = 0f;
            result.M31 = vector2.Z;
            result.M32 = vector3.Z;
            result.M33 = vector.Z;
            result.M34 = 0f;
            result.M41 = -Vector3.Dot(vector2, cameraPosition);
            result.M42 = -Vector3.Dot(vector3, cameraPosition);
            result.M43 = -Vector3.Dot(vector, cameraPosition);
            result.M44 = 1f;
        }

        /// <summary>Creates a world matrix with the specified parameters.</summary>
        /// <param name="position">Position of the object. This value is used in translation operations.</param>
        /// <param name="forward">Forward direction of the object.</param>
        /// <param name="up">Upward direction of the object; usually [0, 1, 0].</param>
        public static Matrix CreateWorld(Vector3 position, Vector3 forward, Vector3 up)
        {
            Matrix matrix;
            var vector = Vector3.Normalize(-(forward));
            var vector2 = Vector3.Normalize(Vector3.Cross(up, vector));
            var vector3 = Vector3.Cross(vector, vector2);
            matrix.M11 = vector2.X;
            matrix.M12 = vector2.Y;
            matrix.M13 = vector2.Z;
            matrix.M14 = 0f;
            matrix.M21 = vector3.X;
            matrix.M22 = vector3.Y;
            matrix.M23 = vector3.Z;
            matrix.M24 = 0f;
            matrix.M31 = vector.X;
            matrix.M32 = vector.Y;
            matrix.M33 = vector.Z;
            matrix.M34 = 0f;
            matrix.M41 = position.X;
            matrix.M42 = position.Y;
            matrix.M43 = position.Z;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>Creates a world matrix with the specified parameters.</summary>
        /// <param name="position">Position of the object. This value is used in translation operations.</param>
        /// <param name="forward">Forward direction of the object.</param>
        /// <param name="up">Upward direction of the object; usually [0, 1, 0].</param>
        /// <param name="result">[OutAttribute] The created world matrix.</param>
        public static void CreateWorld(ref Vector3 position, ref Vector3 forward, ref Vector3 up, out Matrix result)
        {
            var vector = Vector3.Normalize(-(forward));
            var vector2 = Vector3.Normalize(Vector3.Cross(up, vector));
            var vector3 = Vector3.Cross(vector, vector2);
            result.M11 = vector2.X;
            result.M12 = vector2.Y;
            result.M13 = vector2.Z;
            result.M14 = 0f;
            result.M21 = vector3.X;
            result.M22 = vector3.Y;
            result.M23 = vector3.Z;
            result.M24 = 0f;
            result.M31 = vector.X;
            result.M32 = vector.Y;
            result.M33 = vector.Z;
            result.M34 = 0f;
            result.M41 = position.X;
            result.M42 = position.Y;
            result.M43 = position.Z;
            result.M44 = 1f;
        }

        /// <summary>Creates a rotation Matrix from a Quaternion.</summary>
        /// <param name="quaternion">Quaternion to create the Matrix from.</param>
        public static Matrix CreateFromQuaternion(Quaternion quaternion)
        {
            Matrix matrix;
            var num9 = quaternion.X * quaternion.X;
            var num8 = quaternion.Y * quaternion.Y;
            var num7 = quaternion.Z * quaternion.Z;
            var num6 = quaternion.X * quaternion.Y;
            var num5 = quaternion.Z * quaternion.W;
            var num4 = quaternion.Z * quaternion.X;
            var num3 = quaternion.Y * quaternion.W;
            var num2 = quaternion.Y * quaternion.Z;
            var num = quaternion.X * quaternion.W;
            matrix.M11 = 1f - (2f * (num8 + num7));
            matrix.M12 = 2f * (num6 + num5);
            matrix.M13 = 2f * (num4 - num3);
            matrix.M14 = 0f;
            matrix.M21 = 2f * (num6 - num5);
            matrix.M22 = 1f - (2f * (num7 + num9));
            matrix.M23 = 2f * (num2 + num);
            matrix.M24 = 0f;
            matrix.M31 = 2f * (num4 + num3);
            matrix.M32 = 2f * (num2 - num);
            matrix.M33 = 1f - (2f * (num8 + num9));
            matrix.M34 = 0f;
            matrix.M41 = 0f;
            matrix.M42 = 0f;
            matrix.M43 = 0f;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>Creates a rotation Matrix from a Quaternion.</summary>
        /// <param name="quaternion">Quaternion to create the Matrix from.</param>
        /// <param name="result">[OutAttribute] The created Matrix.</param>
        public static void CreateFromQuaternion(ref Quaternion quaternion, out Matrix result)
        {
            var num9 = quaternion.X * quaternion.X;
            var num8 = quaternion.Y * quaternion.Y;
            var num7 = quaternion.Z * quaternion.Z;
            var num6 = quaternion.X * quaternion.Y;
            var num5 = quaternion.Z * quaternion.W;
            var num4 = quaternion.Z * quaternion.X;
            var num3 = quaternion.Y * quaternion.W;
            var num2 = quaternion.Y * quaternion.Z;
            var num = quaternion.X * quaternion.W;
            result.M11 = 1f - (2f * (num8 + num7));
            result.M12 = 2f * (num6 + num5);
            result.M13 = 2f * (num4 - num3);
            result.M14 = 0f;
            result.M21 = 2f * (num6 - num5);
            result.M22 = 1f - (2f * (num7 + num9));
            result.M23 = 2f * (num2 + num);
            result.M24 = 0f;
            result.M31 = 2f * (num4 + num3);
            result.M32 = 2f * (num2 - num);
            result.M33 = 1f - (2f * (num8 + num9));
            result.M34 = 0f;
            result.M41 = 0f;
            result.M42 = 0f;
            result.M43 = 0f;
            result.M44 = 1f;
        }

        /// <summary>Creates a new rotation matrix from a specified yaw, pitch, and roll.</summary>
        /// <param name="yaw">Angle of rotation, in radians, around the y-axis.</param>
        /// <param name="pitch">Angle of rotation, in radians, around the x-axis.</param>
        /// <param name="roll">Angle of rotation, in radians, around the z-axis.</param>
        public static Matrix CreateFromYawPitchRoll(float yaw, float pitch, float roll)
        {
            Matrix matrix;
            Quaternion quaternion;
            Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll, out quaternion);
            CreateFromQuaternion(ref quaternion, out matrix);
            return matrix;
        }

        /// <summary>Fills in a rotation matrix from a specified yaw, pitch, and roll.</summary>
        /// <param name="yaw">Angle of rotation, in radians, around the y-axis.</param>
        /// <param name="pitch">Angle of rotation, in radians, around the x-axis.</param>
        /// <param name="roll">Angle of rotation, in radians, around the z-axis.</param>
        /// <param name="result">[OutAttribute] An existing matrix filled in to represent the specified yaw, pitch, and roll.</param>
        public static void CreateFromYawPitchRoll(float yaw, float pitch, float roll, out Matrix result)
        {
            Quaternion quaternion;
            Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll, out quaternion);
            CreateFromQuaternion(ref quaternion, out result);
        }

        /// <summary>Creates a Matrix that flattens geometry into a specified Plane as if casting a shadow from a specified light source.</summary>
        /// <param name="lightDirection">A Vector3 specifying the direction from which the light that will cast the shadow is coming.</param>
        /// <param name="plane">The Plane onto which the new matrix should flatten geometry so as to cast a shadow.</param>
        public static Matrix CreateShadow(Vector3 lightDirection, Plane plane)
        {
            Matrix matrix;
            Plane plane2;
            Plane.Normalize(ref plane, out plane2);
            var num = ((plane2.Normal.X * lightDirection.X) + (plane2.Normal.Y * lightDirection.Y)) +
                      (plane2.Normal.Z * lightDirection.Z);
            var num5 = -plane2.Normal.X;
            var num4 = -plane2.Normal.Y;
            var num3 = -plane2.Normal.Z;
            var num2 = -plane2.D;
            matrix.M11 = (num5 * lightDirection.X) + num;
            matrix.M21 = num4 * lightDirection.X;
            matrix.M31 = num3 * lightDirection.X;
            matrix.M41 = num2 * lightDirection.X;
            matrix.M12 = num5 * lightDirection.Y;
            matrix.M22 = (num4 * lightDirection.Y) + num;
            matrix.M32 = num3 * lightDirection.Y;
            matrix.M42 = num2 * lightDirection.Y;
            matrix.M13 = num5 * lightDirection.Z;
            matrix.M23 = num4 * lightDirection.Z;
            matrix.M33 = (num3 * lightDirection.Z) + num;
            matrix.M43 = num2 * lightDirection.Z;
            matrix.M14 = 0f;
            matrix.M24 = 0f;
            matrix.M34 = 0f;
            matrix.M44 = num;
            return matrix;
        }

        /// <summary>Fills in a Matrix to flatten geometry into a specified Plane as if casting a shadow from a specified light source.</summary>
        /// <param name="lightDirection">A Vector3 specifying the direction from which the light that will cast the shadow is coming.</param>
        /// <param name="plane">The Plane onto which the new matrix should flatten geometry so as to cast a shadow.</param>
        /// <param name="result">[OutAttribute] A Matrix that can be used to flatten geometry onto the specified plane from the specified direction.</param>
        public static void CreateShadow(ref Vector3 lightDirection, ref Plane plane, out Matrix result)
        {
            Plane plane2;
            Plane.Normalize(ref plane, out plane2);
            var num = ((plane2.Normal.X * lightDirection.X) + (plane2.Normal.Y * lightDirection.Y)) +
                      (plane2.Normal.Z * lightDirection.Z);
            var num5 = -plane2.Normal.X;
            var num4 = -plane2.Normal.Y;
            var num3 = -plane2.Normal.Z;
            var num2 = -plane2.D;
            result.M11 = (num5 * lightDirection.X) + num;
            result.M21 = num4 * lightDirection.X;
            result.M31 = num3 * lightDirection.X;
            result.M41 = num2 * lightDirection.X;
            result.M12 = num5 * lightDirection.Y;
            result.M22 = (num4 * lightDirection.Y) + num;
            result.M32 = num3 * lightDirection.Y;
            result.M42 = num2 * lightDirection.Y;
            result.M13 = num5 * lightDirection.Z;
            result.M23 = num4 * lightDirection.Z;
            result.M33 = (num3 * lightDirection.Z) + num;
            result.M43 = num2 * lightDirection.Z;
            result.M14 = 0f;
            result.M24 = 0f;
            result.M34 = 0f;
            result.M44 = num;
        }

        /// <summary>Creates a Matrix that reflects the coordinate system about a specified Plane.</summary>
        /// <param name="value">The Plane about which to create a reflection.</param>
        public static Matrix CreateReflection(Plane value)
        {
            Matrix matrix;
            value.Normalize();
            var x = value.Normal.X;
            var y = value.Normal.Y;
            var z = value.Normal.Z;
            var num3 = -2f * x;
            var num2 = -2f * y;
            var num = -2f * z;
            matrix.M11 = (num3 * x) + 1f;
            matrix.M12 = num2 * x;
            matrix.M13 = num * x;
            matrix.M14 = 0f;
            matrix.M21 = num3 * y;
            matrix.M22 = (num2 * y) + 1f;
            matrix.M23 = num * y;
            matrix.M24 = 0f;
            matrix.M31 = num3 * z;
            matrix.M32 = num2 * z;
            matrix.M33 = (num * z) + 1f;
            matrix.M34 = 0f;
            matrix.M41 = num3 * value.D;
            matrix.M42 = num2 * value.D;
            matrix.M43 = num * value.D;
            matrix.M44 = 1f;
            return matrix;
        }

        /// <summary>Fills in an existing Matrix so that it reflects the coordinate system about a specified Plane.</summary>
        /// <param name="value">The Plane about which to create a reflection.</param>
        /// <param name="result">[OutAttribute] A Matrix that creates the reflection.</param>
        public static void CreateReflection(ref Plane value, out Matrix result)
        {
            Plane plane;
            Plane.Normalize(ref value, out plane);
            value.Normalize();
            var x = plane.Normal.X;
            var y = plane.Normal.Y;
            var z = plane.Normal.Z;
            var num3 = -2f * x;
            var num2 = -2f * y;
            var num = -2f * z;
            result.M11 = (num3 * x) + 1f;
            result.M12 = num2 * x;
            result.M13 = num * x;
            result.M14 = 0f;
            result.M21 = num3 * y;
            result.M22 = (num2 * y) + 1f;
            result.M23 = num * y;
            result.M24 = 0f;
            result.M31 = num3 * z;
            result.M32 = num2 * z;
            result.M33 = (num * z) + 1f;
            result.M34 = 0f;
            result.M41 = num3 * plane.D;
            result.M42 = num2 * plane.D;
            result.M43 = num * plane.D;
            result.M44 = 1f;
        }

        /// <summary>Transforms a Matrix by applying a Quaternion rotation.</summary>
        /// <param name="value">The Matrix to transform.</param>
        /// <param name="rotation">The rotation to apply, expressed as a Quaternion.</param>
        public static Matrix Transform(Matrix value, Quaternion rotation)
        {
            Matrix matrix;
            var num21 = rotation.X + rotation.X;
            var num11 = rotation.Y + rotation.Y;
            var num10 = rotation.Z + rotation.Z;
            var num20 = rotation.W * num21;
            var num19 = rotation.W * num11;
            var num18 = rotation.W * num10;
            var num17 = rotation.X * num21;
            var num16 = rotation.X * num11;
            var num15 = rotation.X * num10;
            var num14 = rotation.Y * num11;
            var num13 = rotation.Y * num10;
            var num12 = rotation.Z * num10;
            var num9 = (1f - num14) - num12;
            var num8 = num16 - num18;
            var num7 = num15 + num19;
            var num6 = num16 + num18;
            var num5 = (1f - num17) - num12;
            var num4 = num13 - num20;
            var num3 = num15 - num19;
            var num2 = num13 + num20;
            var num = (1f - num17) - num14;
            matrix.M11 = ((value.M11 * num9) + (value.M12 * num8)) + (value.M13 * num7);
            matrix.M12 = ((value.M11 * num6) + (value.M12 * num5)) + (value.M13 * num4);
            matrix.M13 = ((value.M11 * num3) + (value.M12 * num2)) + (value.M13 * num);
            matrix.M14 = value.M14;
            matrix.M21 = ((value.M21 * num9) + (value.M22 * num8)) + (value.M23 * num7);
            matrix.M22 = ((value.M21 * num6) + (value.M22 * num5)) + (value.M23 * num4);
            matrix.M23 = ((value.M21 * num3) + (value.M22 * num2)) + (value.M23 * num);
            matrix.M24 = value.M24;
            matrix.M31 = ((value.M31 * num9) + (value.M32 * num8)) + (value.M33 * num7);
            matrix.M32 = ((value.M31 * num6) + (value.M32 * num5)) + (value.M33 * num4);
            matrix.M33 = ((value.M31 * num3) + (value.M32 * num2)) + (value.M33 * num);
            matrix.M34 = value.M34;
            matrix.M41 = ((value.M41 * num9) + (value.M42 * num8)) + (value.M43 * num7);
            matrix.M42 = ((value.M41 * num6) + (value.M42 * num5)) + (value.M43 * num4);
            matrix.M43 = ((value.M41 * num3) + (value.M42 * num2)) + (value.M43 * num);
            matrix.M44 = value.M44;
            return matrix;
        }

        /// <summary>Transforms a Matrix by applying a Quaternion rotation.</summary>
        /// <param name="value">The Matrix to transform.</param>
        /// <param name="rotation">The rotation to apply, expressed as a Quaternion.</param>
        /// <param name="result">[OutAttribute] An existing Matrix filled in with the result of the transform.</param>
        public static void Transform(ref Matrix value, ref Quaternion rotation, out Matrix result)
        {
            var num21 = rotation.X + rotation.X;
            var num11 = rotation.Y + rotation.Y;
            var num10 = rotation.Z + rotation.Z;
            var num20 = rotation.W * num21;
            var num19 = rotation.W * num11;
            var num18 = rotation.W * num10;
            var num17 = rotation.X * num21;
            var num16 = rotation.X * num11;
            var num15 = rotation.X * num10;
            var num14 = rotation.Y * num11;
            var num13 = rotation.Y * num10;
            var num12 = rotation.Z * num10;
            var num9 = (1f - num14) - num12;
            var num8 = num16 - num18;
            var num7 = num15 + num19;
            var num6 = num16 + num18;
            var num5 = (1f - num17) - num12;
            var num4 = num13 - num20;
            var num3 = num15 - num19;
            var num2 = num13 + num20;
            var num = (1f - num17) - num14;
            var num37 = ((value.M11 * num9) + (value.M12 * num8)) + (value.M13 * num7);
            var num36 = ((value.M11 * num6) + (value.M12 * num5)) + (value.M13 * num4);
            var num35 = ((value.M11 * num3) + (value.M12 * num2)) + (value.M13 * num);
            var num34 = value.M14;
            var num33 = ((value.M21 * num9) + (value.M22 * num8)) + (value.M23 * num7);
            var num32 = ((value.M21 * num6) + (value.M22 * num5)) + (value.M23 * num4);
            var num31 = ((value.M21 * num3) + (value.M22 * num2)) + (value.M23 * num);
            var num30 = value.M24;
            var num29 = ((value.M31 * num9) + (value.M32 * num8)) + (value.M33 * num7);
            var num28 = ((value.M31 * num6) + (value.M32 * num5)) + (value.M33 * num4);
            var num27 = ((value.M31 * num3) + (value.M32 * num2)) + (value.M33 * num);
            var num26 = value.M34;
            var num25 = ((value.M41 * num9) + (value.M42 * num8)) + (value.M43 * num7);
            var num24 = ((value.M41 * num6) + (value.M42 * num5)) + (value.M43 * num4);
            var num23 = ((value.M41 * num3) + (value.M42 * num2)) + (value.M43 * num);
            var num22 = value.M44;
            result.M11 = num37;
            result.M12 = num36;
            result.M13 = num35;
            result.M14 = num34;
            result.M21 = num33;
            result.M22 = num32;
            result.M23 = num31;
            result.M24 = num30;
            result.M31 = num29;
            result.M32 = num28;
            result.M33 = num27;
            result.M34 = num26;
            result.M41 = num25;
            result.M42 = num24;
            result.M43 = num23;
            result.M44 = num22;
        }

        /// <summary>Retrieves a string representation of the current object.</summary>
        public override string ToString()
        {
            var currentCulture = CultureInfo.CurrentCulture;
            return ("{ " +
                    string.Format(currentCulture, "{{M11:{0} M12:{1} M13:{2} M14:{3}}} ",
                        new object[]
                        {
                            M11.ToString(currentCulture), M12.ToString(currentCulture), M13.ToString(currentCulture),
                            M14.ToString(currentCulture)
                        }) +
                    string.Format(currentCulture, "{{M21:{0} M22:{1} M23:{2} M24:{3}}} ",
                        new object[]
                        {
                            M21.ToString(currentCulture), M22.ToString(currentCulture), M23.ToString(currentCulture),
                            M24.ToString(currentCulture)
                        }) +
                    string.Format(currentCulture, "{{M31:{0} M32:{1} M33:{2} M34:{3}}} ",
                        new object[]
                        {
                            M31.ToString(currentCulture), M32.ToString(currentCulture), M33.ToString(currentCulture),
                            M34.ToString(currentCulture)
                        }) +
                    string.Format(currentCulture, "{{M41:{0} M42:{1} M43:{2} M44:{3}}} ",
                        new object[]
                        {
                            M41.ToString(currentCulture), M42.ToString(currentCulture), M43.ToString(currentCulture),
                            M44.ToString(currentCulture)
                        }) + "}");
        }

        /// <summary>Determines whether the specified Object is equal to the Matrix.</summary>
        /// <param name="other">The Object to compare with the current Matrix.</param>
        public bool Equals(Matrix other)
        {
            return ((((((M11 == other.M11) && (M22 == other.M22)) && ((M33 == other.M33) && (M44 == other.M44))) &&
                      (((M12 == other.M12) && (M13 == other.M13)) && ((M14 == other.M14) && (M21 == other.M21)))) &&
                     ((((M23 == other.M23) && (M24 == other.M24)) && ((M31 == other.M31) && (M32 == other.M32))) &&
                      (((M34 == other.M34) && (M41 == other.M41)) && (M42 == other.M42)))) && (M43 == other.M43));
        }

        /// <summary>Returns a value that indicates whether the current instance is equal to a specified object.</summary>
        /// <param name="obj">Object with which to make the comparison.</param>
        public override bool Equals(object obj)
        {
            var flag = false;
            if (obj is Matrix)
                flag = Equals((Matrix)obj);
            return flag;
        }

        /// <summary>Gets the hash code of this object.</summary>
        public override int GetHashCode()
        {
            return (((((((((((((((M11.GetHashCode() + M12.GetHashCode()) + M13.GetHashCode()) + M14.GetHashCode()) +
                               M21.GetHashCode()) + M22.GetHashCode()) + M23.GetHashCode()) + M24.GetHashCode()) +
                           M31.GetHashCode()) + M32.GetHashCode()) + M33.GetHashCode()) + M34.GetHashCode()) + M41.GetHashCode()) +
                      M42.GetHashCode()) + M43.GetHashCode()) + M44.GetHashCode());
        }

        /// <summary>Transposes the rows and columns of a matrix.</summary>
        /// <param name="matrix">Source matrix.</param>
        public static Matrix Transpose(Matrix matrix)
        {
            Matrix matrix2;
            matrix2.M11 = matrix.M11;
            matrix2.M12 = matrix.M21;
            matrix2.M13 = matrix.M31;
            matrix2.M14 = matrix.M41;
            matrix2.M21 = matrix.M12;
            matrix2.M22 = matrix.M22;
            matrix2.M23 = matrix.M32;
            matrix2.M24 = matrix.M42;
            matrix2.M31 = matrix.M13;
            matrix2.M32 = matrix.M23;
            matrix2.M33 = matrix.M33;
            matrix2.M34 = matrix.M43;
            matrix2.M41 = matrix.M14;
            matrix2.M42 = matrix.M24;
            matrix2.M43 = matrix.M34;
            matrix2.M44 = matrix.M44;
            return matrix2;
        }

        /// <summary>Transposes the rows and columns of a matrix.</summary>
        /// <param name="matrix">Source matrix.</param>
        /// <param name="result">[OutAttribute] Transposed matrix.</param>
        public static void Transpose(ref Matrix matrix, out Matrix result)
        {
            var num16 = matrix.M11;
            var num15 = matrix.M12;
            var num14 = matrix.M13;
            var num13 = matrix.M14;
            var num12 = matrix.M21;
            var num11 = matrix.M22;
            var num10 = matrix.M23;
            var num9 = matrix.M24;
            var num8 = matrix.M31;
            var num7 = matrix.M32;
            var num6 = matrix.M33;
            var num5 = matrix.M34;
            var num4 = matrix.M41;
            var num3 = matrix.M42;
            var num2 = matrix.M43;
            var num = matrix.M44;
            result.M11 = num16;
            result.M12 = num12;
            result.M13 = num8;
            result.M14 = num4;
            result.M21 = num15;
            result.M22 = num11;
            result.M23 = num7;
            result.M24 = num3;
            result.M31 = num14;
            result.M32 = num10;
            result.M33 = num6;
            result.M34 = num2;
            result.M41 = num13;
            result.M42 = num9;
            result.M43 = num5;
            result.M44 = num;
        }

        /// <summary>Calculates the determinant of the matrix.</summary>
        public float Determinant()
        {
            var num22 = M11;
            var num21 = M12;
            var num20 = M13;
            var num19 = M14;
            var num12 = M21;
            var num11 = M22;
            var num10 = M23;
            var num9 = M24;
            var num8 = M31;
            var num7 = M32;
            var num6 = M33;
            var num5 = M34;
            var num4 = M41;
            var num3 = M42;
            var num2 = M43;
            var num = M44;
            var num18 = (num6 * num) - (num5 * num2);
            var num17 = (num7 * num) - (num5 * num3);
            var num16 = (num7 * num2) - (num6 * num3);
            var num15 = (num8 * num) - (num5 * num4);
            var num14 = (num8 * num2) - (num6 * num4);
            var num13 = (num8 * num3) - (num7 * num4);
            return ((((num22 * (((num11 * num18) - (num10 * num17)) + (num9 * num16))) -
                      (num21 * (((num12 * num18) - (num10 * num15)) + (num9 * num14)))) +
                     (num20 * (((num12 * num17) - (num11 * num15)) + (num9 * num13)))) -
                    (num19 * (((num12 * num16) - (num11 * num14)) + (num10 * num13))));
        }

        /// <summary>Calculates the inverse of a matrix.</summary>
        /// <param name="matrix">Source matrix.</param>
        public static Matrix Invert(Matrix matrix)
        {
            Matrix matrix2;
            var num5 = matrix.M11;
            var num4 = matrix.M12;
            var num3 = matrix.M13;
            var num2 = matrix.M14;
            var num9 = matrix.M21;
            var num8 = matrix.M22;
            var num7 = matrix.M23;
            var num6 = matrix.M24;
            var num17 = matrix.M31;
            var num16 = matrix.M32;
            var num15 = matrix.M33;
            var num14 = matrix.M34;
            var num13 = matrix.M41;
            var num12 = matrix.M42;
            var num11 = matrix.M43;
            var num10 = matrix.M44;
            var num23 = (num15 * num10) - (num14 * num11);
            var num22 = (num16 * num10) - (num14 * num12);
            var num21 = (num16 * num11) - (num15 * num12);
            var num20 = (num17 * num10) - (num14 * num13);
            var num19 = (num17 * num11) - (num15 * num13);
            var num18 = (num17 * num12) - (num16 * num13);
            var num39 = ((num8 * num23) - (num7 * num22)) + (num6 * num21);
            var num38 = -(((num9 * num23) - (num7 * num20)) + (num6 * num19));
            var num37 = ((num9 * num22) - (num8 * num20)) + (num6 * num18);
            var num36 = -(((num9 * num21) - (num8 * num19)) + (num7 * num18));
            var num = 1f / ((((num5 * num39) + (num4 * num38)) + (num3 * num37)) + (num2 * num36));
            matrix2.M11 = num39 * num;
            matrix2.M21 = num38 * num;
            matrix2.M31 = num37 * num;
            matrix2.M41 = num36 * num;
            matrix2.M12 = -(((num4 * num23) - (num3 * num22)) + (num2 * num21)) * num;
            matrix2.M22 = (((num5 * num23) - (num3 * num20)) + (num2 * num19)) * num;
            matrix2.M32 = -(((num5 * num22) - (num4 * num20)) + (num2 * num18)) * num;
            matrix2.M42 = (((num5 * num21) - (num4 * num19)) + (num3 * num18)) * num;
            var num35 = (num7 * num10) - (num6 * num11);
            var num34 = (num8 * num10) - (num6 * num12);
            var num33 = (num8 * num11) - (num7 * num12);
            var num32 = (num9 * num10) - (num6 * num13);
            var num31 = (num9 * num11) - (num7 * num13);
            var num30 = (num9 * num12) - (num8 * num13);
            matrix2.M13 = (((num4 * num35) - (num3 * num34)) + (num2 * num33)) * num;
            matrix2.M23 = -(((num5 * num35) - (num3 * num32)) + (num2 * num31)) * num;
            matrix2.M33 = (((num5 * num34) - (num4 * num32)) + (num2 * num30)) * num;
            matrix2.M43 = -(((num5 * num33) - (num4 * num31)) + (num3 * num30)) * num;
            var num29 = (num7 * num14) - (num6 * num15);
            var num28 = (num8 * num14) - (num6 * num16);
            var num27 = (num8 * num15) - (num7 * num16);
            var num26 = (num9 * num14) - (num6 * num17);
            var num25 = (num9 * num15) - (num7 * num17);
            var num24 = (num9 * num16) - (num8 * num17);
            matrix2.M14 = -(((num4 * num29) - (num3 * num28)) + (num2 * num27)) * num;
            matrix2.M24 = (((num5 * num29) - (num3 * num26)) + (num2 * num25)) * num;
            matrix2.M34 = -(((num5 * num28) - (num4 * num26)) + (num2 * num24)) * num;
            matrix2.M44 = (((num5 * num27) - (num4 * num25)) + (num3 * num24)) * num;
            return matrix2;
        }

        /// <summary>Calculates the inverse of a matrix.</summary>
        /// <param name="matrix">The source matrix.</param>
        /// <param name="result">[OutAttribute] The inverse of matrix. The same matrix can be used for both arguments.</param>
        public static void Invert(ref Matrix matrix, out Matrix result)
        {
            var num5 = matrix.M11;
            var num4 = matrix.M12;
            var num3 = matrix.M13;
            var num2 = matrix.M14;
            var num9 = matrix.M21;
            var num8 = matrix.M22;
            var num7 = matrix.M23;
            var num6 = matrix.M24;
            var num17 = matrix.M31;
            var num16 = matrix.M32;
            var num15 = matrix.M33;
            var num14 = matrix.M34;
            var num13 = matrix.M41;
            var num12 = matrix.M42;
            var num11 = matrix.M43;
            var num10 = matrix.M44;
            var num23 = (num15 * num10) - (num14 * num11);
            var num22 = (num16 * num10) - (num14 * num12);
            var num21 = (num16 * num11) - (num15 * num12);
            var num20 = (num17 * num10) - (num14 * num13);
            var num19 = (num17 * num11) - (num15 * num13);
            var num18 = (num17 * num12) - (num16 * num13);
            var num39 = ((num8 * num23) - (num7 * num22)) + (num6 * num21);
            var num38 = -(((num9 * num23) - (num7 * num20)) + (num6 * num19));
            var num37 = ((num9 * num22) - (num8 * num20)) + (num6 * num18);
            var num36 = -(((num9 * num21) - (num8 * num19)) + (num7 * num18));
            var num = 1f / ((((num5 * num39) + (num4 * num38)) + (num3 * num37)) + (num2 * num36));
            result.M11 = num39 * num;
            result.M21 = num38 * num;
            result.M31 = num37 * num;
            result.M41 = num36 * num;
            result.M12 = -(((num4 * num23) - (num3 * num22)) + (num2 * num21)) * num;
            result.M22 = (((num5 * num23) - (num3 * num20)) + (num2 * num19)) * num;
            result.M32 = -(((num5 * num22) - (num4 * num20)) + (num2 * num18)) * num;
            result.M42 = (((num5 * num21) - (num4 * num19)) + (num3 * num18)) * num;
            var num35 = (num7 * num10) - (num6 * num11);
            var num34 = (num8 * num10) - (num6 * num12);
            var num33 = (num8 * num11) - (num7 * num12);
            var num32 = (num9 * num10) - (num6 * num13);
            var num31 = (num9 * num11) - (num7 * num13);
            var num30 = (num9 * num12) - (num8 * num13);
            result.M13 = (((num4 * num35) - (num3 * num34)) + (num2 * num33)) * num;
            result.M23 = -(((num5 * num35) - (num3 * num32)) + (num2 * num31)) * num;
            result.M33 = (((num5 * num34) - (num4 * num32)) + (num2 * num30)) * num;
            result.M43 = -(((num5 * num33) - (num4 * num31)) + (num3 * num30)) * num;
            var num29 = (num7 * num14) - (num6 * num15);
            var num28 = (num8 * num14) - (num6 * num16);
            var num27 = (num8 * num15) - (num7 * num16);
            var num26 = (num9 * num14) - (num6 * num17);
            var num25 = (num9 * num15) - (num7 * num17);
            var num24 = (num9 * num16) - (num8 * num17);
            result.M14 = -(((num4 * num29) - (num3 * num28)) + (num2 * num27)) * num;
            result.M24 = (((num5 * num29) - (num3 * num26)) + (num2 * num25)) * num;
            result.M34 = -(((num5 * num28) - (num4 * num26)) + (num2 * num24)) * num;
            result.M44 = (((num5 * num27) - (num4 * num25)) + (num3 * num24)) * num;
        }

        /// <summary>Linearly interpolates between the corresponding values of two matrices.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">Source matrix.</param>
        /// <param name="amount">Interpolation value.</param>
        public static Matrix Lerp(Matrix matrix1, Matrix matrix2, float amount)
        {
            Matrix matrix;
            matrix.M11 = matrix1.M11 + ((matrix2.M11 - matrix1.M11) * amount);
            matrix.M12 = matrix1.M12 + ((matrix2.M12 - matrix1.M12) * amount);
            matrix.M13 = matrix1.M13 + ((matrix2.M13 - matrix1.M13) * amount);
            matrix.M14 = matrix1.M14 + ((matrix2.M14 - matrix1.M14) * amount);
            matrix.M21 = matrix1.M21 + ((matrix2.M21 - matrix1.M21) * amount);
            matrix.M22 = matrix1.M22 + ((matrix2.M22 - matrix1.M22) * amount);
            matrix.M23 = matrix1.M23 + ((matrix2.M23 - matrix1.M23) * amount);
            matrix.M24 = matrix1.M24 + ((matrix2.M24 - matrix1.M24) * amount);
            matrix.M31 = matrix1.M31 + ((matrix2.M31 - matrix1.M31) * amount);
            matrix.M32 = matrix1.M32 + ((matrix2.M32 - matrix1.M32) * amount);
            matrix.M33 = matrix1.M33 + ((matrix2.M33 - matrix1.M33) * amount);
            matrix.M34 = matrix1.M34 + ((matrix2.M34 - matrix1.M34) * amount);
            matrix.M41 = matrix1.M41 + ((matrix2.M41 - matrix1.M41) * amount);
            matrix.M42 = matrix1.M42 + ((matrix2.M42 - matrix1.M42) * amount);
            matrix.M43 = matrix1.M43 + ((matrix2.M43 - matrix1.M43) * amount);
            matrix.M44 = matrix1.M44 + ((matrix2.M44 - matrix1.M44) * amount);
            return matrix;
        }

        /// <summary>Linearly interpolates between the corresponding values of two matrices.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">Source matrix.</param>
        /// <param name="amount">Interpolation value.</param>
        /// <param name="result">[OutAttribute] Resulting matrix.</param>
        public static void Lerp(ref Matrix matrix1, ref Matrix matrix2, float amount, out Matrix result)
        {
            result.M11 = matrix1.M11 + ((matrix2.M11 - matrix1.M11) * amount);
            result.M12 = matrix1.M12 + ((matrix2.M12 - matrix1.M12) * amount);
            result.M13 = matrix1.M13 + ((matrix2.M13 - matrix1.M13) * amount);
            result.M14 = matrix1.M14 + ((matrix2.M14 - matrix1.M14) * amount);
            result.M21 = matrix1.M21 + ((matrix2.M21 - matrix1.M21) * amount);
            result.M22 = matrix1.M22 + ((matrix2.M22 - matrix1.M22) * amount);
            result.M23 = matrix1.M23 + ((matrix2.M23 - matrix1.M23) * amount);
            result.M24 = matrix1.M24 + ((matrix2.M24 - matrix1.M24) * amount);
            result.M31 = matrix1.M31 + ((matrix2.M31 - matrix1.M31) * amount);
            result.M32 = matrix1.M32 + ((matrix2.M32 - matrix1.M32) * amount);
            result.M33 = matrix1.M33 + ((matrix2.M33 - matrix1.M33) * amount);
            result.M34 = matrix1.M34 + ((matrix2.M34 - matrix1.M34) * amount);
            result.M41 = matrix1.M41 + ((matrix2.M41 - matrix1.M41) * amount);
            result.M42 = matrix1.M42 + ((matrix2.M42 - matrix1.M42) * amount);
            result.M43 = matrix1.M43 + ((matrix2.M43 - matrix1.M43) * amount);
            result.M44 = matrix1.M44 + ((matrix2.M44 - matrix1.M44) * amount);
        }

        /// <summary>Negates individual elements of a matrix.</summary>
        /// <param name="matrix">Source matrix.</param>
        public static Matrix Negate(Matrix matrix)
        {
            Matrix matrix2;
            matrix2.M11 = -matrix.M11;
            matrix2.M12 = -matrix.M12;
            matrix2.M13 = -matrix.M13;
            matrix2.M14 = -matrix.M14;
            matrix2.M21 = -matrix.M21;
            matrix2.M22 = -matrix.M22;
            matrix2.M23 = -matrix.M23;
            matrix2.M24 = -matrix.M24;
            matrix2.M31 = -matrix.M31;
            matrix2.M32 = -matrix.M32;
            matrix2.M33 = -matrix.M33;
            matrix2.M34 = -matrix.M34;
            matrix2.M41 = -matrix.M41;
            matrix2.M42 = -matrix.M42;
            matrix2.M43 = -matrix.M43;
            matrix2.M44 = -matrix.M44;
            return matrix2;
        }

        /// <summary>Negates individual elements of a matrix.</summary>
        /// <param name="matrix">Source matrix.</param>
        /// <param name="result">[OutAttribute] Negated matrix.</param>
        public static void Negate(ref Matrix matrix, out Matrix result)
        {
            result.M11 = -matrix.M11;
            result.M12 = -matrix.M12;
            result.M13 = -matrix.M13;
            result.M14 = -matrix.M14;
            result.M21 = -matrix.M21;
            result.M22 = -matrix.M22;
            result.M23 = -matrix.M23;
            result.M24 = -matrix.M24;
            result.M31 = -matrix.M31;
            result.M32 = -matrix.M32;
            result.M33 = -matrix.M33;
            result.M34 = -matrix.M34;
            result.M41 = -matrix.M41;
            result.M42 = -matrix.M42;
            result.M43 = -matrix.M43;
            result.M44 = -matrix.M44;
        }

        /// <summary>Adds a matrix to another matrix.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">Source matrix.</param>
        public static Matrix Add(Matrix matrix1, Matrix matrix2)
        {
            Matrix matrix;
            matrix.M11 = matrix1.M11 + matrix2.M11;
            matrix.M12 = matrix1.M12 + matrix2.M12;
            matrix.M13 = matrix1.M13 + matrix2.M13;
            matrix.M14 = matrix1.M14 + matrix2.M14;
            matrix.M21 = matrix1.M21 + matrix2.M21;
            matrix.M22 = matrix1.M22 + matrix2.M22;
            matrix.M23 = matrix1.M23 + matrix2.M23;
            matrix.M24 = matrix1.M24 + matrix2.M24;
            matrix.M31 = matrix1.M31 + matrix2.M31;
            matrix.M32 = matrix1.M32 + matrix2.M32;
            matrix.M33 = matrix1.M33 + matrix2.M33;
            matrix.M34 = matrix1.M34 + matrix2.M34;
            matrix.M41 = matrix1.M41 + matrix2.M41;
            matrix.M42 = matrix1.M42 + matrix2.M42;
            matrix.M43 = matrix1.M43 + matrix2.M43;
            matrix.M44 = matrix1.M44 + matrix2.M44;
            return matrix;
        }

        /// <summary>Adds a matrix to another matrix.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">Source matrix.</param>
        /// <param name="result">[OutAttribute] Resulting matrix.</param>
        public static void Add(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
        {
            result.M11 = matrix1.M11 + matrix2.M11;
            result.M12 = matrix1.M12 + matrix2.M12;
            result.M13 = matrix1.M13 + matrix2.M13;
            result.M14 = matrix1.M14 + matrix2.M14;
            result.M21 = matrix1.M21 + matrix2.M21;
            result.M22 = matrix1.M22 + matrix2.M22;
            result.M23 = matrix1.M23 + matrix2.M23;
            result.M24 = matrix1.M24 + matrix2.M24;
            result.M31 = matrix1.M31 + matrix2.M31;
            result.M32 = matrix1.M32 + matrix2.M32;
            result.M33 = matrix1.M33 + matrix2.M33;
            result.M34 = matrix1.M34 + matrix2.M34;
            result.M41 = matrix1.M41 + matrix2.M41;
            result.M42 = matrix1.M42 + matrix2.M42;
            result.M43 = matrix1.M43 + matrix2.M43;
            result.M44 = matrix1.M44 + matrix2.M44;
        }

        /// <summary>Subtracts matrices.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">Source matrix.</param>
        public static Matrix Subtract(Matrix matrix1, Matrix matrix2)
        {
            Matrix matrix;
            matrix.M11 = matrix1.M11 - matrix2.M11;
            matrix.M12 = matrix1.M12 - matrix2.M12;
            matrix.M13 = matrix1.M13 - matrix2.M13;
            matrix.M14 = matrix1.M14 - matrix2.M14;
            matrix.M21 = matrix1.M21 - matrix2.M21;
            matrix.M22 = matrix1.M22 - matrix2.M22;
            matrix.M23 = matrix1.M23 - matrix2.M23;
            matrix.M24 = matrix1.M24 - matrix2.M24;
            matrix.M31 = matrix1.M31 - matrix2.M31;
            matrix.M32 = matrix1.M32 - matrix2.M32;
            matrix.M33 = matrix1.M33 - matrix2.M33;
            matrix.M34 = matrix1.M34 - matrix2.M34;
            matrix.M41 = matrix1.M41 - matrix2.M41;
            matrix.M42 = matrix1.M42 - matrix2.M42;
            matrix.M43 = matrix1.M43 - matrix2.M43;
            matrix.M44 = matrix1.M44 - matrix2.M44;
            return matrix;
        }

        /// <summary>Subtracts matrices.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">Source matrix.</param>
        /// <param name="result">[OutAttribute] Result of the subtraction.</param>
        public static void Subtract(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
        {
            result.M11 = matrix1.M11 - matrix2.M11;
            result.M12 = matrix1.M12 - matrix2.M12;
            result.M13 = matrix1.M13 - matrix2.M13;
            result.M14 = matrix1.M14 - matrix2.M14;
            result.M21 = matrix1.M21 - matrix2.M21;
            result.M22 = matrix1.M22 - matrix2.M22;
            result.M23 = matrix1.M23 - matrix2.M23;
            result.M24 = matrix1.M24 - matrix2.M24;
            result.M31 = matrix1.M31 - matrix2.M31;
            result.M32 = matrix1.M32 - matrix2.M32;
            result.M33 = matrix1.M33 - matrix2.M33;
            result.M34 = matrix1.M34 - matrix2.M34;
            result.M41 = matrix1.M41 - matrix2.M41;
            result.M42 = matrix1.M42 - matrix2.M42;
            result.M43 = matrix1.M43 - matrix2.M43;
            result.M44 = matrix1.M44 - matrix2.M44;
        }

        /// <summary>Multiplies a matrix by another matrix.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">Source matrix.</param>
        public static Matrix Multiply(Matrix matrix1, Matrix matrix2)
        {
            Matrix matrix;
            matrix.M11 = (((matrix1.M11 * matrix2.M11) + (matrix1.M12 * matrix2.M21)) + (matrix1.M13 * matrix2.M31)) +
                         (matrix1.M14 * matrix2.M41);
            matrix.M12 = (((matrix1.M11 * matrix2.M12) + (matrix1.M12 * matrix2.M22)) + (matrix1.M13 * matrix2.M32)) +
                         (matrix1.M14 * matrix2.M42);
            matrix.M13 = (((matrix1.M11 * matrix2.M13) + (matrix1.M12 * matrix2.M23)) + (matrix1.M13 * matrix2.M33)) +
                         (matrix1.M14 * matrix2.M43);
            matrix.M14 = (((matrix1.M11 * matrix2.M14) + (matrix1.M12 * matrix2.M24)) + (matrix1.M13 * matrix2.M34)) +
                         (matrix1.M14 * matrix2.M44);
            matrix.M21 = (((matrix1.M21 * matrix2.M11) + (matrix1.M22 * matrix2.M21)) + (matrix1.M23 * matrix2.M31)) +
                         (matrix1.M24 * matrix2.M41);
            matrix.M22 = (((matrix1.M21 * matrix2.M12) + (matrix1.M22 * matrix2.M22)) + (matrix1.M23 * matrix2.M32)) +
                         (matrix1.M24 * matrix2.M42);
            matrix.M23 = (((matrix1.M21 * matrix2.M13) + (matrix1.M22 * matrix2.M23)) + (matrix1.M23 * matrix2.M33)) +
                         (matrix1.M24 * matrix2.M43);
            matrix.M24 = (((matrix1.M21 * matrix2.M14) + (matrix1.M22 * matrix2.M24)) + (matrix1.M23 * matrix2.M34)) +
                         (matrix1.M24 * matrix2.M44);
            matrix.M31 = (((matrix1.M31 * matrix2.M11) + (matrix1.M32 * matrix2.M21)) + (matrix1.M33 * matrix2.M31)) +
                         (matrix1.M34 * matrix2.M41);
            matrix.M32 = (((matrix1.M31 * matrix2.M12) + (matrix1.M32 * matrix2.M22)) + (matrix1.M33 * matrix2.M32)) +
                         (matrix1.M34 * matrix2.M42);
            matrix.M33 = (((matrix1.M31 * matrix2.M13) + (matrix1.M32 * matrix2.M23)) + (matrix1.M33 * matrix2.M33)) +
                         (matrix1.M34 * matrix2.M43);
            matrix.M34 = (((matrix1.M31 * matrix2.M14) + (matrix1.M32 * matrix2.M24)) + (matrix1.M33 * matrix2.M34)) +
                         (matrix1.M34 * matrix2.M44);
            matrix.M41 = (((matrix1.M41 * matrix2.M11) + (matrix1.M42 * matrix2.M21)) + (matrix1.M43 * matrix2.M31)) +
                         (matrix1.M44 * matrix2.M41);
            matrix.M42 = (((matrix1.M41 * matrix2.M12) + (matrix1.M42 * matrix2.M22)) + (matrix1.M43 * matrix2.M32)) +
                         (matrix1.M44 * matrix2.M42);
            matrix.M43 = (((matrix1.M41 * matrix2.M13) + (matrix1.M42 * matrix2.M23)) + (matrix1.M43 * matrix2.M33)) +
                         (matrix1.M44 * matrix2.M43);
            matrix.M44 = (((matrix1.M41 * matrix2.M14) + (matrix1.M42 * matrix2.M24)) + (matrix1.M43 * matrix2.M34)) +
                         (matrix1.M44 * matrix2.M44);
            return matrix;
        }

        /// <summary>Multiplies a matrix by another matrix.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">Source matrix.</param>
        /// <param name="result">[OutAttribute] Result of the multiplication.</param>
        public static void Multiply(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
        {
            var num16 = (((matrix1.M11 * matrix2.M11) + (matrix1.M12 * matrix2.M21)) + (matrix1.M13 * matrix2.M31)) +
                        (matrix1.M14 * matrix2.M41);
            var num15 = (((matrix1.M11 * matrix2.M12) + (matrix1.M12 * matrix2.M22)) + (matrix1.M13 * matrix2.M32)) +
                        (matrix1.M14 * matrix2.M42);
            var num14 = (((matrix1.M11 * matrix2.M13) + (matrix1.M12 * matrix2.M23)) + (matrix1.M13 * matrix2.M33)) +
                        (matrix1.M14 * matrix2.M43);
            var num13 = (((matrix1.M11 * matrix2.M14) + (matrix1.M12 * matrix2.M24)) + (matrix1.M13 * matrix2.M34)) +
                        (matrix1.M14 * matrix2.M44);
            var num12 = (((matrix1.M21 * matrix2.M11) + (matrix1.M22 * matrix2.M21)) + (matrix1.M23 * matrix2.M31)) +
                        (matrix1.M24 * matrix2.M41);
            var num11 = (((matrix1.M21 * matrix2.M12) + (matrix1.M22 * matrix2.M22)) + (matrix1.M23 * matrix2.M32)) +
                        (matrix1.M24 * matrix2.M42);
            var num10 = (((matrix1.M21 * matrix2.M13) + (matrix1.M22 * matrix2.M23)) + (matrix1.M23 * matrix2.M33)) +
                        (matrix1.M24 * matrix2.M43);
            var num9 = (((matrix1.M21 * matrix2.M14) + (matrix1.M22 * matrix2.M24)) + (matrix1.M23 * matrix2.M34)) +
                       (matrix1.M24 * matrix2.M44);
            var num8 = (((matrix1.M31 * matrix2.M11) + (matrix1.M32 * matrix2.M21)) + (matrix1.M33 * matrix2.M31)) +
                       (matrix1.M34 * matrix2.M41);
            var num7 = (((matrix1.M31 * matrix2.M12) + (matrix1.M32 * matrix2.M22)) + (matrix1.M33 * matrix2.M32)) +
                       (matrix1.M34 * matrix2.M42);
            var num6 = (((matrix1.M31 * matrix2.M13) + (matrix1.M32 * matrix2.M23)) + (matrix1.M33 * matrix2.M33)) +
                       (matrix1.M34 * matrix2.M43);
            var num5 = (((matrix1.M31 * matrix2.M14) + (matrix1.M32 * matrix2.M24)) + (matrix1.M33 * matrix2.M34)) +
                       (matrix1.M34 * matrix2.M44);
            var num4 = (((matrix1.M41 * matrix2.M11) + (matrix1.M42 * matrix2.M21)) + (matrix1.M43 * matrix2.M31)) +
                       (matrix1.M44 * matrix2.M41);
            var num3 = (((matrix1.M41 * matrix2.M12) + (matrix1.M42 * matrix2.M22)) + (matrix1.M43 * matrix2.M32)) +
                       (matrix1.M44 * matrix2.M42);
            var num2 = (((matrix1.M41 * matrix2.M13) + (matrix1.M42 * matrix2.M23)) + (matrix1.M43 * matrix2.M33)) +
                       (matrix1.M44 * matrix2.M43);
            var num = (((matrix1.M41 * matrix2.M14) + (matrix1.M42 * matrix2.M24)) + (matrix1.M43 * matrix2.M34)) +
                      (matrix1.M44 * matrix2.M44);
            result.M11 = num16;
            result.M12 = num15;
            result.M13 = num14;
            result.M14 = num13;
            result.M21 = num12;
            result.M22 = num11;
            result.M23 = num10;
            result.M24 = num9;
            result.M31 = num8;
            result.M32 = num7;
            result.M33 = num6;
            result.M34 = num5;
            result.M41 = num4;
            result.M42 = num3;
            result.M43 = num2;
            result.M44 = num;
        }

        /// <summary>Multiplies a matrix by a scalar value.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        public static Matrix Multiply(Matrix matrix1, float scaleFactor)
        {
            Matrix matrix;
            var num = scaleFactor;
            matrix.M11 = matrix1.M11 * num;
            matrix.M12 = matrix1.M12 * num;
            matrix.M13 = matrix1.M13 * num;
            matrix.M14 = matrix1.M14 * num;
            matrix.M21 = matrix1.M21 * num;
            matrix.M22 = matrix1.M22 * num;
            matrix.M23 = matrix1.M23 * num;
            matrix.M24 = matrix1.M24 * num;
            matrix.M31 = matrix1.M31 * num;
            matrix.M32 = matrix1.M32 * num;
            matrix.M33 = matrix1.M33 * num;
            matrix.M34 = matrix1.M34 * num;
            matrix.M41 = matrix1.M41 * num;
            matrix.M42 = matrix1.M42 * num;
            matrix.M43 = matrix1.M43 * num;
            matrix.M44 = matrix1.M44 * num;
            return matrix;
        }

        /// <summary>Multiplies a matrix by a scalar value.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        /// <param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Matrix matrix1, float scaleFactor, out Matrix result)
        {
            var num = scaleFactor;
            result.M11 = matrix1.M11 * num;
            result.M12 = matrix1.M12 * num;
            result.M13 = matrix1.M13 * num;
            result.M14 = matrix1.M14 * num;
            result.M21 = matrix1.M21 * num;
            result.M22 = matrix1.M22 * num;
            result.M23 = matrix1.M23 * num;
            result.M24 = matrix1.M24 * num;
            result.M31 = matrix1.M31 * num;
            result.M32 = matrix1.M32 * num;
            result.M33 = matrix1.M33 * num;
            result.M34 = matrix1.M34 * num;
            result.M41 = matrix1.M41 * num;
            result.M42 = matrix1.M42 * num;
            result.M43 = matrix1.M43 * num;
            result.M44 = matrix1.M44 * num;
        }

        /// <summary>Divides the components of a matrix by the corresponding components of another matrix.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">The divisor.</param>
        public static Matrix Divide(Matrix matrix1, Matrix matrix2)
        {
            Matrix matrix;
            matrix.M11 = matrix1.M11 / matrix2.M11;
            matrix.M12 = matrix1.M12 / matrix2.M12;
            matrix.M13 = matrix1.M13 / matrix2.M13;
            matrix.M14 = matrix1.M14 / matrix2.M14;
            matrix.M21 = matrix1.M21 / matrix2.M21;
            matrix.M22 = matrix1.M22 / matrix2.M22;
            matrix.M23 = matrix1.M23 / matrix2.M23;
            matrix.M24 = matrix1.M24 / matrix2.M24;
            matrix.M31 = matrix1.M31 / matrix2.M31;
            matrix.M32 = matrix1.M32 / matrix2.M32;
            matrix.M33 = matrix1.M33 / matrix2.M33;
            matrix.M34 = matrix1.M34 / matrix2.M34;
            matrix.M41 = matrix1.M41 / matrix2.M41;
            matrix.M42 = matrix1.M42 / matrix2.M42;
            matrix.M43 = matrix1.M43 / matrix2.M43;
            matrix.M44 = matrix1.M44 / matrix2.M44;
            return matrix;
        }

        /// <summary>Divides the components of a matrix by the corresponding components of another matrix.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">The divisor.</param>
        /// <param name="result">[OutAttribute] Result of the division.</param>
        public static void Divide(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
        {
            result.M11 = matrix1.M11 / matrix2.M11;
            result.M12 = matrix1.M12 / matrix2.M12;
            result.M13 = matrix1.M13 / matrix2.M13;
            result.M14 = matrix1.M14 / matrix2.M14;
            result.M21 = matrix1.M21 / matrix2.M21;
            result.M22 = matrix1.M22 / matrix2.M22;
            result.M23 = matrix1.M23 / matrix2.M23;
            result.M24 = matrix1.M24 / matrix2.M24;
            result.M31 = matrix1.M31 / matrix2.M31;
            result.M32 = matrix1.M32 / matrix2.M32;
            result.M33 = matrix1.M33 / matrix2.M33;
            result.M34 = matrix1.M34 / matrix2.M34;
            result.M41 = matrix1.M41 / matrix2.M41;
            result.M42 = matrix1.M42 / matrix2.M42;
            result.M43 = matrix1.M43 / matrix2.M43;
            result.M44 = matrix1.M44 / matrix2.M44;
        }

        /// <summary>Divides the components of a matrix by a scalar.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="divider">The divisor.</param>
        public static Matrix Divide(Matrix matrix1, float divider)
        {
            Matrix matrix;
            var num = 1f / divider;
            matrix.M11 = matrix1.M11 * num;
            matrix.M12 = matrix1.M12 * num;
            matrix.M13 = matrix1.M13 * num;
            matrix.M14 = matrix1.M14 * num;
            matrix.M21 = matrix1.M21 * num;
            matrix.M22 = matrix1.M22 * num;
            matrix.M23 = matrix1.M23 * num;
            matrix.M24 = matrix1.M24 * num;
            matrix.M31 = matrix1.M31 * num;
            matrix.M32 = matrix1.M32 * num;
            matrix.M33 = matrix1.M33 * num;
            matrix.M34 = matrix1.M34 * num;
            matrix.M41 = matrix1.M41 * num;
            matrix.M42 = matrix1.M42 * num;
            matrix.M43 = matrix1.M43 * num;
            matrix.M44 = matrix1.M44 * num;
            return matrix;
        }

        /// <summary>Divides the components of a matrix by a scalar.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="divider">The divisor.</param>
        /// <param name="result">[OutAttribute] Result of the division.</param>
        public static void Divide(ref Matrix matrix1, float divider, out Matrix result)
        {
            var num = 1f / divider;
            result.M11 = matrix1.M11 * num;
            result.M12 = matrix1.M12 * num;
            result.M13 = matrix1.M13 * num;
            result.M14 = matrix1.M14 * num;
            result.M21 = matrix1.M21 * num;
            result.M22 = matrix1.M22 * num;
            result.M23 = matrix1.M23 * num;
            result.M24 = matrix1.M24 * num;
            result.M31 = matrix1.M31 * num;
            result.M32 = matrix1.M32 * num;
            result.M33 = matrix1.M33 * num;
            result.M34 = matrix1.M34 * num;
            result.M41 = matrix1.M41 * num;
            result.M42 = matrix1.M42 * num;
            result.M43 = matrix1.M43 * num;
            result.M44 = matrix1.M44 * num;
        }

        /// <summary>Negates individual elements of a matrix.</summary>
        /// <param name="matrix1">Source matrix.</param>
        public static Matrix operator -(Matrix matrix1)
        {
            Matrix matrix;
            matrix.M11 = -matrix1.M11;
            matrix.M12 = -matrix1.M12;
            matrix.M13 = -matrix1.M13;
            matrix.M14 = -matrix1.M14;
            matrix.M21 = -matrix1.M21;
            matrix.M22 = -matrix1.M22;
            matrix.M23 = -matrix1.M23;
            matrix.M24 = -matrix1.M24;
            matrix.M31 = -matrix1.M31;
            matrix.M32 = -matrix1.M32;
            matrix.M33 = -matrix1.M33;
            matrix.M34 = -matrix1.M34;
            matrix.M41 = -matrix1.M41;
            matrix.M42 = -matrix1.M42;
            matrix.M43 = -matrix1.M43;
            matrix.M44 = -matrix1.M44;
            return matrix;
        }

        /// <summary>Compares a matrix for equality with another matrix.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">Source matrix.</param>
        public static bool operator ==(Matrix matrix1, Matrix matrix2)
        {
            return ((((((matrix1.M11 == matrix2.M11) && (matrix1.M22 == matrix2.M22)) &&
                       ((matrix1.M33 == matrix2.M33) && (matrix1.M44 == matrix2.M44))) &&
                      (((matrix1.M12 == matrix2.M12) && (matrix1.M13 == matrix2.M13)) &&
                       ((matrix1.M14 == matrix2.M14) && (matrix1.M21 == matrix2.M21)))) &&
                     ((((matrix1.M23 == matrix2.M23) && (matrix1.M24 == matrix2.M24)) &&
                       ((matrix1.M31 == matrix2.M31) && (matrix1.M32 == matrix2.M32))) &&
                      (((matrix1.M34 == matrix2.M34) && (matrix1.M41 == matrix2.M41)) && (matrix1.M42 == matrix2.M42)))) &&
                    (matrix1.M43 == matrix2.M43));
        }

        /// <summary>Tests a matrix for inequality with another matrix.</summary>
        /// <param name="matrix1">The matrix on the left of the equal sign.</param>
        /// <param name="matrix2">The matrix on the right of the equal sign.</param>
        public static bool operator !=(Matrix matrix1, Matrix matrix2)
        {
            if (((((matrix1.M11 == matrix2.M11) && (matrix1.M12 == matrix2.M12)) &&
                  ((matrix1.M13 == matrix2.M13) && (matrix1.M14 == matrix2.M14))) &&
                 (((matrix1.M21 == matrix2.M21) && (matrix1.M22 == matrix2.M22)) &&
                  ((matrix1.M23 == matrix2.M23) && (matrix1.M24 == matrix2.M24)))) &&
                ((((matrix1.M31 == matrix2.M31) && (matrix1.M32 == matrix2.M32)) &&
                  ((matrix1.M33 == matrix2.M33) && (matrix1.M34 == matrix2.M34))) &&
                 (((matrix1.M41 == matrix2.M41) && (matrix1.M42 == matrix2.M42)) && (matrix1.M43 == matrix2.M43))))
                return (matrix1.M44 != matrix2.M44);
            return true;
        }

        /// <summary>Adds a matrix to another matrix.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">Source matrix.</param>
        public static Matrix operator +(Matrix matrix1, Matrix matrix2)
        {
            Matrix matrix;
            matrix.M11 = matrix1.M11 + matrix2.M11;
            matrix.M12 = matrix1.M12 + matrix2.M12;
            matrix.M13 = matrix1.M13 + matrix2.M13;
            matrix.M14 = matrix1.M14 + matrix2.M14;
            matrix.M21 = matrix1.M21 + matrix2.M21;
            matrix.M22 = matrix1.M22 + matrix2.M22;
            matrix.M23 = matrix1.M23 + matrix2.M23;
            matrix.M24 = matrix1.M24 + matrix2.M24;
            matrix.M31 = matrix1.M31 + matrix2.M31;
            matrix.M32 = matrix1.M32 + matrix2.M32;
            matrix.M33 = matrix1.M33 + matrix2.M33;
            matrix.M34 = matrix1.M34 + matrix2.M34;
            matrix.M41 = matrix1.M41 + matrix2.M41;
            matrix.M42 = matrix1.M42 + matrix2.M42;
            matrix.M43 = matrix1.M43 + matrix2.M43;
            matrix.M44 = matrix1.M44 + matrix2.M44;
            return matrix;
        }

        /// <summary>Subtracts matrices.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">Source matrix.</param>
        public static Matrix operator -(Matrix matrix1, Matrix matrix2)
        {
            Matrix matrix;
            matrix.M11 = matrix1.M11 - matrix2.M11;
            matrix.M12 = matrix1.M12 - matrix2.M12;
            matrix.M13 = matrix1.M13 - matrix2.M13;
            matrix.M14 = matrix1.M14 - matrix2.M14;
            matrix.M21 = matrix1.M21 - matrix2.M21;
            matrix.M22 = matrix1.M22 - matrix2.M22;
            matrix.M23 = matrix1.M23 - matrix2.M23;
            matrix.M24 = matrix1.M24 - matrix2.M24;
            matrix.M31 = matrix1.M31 - matrix2.M31;
            matrix.M32 = matrix1.M32 - matrix2.M32;
            matrix.M33 = matrix1.M33 - matrix2.M33;
            matrix.M34 = matrix1.M34 - matrix2.M34;
            matrix.M41 = matrix1.M41 - matrix2.M41;
            matrix.M42 = matrix1.M42 - matrix2.M42;
            matrix.M43 = matrix1.M43 - matrix2.M43;
            matrix.M44 = matrix1.M44 - matrix2.M44;
            return matrix;
        }

        /// <summary>Multiplies a matrix by another matrix.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">Source matrix.</param>
        public static Matrix operator *(Matrix matrix1, Matrix matrix2)
        {
            Matrix matrix;
            matrix.M11 = (((matrix1.M11 * matrix2.M11) + (matrix1.M12 * matrix2.M21)) + (matrix1.M13 * matrix2.M31)) +
                         (matrix1.M14 * matrix2.M41);
            matrix.M12 = (((matrix1.M11 * matrix2.M12) + (matrix1.M12 * matrix2.M22)) + (matrix1.M13 * matrix2.M32)) +
                         (matrix1.M14 * matrix2.M42);
            matrix.M13 = (((matrix1.M11 * matrix2.M13) + (matrix1.M12 * matrix2.M23)) + (matrix1.M13 * matrix2.M33)) +
                         (matrix1.M14 * matrix2.M43);
            matrix.M14 = (((matrix1.M11 * matrix2.M14) + (matrix1.M12 * matrix2.M24)) + (matrix1.M13 * matrix2.M34)) +
                         (matrix1.M14 * matrix2.M44);
            matrix.M21 = (((matrix1.M21 * matrix2.M11) + (matrix1.M22 * matrix2.M21)) + (matrix1.M23 * matrix2.M31)) +
                         (matrix1.M24 * matrix2.M41);
            matrix.M22 = (((matrix1.M21 * matrix2.M12) + (matrix1.M22 * matrix2.M22)) + (matrix1.M23 * matrix2.M32)) +
                         (matrix1.M24 * matrix2.M42);
            matrix.M23 = (((matrix1.M21 * matrix2.M13) + (matrix1.M22 * matrix2.M23)) + (matrix1.M23 * matrix2.M33)) +
                         (matrix1.M24 * matrix2.M43);
            matrix.M24 = (((matrix1.M21 * matrix2.M14) + (matrix1.M22 * matrix2.M24)) + (matrix1.M23 * matrix2.M34)) +
                         (matrix1.M24 * matrix2.M44);
            matrix.M31 = (((matrix1.M31 * matrix2.M11) + (matrix1.M32 * matrix2.M21)) + (matrix1.M33 * matrix2.M31)) +
                         (matrix1.M34 * matrix2.M41);
            matrix.M32 = (((matrix1.M31 * matrix2.M12) + (matrix1.M32 * matrix2.M22)) + (matrix1.M33 * matrix2.M32)) +
                         (matrix1.M34 * matrix2.M42);
            matrix.M33 = (((matrix1.M31 * matrix2.M13) + (matrix1.M32 * matrix2.M23)) + (matrix1.M33 * matrix2.M33)) +
                         (matrix1.M34 * matrix2.M43);
            matrix.M34 = (((matrix1.M31 * matrix2.M14) + (matrix1.M32 * matrix2.M24)) + (matrix1.M33 * matrix2.M34)) +
                         (matrix1.M34 * matrix2.M44);
            matrix.M41 = (((matrix1.M41 * matrix2.M11) + (matrix1.M42 * matrix2.M21)) + (matrix1.M43 * matrix2.M31)) +
                         (matrix1.M44 * matrix2.M41);
            matrix.M42 = (((matrix1.M41 * matrix2.M12) + (matrix1.M42 * matrix2.M22)) + (matrix1.M43 * matrix2.M32)) +
                         (matrix1.M44 * matrix2.M42);
            matrix.M43 = (((matrix1.M41 * matrix2.M13) + (matrix1.M42 * matrix2.M23)) + (matrix1.M43 * matrix2.M33)) +
                         (matrix1.M44 * matrix2.M43);
            matrix.M44 = (((matrix1.M41 * matrix2.M14) + (matrix1.M42 * matrix2.M24)) + (matrix1.M43 * matrix2.M34)) +
                         (matrix1.M44 * matrix2.M44);
            return matrix;
        }

        /// <summary>Multiplies a matrix by a scalar value.</summary>
        /// <param name="matrix">Source matrix.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        public static Matrix operator *(Matrix matrix, float scaleFactor)
        {
            Matrix matrix2;
            var num = scaleFactor;
            matrix2.M11 = matrix.M11 * num;
            matrix2.M12 = matrix.M12 * num;
            matrix2.M13 = matrix.M13 * num;
            matrix2.M14 = matrix.M14 * num;
            matrix2.M21 = matrix.M21 * num;
            matrix2.M22 = matrix.M22 * num;
            matrix2.M23 = matrix.M23 * num;
            matrix2.M24 = matrix.M24 * num;
            matrix2.M31 = matrix.M31 * num;
            matrix2.M32 = matrix.M32 * num;
            matrix2.M33 = matrix.M33 * num;
            matrix2.M34 = matrix.M34 * num;
            matrix2.M41 = matrix.M41 * num;
            matrix2.M42 = matrix.M42 * num;
            matrix2.M43 = matrix.M43 * num;
            matrix2.M44 = matrix.M44 * num;
            return matrix2;
        }

        /// <summary>Multiplies a matrix by a scalar value.</summary>
        /// <param name="scaleFactor">Scalar value.</param>
        /// <param name="matrix">Source matrix.</param>
        public static Matrix operator *(float scaleFactor, Matrix matrix)
        {
            Matrix matrix2;
            var num = scaleFactor;
            matrix2.M11 = matrix.M11 * num;
            matrix2.M12 = matrix.M12 * num;
            matrix2.M13 = matrix.M13 * num;
            matrix2.M14 = matrix.M14 * num;
            matrix2.M21 = matrix.M21 * num;
            matrix2.M22 = matrix.M22 * num;
            matrix2.M23 = matrix.M23 * num;
            matrix2.M24 = matrix.M24 * num;
            matrix2.M31 = matrix.M31 * num;
            matrix2.M32 = matrix.M32 * num;
            matrix2.M33 = matrix.M33 * num;
            matrix2.M34 = matrix.M34 * num;
            matrix2.M41 = matrix.M41 * num;
            matrix2.M42 = matrix.M42 * num;
            matrix2.M43 = matrix.M43 * num;
            matrix2.M44 = matrix.M44 * num;
            return matrix2;
        }

        /// <summary>Divides the components of a matrix by the corresponding components of another matrix.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">The divisor.</param>
        public static Matrix operator /(Matrix matrix1, Matrix matrix2)
        {
            Matrix matrix;
            matrix.M11 = matrix1.M11 / matrix2.M11;
            matrix.M12 = matrix1.M12 / matrix2.M12;
            matrix.M13 = matrix1.M13 / matrix2.M13;
            matrix.M14 = matrix1.M14 / matrix2.M14;
            matrix.M21 = matrix1.M21 / matrix2.M21;
            matrix.M22 = matrix1.M22 / matrix2.M22;
            matrix.M23 = matrix1.M23 / matrix2.M23;
            matrix.M24 = matrix1.M24 / matrix2.M24;
            matrix.M31 = matrix1.M31 / matrix2.M31;
            matrix.M32 = matrix1.M32 / matrix2.M32;
            matrix.M33 = matrix1.M33 / matrix2.M33;
            matrix.M34 = matrix1.M34 / matrix2.M34;
            matrix.M41 = matrix1.M41 / matrix2.M41;
            matrix.M42 = matrix1.M42 / matrix2.M42;
            matrix.M43 = matrix1.M43 / matrix2.M43;
            matrix.M44 = matrix1.M44 / matrix2.M44;
            return matrix;
        }

        /// <summary>Divides the components of a matrix by a scalar.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="divider">The divisor.</param>
        public static Matrix operator /(Matrix matrix1, float divider)
        {
            Matrix matrix;
            var num = 1f / divider;
            matrix.M11 = matrix1.M11 * num;
            matrix.M12 = matrix1.M12 * num;
            matrix.M13 = matrix1.M13 * num;
            matrix.M14 = matrix1.M14 * num;
            matrix.M21 = matrix1.M21 * num;
            matrix.M22 = matrix1.M22 * num;
            matrix.M23 = matrix1.M23 * num;
            matrix.M24 = matrix1.M24 * num;
            matrix.M31 = matrix1.M31 * num;
            matrix.M32 = matrix1.M32 * num;
            matrix.M33 = matrix1.M33 * num;
            matrix.M34 = matrix1.M34 * num;
            matrix.M41 = matrix1.M41 * num;
            matrix.M42 = matrix1.M42 * num;
            matrix.M43 = matrix1.M43 * num;
            matrix.M44 = matrix1.M44 * num;
            return matrix;
        }

        /// <summary>
        /// Initializes the <see cref="Matrix"/> struct.
        /// </summary>
        static Matrix()
        {
            _identity = new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f);
        }
    }
}