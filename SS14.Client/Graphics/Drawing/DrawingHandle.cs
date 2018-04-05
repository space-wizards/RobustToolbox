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
        internal Godot.RID Item { get; private set; }

        internal DrawingHandle(Godot.RID item)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
        }

        public void Dispose()
        {
            Item = null;
        }

        public void DrawCircle(Vector2 position, float radius, Color color)
        {
            VS.CanvasItemAddCircle(Item, position.Convert(), radius, color.Convert());
        }

        public void DrawStyleBox(StyleBox styleBox, Box2 box)
        {
            styleBox.GodotStyleBox.Draw(Item, box.Convert());
        }

        public void DrawLine(Vector2 from, Vector2 to, Color color, float width = 1, bool antialiased = false)
        {
            VS.CanvasItemAddLine(Item, from.Convert(), to.Convert(), color.Convert(), width, antialiased);
        }

        public void DrawRect(Box2 rect, Color color, bool filled = true)
        {
            if (filled)
            {
                VS.CanvasItemAddRect(Item, rect.Convert(), color.Convert());
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
            texture.GodotTexture.Draw(Item, position.Convert(), modulate?.Convert(), false, normalMap);
        }

        public void DrawTextureRect(Texture texture, Box2 rect, bool tile, Color? modulate = null, bool transpose = false, Texture normalMap = null)
        {
            texture.GodotTexture.DrawRect(Item, rect.Convert(), tile, modulate?.Convert(), transpose, normalMap);
        }

        public void SetTransform(Vector2 position, Angle rotation, Vector2 scale)
        {
            var transform = Godot.Transform2D.Identity.Scaled(scale.Convert()).Rotated((float)rotation.Theta).Translated(position.Convert());
            VS.CanvasItemAddSetTransform(Item, transform);
        }

        public void SetTransform(Matrix3 matrix)
        {
            VS.CanvasItemAddSetTransform(Item, matrix.Convert());
        }
    }
}
