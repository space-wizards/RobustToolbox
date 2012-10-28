using System;
using System.Security;
using System.Runtime.InteropServices;
using SFML.Window;

namespace SFML
{
    namespace Graphics
    {
        ////////////////////////////////////////////////////////////
        /// <summary>
        /// This class defines a sprite : texture, transformations,
        /// color, and draw on screen
        /// </summary>
        ////////////////////////////////////////////////////////////
        public class Sprite : Transformable, Drawable
        {
            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Default constructor
            /// </summary>
            ////////////////////////////////////////////////////////////
            public Sprite() :
                base(sfSprite_create())
            {
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the sprite from a source texture
            /// </summary>
            /// <param name="texture">Source texture to assign to the sprite</param>
            ////////////////////////////////////////////////////////////
            public Sprite(Texture texture) :
                base(sfSprite_create())
            {
                Texture = texture;
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the sprite from a source texture
            /// </summary>
            /// <param name="texture">Source texture to assign to the sprite</param>
            /// <param name="rectangle">Sub-rectangle of the texture to assign to the sprite</param>
            ////////////////////////////////////////////////////////////
            public Sprite(Texture texture, IntRect rectangle) :
                base(sfSprite_create())
            {
                Texture = texture;
                TextureRect = rectangle;
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the sprite from another sprite
            /// </summary>
            /// <param name="copy">Sprite to copy</param>
            ////////////////////////////////////////////////////////////
            public Sprite(Sprite copy) :
                base(sfSprite_copy(copy.CPointer))
            {
                Origin = copy.Origin;
                Position = copy.Position;
                Rotation = copy.Rotation;
                Scale = copy.Scale;

                Texture = copy.Texture;
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Global color of the object
            /// </summary>
            ////////////////////////////////////////////////////////////
            public Color Color
            {
                get { return sfSprite_getColor(CPointer); }
                set { sfSprite_setColor(CPointer, value); }
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Source texture displayed by the sprite
            /// </summary>
            ////////////////////////////////////////////////////////////
            public Texture Texture
            {
                get { return myTexture; }
                set { myTexture = value; sfSprite_setTexture(CPointer, value != null ? value.CPointer : IntPtr.Zero, false); }
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Sub-rectangle of the source image displayed by the sprite
            /// </summary>
            ////////////////////////////////////////////////////////////
            public IntRect TextureRect
            {
                get { return sfSprite_getTextureRect(CPointer); }
                set { sfSprite_setTextureRect(CPointer, value); }
            }

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
                return sfSprite_getLocalBounds(CPointer);
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
            /// <summary>
            /// Provide a string describing the object
            /// </summary>
            /// <returns>String description of the object</returns>
            ////////////////////////////////////////////////////////////
            public override string ToString()
            {
                return "[Sprite]" +
                       " Color(" + Color + ")" +
                       " Texture(" + Texture + ")" +
                       " TextureRect(" + TextureRect + ")";
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
                    sfRenderWindow_drawSprite(((RenderWindow)target).CPointer, CPointer, ref marshaledStates);
                }
                else if (target is RenderTexture)
                {
                    sfRenderTexture_drawSprite(((RenderTexture)target).CPointer, CPointer, ref marshaledStates);
                }
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Handle the destruction of the object
            /// </summary>
            /// <param name="disposing">Is the GC disposing the object, or is it an explicit call ?</param>
            ////////////////////////////////////////////////////////////
            protected override void Destroy(bool disposing)
            {
                sfSprite_destroy(CPointer);
            }

            private Texture myTexture = null;

            #region Imports

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfSprite_create();

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfSprite_copy(IntPtr Sprite);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSprite_destroy(IntPtr CPointer);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSprite_setColor(IntPtr CPointer, Color Color);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern Color sfSprite_getColor(IntPtr CPointer);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfRenderWindow_drawSprite(IntPtr CPointer, IntPtr Sprite, ref RenderStates.MarshalData states);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfRenderTexture_drawSprite(IntPtr CPointer, IntPtr Sprite, ref RenderStates.MarshalData states);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSprite_setTexture(IntPtr CPointer, IntPtr Texture, bool AdjustToNewSize);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSprite_setTextureRect(IntPtr CPointer, IntRect Rect);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntRect sfSprite_getTextureRect(IntPtr CPointer);

            [DllImport("csfml-graphics-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern FloatRect sfSprite_getLocalBounds(IntPtr CPointer);

            #endregion
        }
    }
}
