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
            CheckDisposed();
            VS.CanvasItemAddCircle(Item, position.Convert(), radius, color.Convert());
        }

        public void DrawStyleBox(StyleBox styleBox, Box2 box)
        {
            CheckDisposed();
            styleBox.GodotStyleBox.Draw(Item, box.Convert());
        }

        public void DrawLine(Vector2 from, Vector2 to, Color color, float width = 1, bool antialiased = false)
        {
            CheckDisposed();
            VS.CanvasItemAddLine(Item, from.Convert(), to.Convert(), color.Convert(), width, antialiased);
        }

        public void DrawRect(Box2 rect, Color color, bool filled = true)
        {
            CheckDisposed();
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
            CheckDisposed();
            texture.GodotTexture.Draw(Item, position.Convert(), modulate?.Convert(), false, normalMap);
        }

        public void DrawTextureRect(Texture texture, Box2 rect, bool tile, Color? modulate = null, bool transpose = false, Texture normalMap = null)
        {
            CheckDisposed();
            texture.GodotTexture.DrawRect(Item, rect.Convert(), tile, modulate?.Convert(), transpose, normalMap);
        }

        public void SetTransform(Vector2 position, Angle rotation, Vector2 scale)
        {
            CheckDisposed();
            var transform = Godot.Transform2D.Identity.Rotated((float)rotation.Theta).Scaled(scale.Convert());
            SetTransform2DRotationAndScale(ref transform, rotation.Theta, scale);
            transform.o = position.Convert();
            VS.CanvasItemAddSetTransform(Item, transform);
        }

        public void SetTransform(Matrix3 matrix)
        {
            CheckDisposed();
            VS.CanvasItemAddSetTransform(Item, matrix.Convert());
        }

        // Effectively equivalent to Godot's internal Transform2D::set_rotation_and_scale defined in math_2d.h.
        private static void SetTransform2DRotationAndScale(ref Godot.Transform2D transform, double rot, Vector2 scale)
        {
            transform.x = new Godot.Vector2((float)Math.Cos(rot), (float)Math.Sin(rot)) * scale.X;
            transform.y = new Godot.Vector2(-(float)Math.Sin(rot), (float)Math.Cos(rot)) * scale.Y;
        }

        private void CheckDisposed()
        {
            if (Item == null)
            {
                throw new ObjectDisposedException(nameof(DrawingHandle));
            }
        }
    }
}
