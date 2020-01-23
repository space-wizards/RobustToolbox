using Robust.Client.Graphics.ClientEye;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Drawing
{
    public abstract class DrawingHandleWorld : DrawingHandleBase
    {
        private const int Ppm = EyeManager.PIXELSPERMETER;

        public abstract void DrawRect(Box2 rect, Color color, bool filled = true);
        public abstract void DrawRect(in Box2Rotated rect, Color color, bool filled = true);

        public abstract void DrawTextureRectRegion(Texture texture, Box2 rect, UIBox2? subRegion = null,
            Color? modulate = null);

        public abstract void DrawTextureRectRegion(Texture texture, in Box2Rotated rect, UIBox2? subRegion = null,
            Color? modulate = null);

        public void DrawTexture(Texture texture, Vector2 position, Color? modulate = null)
        {
            CheckDisposed();

            DrawTextureRect(texture, Box2.FromDimensions(position, texture.Size / (float) Ppm), modulate);
        }

        public void DrawTextureRect(Texture texture, Box2 rect, Color? modulate = null)
        {
            CheckDisposed();

            DrawTextureRectRegion(texture, rect, null, modulate);
        }

        public void DrawTextureRect(Texture texture, in Box2Rotated rect, Color? modulate = null)
        {
            CheckDisposed();

            DrawTextureRectRegion(texture, rect, null, modulate);
        }
    }
}
