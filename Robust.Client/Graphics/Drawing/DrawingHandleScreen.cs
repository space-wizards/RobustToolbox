using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Drawing
{
    public abstract class DrawingHandleScreen : DrawingHandleBase
    {
        public abstract void DrawRect(UIBox2 rect, Color color, bool filled = true);

        public abstract void DrawTextureRectRegion(Texture texture, UIBox2 rect, UIBox2? subRegion = null, Color? modulate = null);

        public void DrawTexture(Texture texture, Vector2 position, Color? modulate = null)
        {
            CheckDisposed();

            DrawTextureRect(texture, UIBox2.FromDimensions(position, texture.Size), modulate);
        }

        public void DrawTextureRect(Texture texture, UIBox2 rect, Color? modulate = null)
        {
            CheckDisposed();

            DrawTextureRectRegion(texture, rect, null, modulate);
        }
    }
}
