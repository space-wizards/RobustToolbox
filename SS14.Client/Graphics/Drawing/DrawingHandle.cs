using System;
using SS14.Client.Utility;
using SS14.Shared.Maths;
using VS = Godot.VisualServer;

namespace SS14.Client.Graphics.Drawing
{
    /// <summary>
    ///     Used for doing direct drawing without entities/nodes/controls.
    /// </summary>
    public sealed class DrawingHandle : IDisposable
    {
        // Use RIDs in the theoretical case some nerd wants to draw something WITHOUT consulting the scene tree.
        // Also it's probably faster or some shit.
        internal Godot.RID item { get; private set; }

        internal DrawingHandle(Godot.RID item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            this.item = item;
        }

        public void Dispose()
        {
            if (item == null)
            {
                return;
            }
            item = null;
        }

        public void DrawCircle(Vector2 position, float radius, Color color)
        {
            VS.CanvasItemAddCircle(item, position.Convert(), radius, color.Convert());
        }

        public void DrawStyleBox(StyleBox styleBox, Box2 box)
        {
            styleBox.GodotStyleBox.Draw(item, box.Convert());
        }

        public void DrawLine(Vector2 from, Vector2 to, Color color, float width = 1, bool antialiased = false)
        {
            VS.CanvasItemAddLine(item, from.Convert(), to.Convert(), color.Convert(), width, antialiased);
        }

        public void DrawRect(Box2 rect, Color color, bool filled = true)
        {
            if (filled)
            {
                VS.CanvasItemAddRect(item, rect.Convert(), color.Convert());
            }
            else
            {
                DrawLine(rect.TopLeft, rect.TopRight, color);
                DrawLine(rect.TopRight, rect.BottomRight, color);
                DrawLine(rect.BottomRight, rect.BottomLeft, color);
                DrawLine(rect.BottomLeft, rect.TopLeft, color);
            }
        }

        public void DrawTexture(Texture texture, Vector2 position, Color? modulate = null, Texture normalMap = null)
        {
            texture.GodotTexture.Draw(item, position.Convert(), modulate?.Convert(), false, normalMap);
        }

        public void DrawTextureRect(Texture texture, Box2 rect, bool tile, Color? modulate = null, bool transpose = false, Texture normalMap = null)
        {
            texture.GodotTexture.DrawRect(item, rect.Convert(), tile, modulate?.Convert(), transpose, normalMap);
        }
    }
}
