using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    public abstract class DrawingHandleWorld : DrawingHandleBase
    {
        private const int Ppm = EyeManager.PixelsPerMeter;

        /// <summary>
        /// Draws an untextured colored rectangle to the world.The coordinate system is right handed.
        /// Make sure to set <see cref="DrawingHandleBase.SetTransform"/>
        /// to set the model matrix if needed.
        /// </summary>
        /// <param name="rect">The four vertices of the quad in object space (or world if the transform is identity.).</param>
        /// <param name="color">Color of the rectangle.</param>
        /// <param name="filled">Is it filled with color, or just the border lines?</param>
        public abstract void DrawRect(Box2 rect, Color color, bool filled = true);

        /// <summary>
        /// Draws an untextured colored rectangle to the world.The coordinate system is right handed.
        /// Make sure to set <see cref="DrawingHandleBase.SetTransform"/>
        /// to set the model matrix if needed.
        /// </summary>
        /// <param name="rect">The four vertices of the quad in object space (or world if the transform is identity.).
        /// The rotation of the rectangle is applied before the transform matrix.</param>
        /// <param name="color">Color of the rectangle.</param>
        /// <param name="filled">Is it filled with color, or just the border lines?</param>
        public abstract void DrawRect(in Box2Rotated rect, Color color, bool filled = true);

        /// <summary>
        /// Draws a sprite to the world. The coordinate system is right handed.
        /// Make sure to set <see cref="DrawingHandleBase.SetTransform"/>
        /// to set the model matrix if needed.
        /// </summary>
        /// <param name="texture">Texture to draw.</param>
        /// <param name="quad">The four vertices of the quad in object space (or world if the transform is identity.).</param>
        /// <param name="modulate">A color to multiply the texture by when shading.</param>
        /// <param name="subRegion">The four corners of the texture sub region in px.</param>
        public abstract void DrawTextureRectRegion(Texture texture, Box2 quad,
            Color? modulate = null, UIBox2? subRegion = null);

        /// <summary>
        /// Draws a sprite to the world. The coordinate system is right handed.
        /// Make sure to set <see cref="DrawingHandleBase.SetTransform"/>
        /// to set the model matrix if needed.
        /// </summary>
        /// <param name="texture">Texture to draw.</param>
        /// <param name="quad">The four vertices of the quad in object space (or world if the transform is identity.).
        /// The rotation of the rectangle is applied before the transform matrix.</param>
        /// <param name="modulate">A color to multiply the texture by when shading.</param>
        /// <param name="subRegion">The four corners of the texture sub region in px.</param>
        public abstract void DrawTextureRectRegion(Texture texture, in Box2Rotated quad,
            Color? modulate = null, UIBox2? subRegion = null);

        /// <summary>
        /// Draws a full texture sprite to the world. The coordinate system is right handed.
        /// Make sure to set <see cref="DrawingHandleBase.SetTransform"/>
        /// to set the model matrix if needed.
        /// </summary>
        /// <param name="texture">Texture to draw.</param>
        /// <param name="position">The coordinates of the quad in object space (or world if the transform is identity.).</param>
        /// <param name="modulate">A color to multiply the texture by when shading.</param>
        /// <remarks>
        /// The sprite will have it's local dimensions calculated so that it has <see cref="EyeManager.PixelsPerMeter"/> texels per meter in the world.
        /// </remarks>
        public void DrawTexture(Texture texture, Vector2 position, Color? modulate = null)
        {
            CheckDisposed();

            DrawTextureRect(texture, Box2.FromDimensions(position, texture.Size / (float) Ppm), modulate);
        }

        /// <summary>
        /// Draws a full texture sprite to the world. The coordinate system is right handed.
        /// Make sure to set <see cref="DrawingHandleBase.SetTransform"/>
        /// to set the model matrix if needed.
        /// </summary>
        /// <param name="texture">Texture to draw.</param>
        /// <param name="quad">The four vertices of the quad in object space (or world if the transform is identity.).</param>
        /// <param name="modulate">A color to multiply the texture by when shading.</param>
        public void DrawTextureRect(Texture texture, Box2 quad, Color? modulate = null)
        {
            CheckDisposed();

            DrawTextureRectRegion(texture, quad, modulate);
        }

        /// <summary>
        /// Draws a full texture sprite to the world. The coordinate system is right handed.
        /// Make sure to set <see cref="DrawingHandleBase.SetTransform"/>
        /// to set the model matrix if needed.
        /// </summary>
        /// <param name="texture">Texture to draw.</param>
        /// <param name="quad">The four vertices of the quad in object space (or world if the transform is identity.).
        /// The rotation of the rectangle is applied before the transform matrix.</param>
        /// <param name="modulate">A color to multiply the texture by when shading.</param>
        public void DrawTextureRect(Texture texture, in Box2Rotated quad, Color? modulate = null)
        {
            CheckDisposed();

            DrawTextureRectRegion(texture, in quad, modulate);
        }
    }
}
