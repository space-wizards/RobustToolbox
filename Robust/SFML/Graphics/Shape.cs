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
        /// Base class for textured shapes with outline
        /// </summary>
        ////////////////////////////////////////////////////////////
        public abstract class Shape : Transformable, Drawable
        {
            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Source texture of the shape
            /// </summary>
            ////////////////////////////////////////////////////////////
            public Texture Texture
            {
                get { return myTexture; }
                set { myTexture = value; sfShape_setTexture(CPointer, value != null ? value.CPointer : IntPtr.Zero, false); }
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Sub-rectangle of the texture that the shape will display
            /// </summary>
            ////////////////////////////////////////////////////////////
            public IntRect TextureRect
            {
                get { return sfShape_getTextureRect(CPointer); }
                set { sfShape_setTextureRect(CPointer, value); }
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Fill color of the shape
            /// </summary>
            ////////////////////////////////////////////////////////////
            public Color FillColor
            {
                get { return sfShape_getFillColor(CPointer); }
                set { sfShape_setFillColor(CPointer, value); }
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Outline color of the shape
            /// </summary>
            ////////////////////////////////////////////////////////////
            public Color OutlineColor
            {
                get { return sfShape_getOutlineColor(CPointer); }
                set { sfShape_setOutlineColor(CPointer, value); }
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Thickness of the shape's outline
            /// </summary>
            ////////////////////////////////////////////////////////////
            public float OutlineThickness
            {
                get { return sfShape_getOutlineThickness(CPointer); }
                set { sfShape_setOutlineThickness(CPointer, value); }
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Get the total number of points of the shape
            /// </summary>
            /// <returns>The total point count</returns>
            ////////////////////////////////////////////////////////////
            public abstract uint GetPointCount();

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Get a point of the shape.
            ///
            /// The result is undefined if index is out of the valid range.
            /// </summary>
            /// <param name="index">Index of the point to get, in range [0 .. PointCount - 1]</param>
            /// <returns>Index-th point of the shape</returns>
            ////////////////////////////////////////////////////////////
            public abstract Vector2 GetPoint(uint index);

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Get the local bounding rectangle of the entity.
            ///
            /// The returned rectangle is in local coordinates, which means
            /// that it ignores the transformations (translation, rotation,
            /// scale, ...) that are applied to the entity.
            /// In other words, this function returns the bounds of the
            /// entity in the entity's coordinate system.
            /// </summary>
            /// <returns>Local bounding rectangle of the entity</returns>
            ////////////////////////////////////////////////////////////
            public FloatRect GetLocalBounds()
            {
                return sfShape_getLocalBounds(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Get the global bounding rectangle of the entity.
            ///
            /// The returned rectangle is in global coordinates, which means
            /// that it takes in account the transformations (translation,
            /// rotation, scale, ...) that are applied to the entity.
            /// In other words, this function returns the bounds of the
            /// sprite in the global 2D world's coordinate system.
            /// </summary>
            /// <returns>Global bounding rectangle of the entity</returns>
            ////////////////////////////////////////////////////////////
            public FloatRect GetGlobalBounds()
            {
                // we don't use the native getGlobalBounds function,
                // because we override the object's transform
                return Transform.TransformRect(GetLocalBounds());
            }

            ////////////////////////////////////////////////////////////
            /// <summmary>
            /// Draw the object to a render target
            ///
            /// This is a pure virtual function that has to be implemented
            /// by the derived class to define how the drawable should be
            /// drawn.
            /// </summmary>
            /// <param name="target">Render target to draw to</param>
            /// <param name="states">Current render states</param>
            ////////////////////////////////////////////////////////////
            public void Draw(RenderTarget target, RenderStates states)
            {
                states.Transform *= Transform;
                RenderStates.MarshalData marshaledStates = states.Marshal();

                if (target is RenderWindow)
                {
                    sfRenderWindow_drawShape(((RenderWindow)target).CPointer, CPointer, ref marshaledStates);
                }
                else if (target is RenderTexture)
                {
                    sfRenderTexture_drawShape(((RenderTexture)target).CPointer, CPointer, ref marshaledStates);
                }
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Default constructor
            /// </summary>
            ////////////////////////////////////////////////////////////
            protected Shape() :
                base(IntPtr.Zero)
            {
                myGetPointCountCallback = new GetPointCountCallbackType(InternalGetPointCount);
                myGetPointCallback = new GetPointCallbackType(InternalGetPoint);
                SetThis(sfShape_create(myGetPointCountCallback, myGetPointCallback, IntPtr.Zero));
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the shape from another shape
            /// </summary>
            /// <param name="copy">Shape to copy</param>
            ////////////////////////////////////////////////////////////
            public Shape(Shape copy) :
                base(IntPtr.Zero)
            {
                myGetPointCountCallback = new GetPointCountCallbackType(InternalGetPointCount);
                myGetPointCallback = new GetPointCallbackType(InternalGetPoint);
                SetThis(sfShape_create(myGetPointCountCallback, myGetPointCallback, IntPtr.Zero));

                Origin = copy.Origin;
                Position = copy.Position;
                Rotation = copy.Rotation;
                Scale = copy.Scale;

                Texture = copy.Texture;
                TextureRect = copy.TextureRect;
                FillColor = copy.FillColor;
                OutlineColor = copy.OutlineColor;
                OutlineThickness = copy.OutlineThickness;
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Recompute the internal geometry of the shape.
            ///
            /// This function must be called by the derived class everytime
            /// the shape's points change (ie. the result of either
            /// PointCount or GetPoint is different).
            /// </summary>
            ////////////////////////////////////////////////////////////
            protected void Update()
            {
                sfShape_update(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Handle the destruction of the object
            /// </summary>
            /// <param name="disposing">Is the GC disposing the object, or is it an explicit call ?</param>
            ////////////////////////////////////////////////////////////
            protected override void Destroy(bool disposing)
            {
                sfShape_destroy(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Callback passed to the C API
            /// </summary>
            ////////////////////////////////////////////////////////////
            private uint InternalGetPointCount(IntPtr userData)
            {
                return GetPointCount();
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Callback passed to the C API
            /// </summary>
            ////////////////////////////////////////////////////////////
            private Vector2 InternalGetPoint(uint index, IntPtr userData)
            {
                return GetPoint(index);
            }

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate uint GetPointCountCallbackType(IntPtr UserData);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate Vector2 GetPointCallbackType(uint index, IntPtr UserData);

            private GetPointCountCallbackType myGetPointCountCallback;
            private GetPointCallbackType myGetPointCallback;

            private Texture myTexture = null;

            #region Imports

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfShape_create(GetPointCountCallbackType getPointCount, GetPointCallbackType getPoint, IntPtr userData);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfShape_copy(IntPtr Shape);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfShape_destroy(IntPtr CPointer);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfShape_setTexture(IntPtr CPointer, IntPtr Texture, bool AdjustToNewSize);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfShape_setTextureRect(IntPtr CPointer, IntRect Rect);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntRect sfShape_getTextureRect(IntPtr CPointer);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfShape_setFillColor(IntPtr CPointer, Color Color);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern Color sfShape_getFillColor(IntPtr CPointer);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfShape_setOutlineColor(IntPtr CPointer, Color Color);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern Color sfShape_getOutlineColor(IntPtr CPointer);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfShape_setOutlineThickness(IntPtr CPointer, float Thickness);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern float sfShape_getOutlineThickness(IntPtr CPointer);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern FloatRect sfShape_getLocalBounds(IntPtr CPointer);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfShape_update(IntPtr CPointer);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfRenderWindow_drawShape(IntPtr CPointer, IntPtr Shape, ref RenderStates.MarshalData states);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfRenderTexture_drawShape(IntPtr CPointer, IntPtr Shape, ref RenderStates.MarshalData states);

            #endregion
        }
    }
}
