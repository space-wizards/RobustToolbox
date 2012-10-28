using System;
using System.Runtime.InteropServices;
using System.Security;
using SFML.Window;

namespace SFML
{
    namespace Graphics
    {
        ////////////////////////////////////////////////////////////
        /// <summary>
        /// Define a 3x3 transform matrix
        /// </summary>
        ////////////////////////////////////////////////////////////
        [StructLayout(LayoutKind.Sequential)]
        public struct Transform
        {
            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct a transform from a 3x3 matrix
            /// </summary>
            /// <param name="a00">Element (0, 0) of the matrix</param>
            /// <param name="a01">Element (0, 1) of the matrix</param>
            /// <param name="a02">Element (0, 2) of the matrix</param>
            /// <param name="a10">Element (1, 0) of the matrix</param>
            /// <param name="a11">Element (1, 1) of the matrix</param>
            /// <param name="a12">Element (1, 2) of the matrix</param>
            /// <param name="a20">Element (2, 0) of the matrix</param>
            /// <param name="a21">Element (2, 1) of the matrix</param>
            /// <param name="a22">Element (2, 2) of the matrix</param>
            ////////////////////////////////////////////////////////////
            public Transform(float a00, float a01, float a02,
                             float a10, float a11, float a12,
                             float a20, float a21, float a22)
            {
                m00 = a00; m01 = a01; m02 = a02;
                m10 = a10; m11 = a11; m12 = a12;
                m20 = a20; m21 = a21; m22 = a22;
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Return the inverse of the transform.
            /// 
            /// If the inverse cannot be computed, an identity transform
            /// is returned.
            /// </summary>
            /// <returns>A new transform which is the inverse of self</returns>
            ////////////////////////////////////////////////////////////
            public Transform GetInverse()
            {
                return sfTransform_getInverse(ref this);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Transform a 2D point.
            /// </summary>
            /// <param name="x">X coordinate of the point to transform</param>
            /// <param name="y">Y coordinate of the point to transform</param>
            /// <returns>Transformed point</returns>
            ////////////////////////////////////////////////////////////
            public Vector2 TransformPoint(float x, float y)
            {
                return TransformPoint(new Vector2(x, y));
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Transform a 2D point.
            /// </summary>
            /// <param name="point">Point to transform</param>
            /// <returns>Transformed point</returns>
            ////////////////////////////////////////////////////////////
            public Vector2 TransformPoint(Vector2 point)
            {
                return sfTransform_transformPoint(ref this, point);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Transform a rectangle.
            /// 
            /// Since SFML doesn't provide support for oriented rectangles,
            /// the result of this function is always an axis-aligned
            /// rectangle. Which means that if the transform contains a
            /// rotation, the bounding rectangle of the transformed rectangle
            /// is returned.
            /// </summary>
            /// <param name="rectangle">Rectangle to transform</param>
            /// <returns>Transformed rectangle</returns>
            ////////////////////////////////////////////////////////////
            public FloatRect TransformRect(FloatRect rectangle)
            {
                return sfTransform_transformRect(ref this, rectangle);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Combine the current transform with another one.
            /// 
            /// The result is a transform that is equivalent to applying
            /// this followed by transform. Mathematically, it is
            /// equivalent to a matrix multiplication.
            /// </summary>
            /// <param name="transform">Transform to combine to this transform</param>
            ////////////////////////////////////////////////////////////
            public void Combine(Transform transform)
            {
                sfTransform_combine(ref this, ref transform);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Combine the current transform with a translation.
            /// </summary>
            /// <param name="x">Offset to apply on X axis</param>
            /// <param name="y">Offset to apply on Y axis</param>
            ////////////////////////////////////////////////////////////
            public void Translate(float x, float y)
            {
                sfTransform_translate(ref this, x, y);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Combine the current transform with a translation.
            /// </summary>
            /// <param name="offset">Translation offset to apply</param>
            ////////////////////////////////////////////////////////////
            public void Translate(Vector2 offset)
            {
                Translate(offset.X, offset.Y);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Combine the current transform with a rotation.
            /// </summary>
            /// <param name="angle">Rotation angle, in degrees</param>
            ////////////////////////////////////////////////////////////
            public void Rotate(float angle)
            {
                sfTransform_rotate(ref this, angle);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Combine the current transform with a rotation.
            /// 
            /// The center of rotation is provided for convenience as a second
            /// argument, so that you can build rotations around arbitrary points
            /// more easily (and efficiently) than the usual
            /// Translate(-center); Rotate(angle); Translate(center).
            /// </summary>
            /// <param name="angle">Rotation angle, in degrees</param>
            /// <param name="centerX">X coordinate of the center of rotation</param>
            /// <param name="centerY">Y coordinate of the center of rotation</param>
            ////////////////////////////////////////////////////////////
            public void Rotate(float angle, float centerX, float centerY)
            {
                sfTransform_rotateWithCenter(ref this, angle, centerX, centerY);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Combine the current transform with a rotation.
            /// 
            /// The center of rotation is provided for convenience as a second
            /// argument, so that you can build rotations around arbitrary points
            /// more easily (and efficiently) than the usual
            /// Translate(-center); Rotate(angle); Translate(center).
            /// </summary>
            /// <param name="angle">Rotation angle, in degrees</param>
            /// <param name="center">Center of rotation</param>
            ////////////////////////////////////////////////////////////
            public void Rotate(float angle, Vector2 center)
            {
                Rotate(angle, center.X, center.Y);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Combine the current transform with a scaling.
            /// </summary>
            /// <param name="scaleX">Scaling factor on the X axis</param>
            /// <param name="scaleY">Scaling factor on the Y axis</param>
            ////////////////////////////////////////////////////////////
            public void Scale(float scaleX, float scaleY)
            {
                sfTransform_scale(ref this, scaleX, scaleY);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Combine the current transform with a scaling.
            /// 
            /// The center of scaling is provided for convenience as a second
            /// argument, so that you can build scaling around arbitrary points
            /// more easily (and efficiently) than the usual
            /// Translate(-center); Scale(factors); Translate(center).
            /// </summary>
            /// <param name="scaleX">Scaling factor on X axis</param>
            /// <param name="scaleY">Scaling factor on Y axis</param>
            /// <param name="centerX">X coordinate of the center of scaling</param>
            /// <param name="centerY">Y coordinate of the center of scaling</param>
            ////////////////////////////////////////////////////////////
            public void Scale(float scaleX, float scaleY, float centerX, float centerY)
            {
                sfTransform_scaleWithCenter(ref this, scaleX, scaleY, centerX, centerY);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Combine the current transform with a scaling.
            /// </summary>
            /// <param name="factors">Scaling factors</param>
            ////////////////////////////////////////////////////////////
            public void Scale(Vector2 factors)
            {
                Scale(factors.X, factors.Y);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Combine the current transform with a scaling.
            /// 
            /// The center of scaling is provided for convenience as a second
            /// argument, so that you can build scaling around arbitrary points
            /// more easily (and efficiently) than the usual
            /// Translate(-center); Scale(factors); Translate(center).
            /// </summary>
            /// <param name="factors">Scaling factors</param>
            /// <param name="center">Center of scaling</param>
            ////////////////////////////////////////////////////////////
            public void Scale(Vector2 factors, Vector2 center)
            {
                Scale(factors.X, factors.Y, center.X, center.Y);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Overload of binary operator * to combine two transforms.
            /// This call is equivalent to calling new Transform(left).Combine(right).
            /// </summary>
            /// <param name="left">Left operand (the first transform)</param>
            /// <param name="right">Right operand (the second transform)</param>
            /// <returns>New combined transform</returns>
            ////////////////////////////////////////////////////////////
            public static Transform operator *(Transform left, Transform right)
            {
                left.Combine(right);
                return left;
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Overload of binary operator * to transform a point.
            /// This call is equivalent to calling left.TransformPoint(right).
            /// </summary>
            /// <param name="left">Left operand (the transform)</param>
            /// <param name="right">Right operand (the point to transform)</param>
            /// <returns>New transformed point</returns>
            ////////////////////////////////////////////////////////////
            public static Vector2 operator *(Transform left, Vector2 right)
            {
                return left.TransformPoint(right);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>The identity transform (does nothing)</summary>
            ////////////////////////////////////////////////////////////
            public static Transform Identity
            {
                get
                {
                    return new Transform(1, 0, 0,
                                         0, 1, 0,
                                         0, 0, 1);
                }
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Provide a string describing the object
            /// </summary>
            /// <returns>String description of the object</returns>
            ////////////////////////////////////////////////////////////
            public override string ToString()
            {
                return "[Transform]" +
                       " Matrix(" +
                       m00 + ", " + m01 + ", " + m02 +
                       m10 + ", " + m11 + ", " + m12 +
                       m20 + ", " + m21 + ", " + m22 +
                       ")";
            }

            float m00, m01, m02;
            float m10, m11, m12;
            float m20, m21, m22;

            #region Imports

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern Transform sfTransform_getInverse(ref Transform transform);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern Vector2 sfTransform_transformPoint(ref Transform transform, Vector2 point);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern FloatRect sfTransform_transformRect(ref Transform transform, FloatRect rectangle);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfTransform_combine(ref Transform transform, ref Transform other);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfTransform_translate(ref Transform transform, float x, float y);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfTransform_rotate(ref Transform transform, float angle);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfTransform_rotateWithCenter(ref Transform transform, float angle, float centerX, float centerY);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfTransform_scale(ref Transform transform, float scaleX, float scaleY);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfTransform_scaleWithCenter(ref Transform transform, float scaleX, float scaleY, float centerX, float centerY);

            #endregion
        }
    }
}
