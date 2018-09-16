using System;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.Utility;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using Color = SS14.Shared.Maths.Color;
using Vector2 = SS14.Shared.Maths.Vector2;
using VS = Godot.VisualServer;

namespace SS14.Client.Graphics.Drawing
{
    /// <summary>
    ///     Used for doing direct drawing without entities/nodes/controls.
    /// </summary>
    public abstract class DrawingHandle : IDisposable
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

        public void SetTransform(Vector2 position, Angle rotation, Vector2 scale)
        {
            CheckDisposed();
            var transform = Godot.Transform2D.Identity.Rotated((float) rotation.Theta).Scaled(scale.Convert());
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
        public static void SetTransform2DRotationAndScale(ref Godot.Transform2D transform, double rot, Vector2 scale)
        {
            transform.x = new Godot.Vector2((float) Math.Cos(rot), (float) Math.Sin(rot)) * scale.X;
            transform.y = new Godot.Vector2(-(float) Math.Sin(rot), (float) Math.Cos(rot)) * scale.Y;
        }

        protected void CheckDisposed()
        {
            if (Item == null)
            {
                throw new ObjectDisposedException(nameof(DrawingHandle));
            }
        }

        public abstract void DrawCircle(Vector2 position, float radius, Color color);

        public abstract void DrawLine(Vector2 @from, Vector2 to, Color color, float width = 1,
            bool antiAliased = false);

        public abstract void DrawTexture(Texture texture, Vector2 position, Color? modulate = null,
            Texture normalMap = null);
    }

    public sealed class DrawingHandleWorld : DrawingHandle
    {
        private const int PPM = EyeManager.PIXELSPERMETER;

        internal DrawingHandleWorld(Godot.RID item) : base(item)
        {
        }

        public override void DrawCircle(Vector2 position, float radius, Color color)
        {
            CheckDisposed();
            VS.CanvasItemAddCircle(Item, ToPixelCoords(position), radius * PPM, color.Convert());
        }

        public override void DrawLine(Vector2 from, Vector2 to, Color color, float width = 1, bool antiAliased = false)
        {
            CheckDisposed();
            VS.CanvasItemAddLine(Item, ToPixelCoords(from), ToPixelCoords(to), color.Convert(), width, antiAliased);
        }

        public override void DrawTexture(Texture texture, Vector2 position, Color? modulate = null,
            Texture normalMap = null)
        {
            CheckDisposed();
            texture.GodotTexture.Draw(Item, ToPixelCoords(position), modulate?.Convert(), false, normalMap);
        }

        public void DrawTextureRect(Texture texture, Box2 rect, bool tile, Color? modulate = null,
            bool transpose = false, Texture normalMap = null)
        {
            CheckDisposed();
            texture.GodotTexture.DrawRect(Item, ToPixelCoords(rect), tile, modulate?.Convert(), transpose, normalMap);
        }

        public void DrawRect(Box2 rect, Color color, bool filled = true)
        {
            CheckDisposed();
            if (filled)
            {
                VS.CanvasItemAddRect(Item, ToPixelCoords(rect), color.Convert());
            }
            else
            {
                DrawLine(rect.TopLeft, rect.TopRight, color);
                DrawLine(rect.TopRight, rect.BottomRight, color);
                DrawLine(rect.BottomRight, rect.BottomLeft, color);
                DrawLine(rect.BottomLeft, rect.TopLeft, color);
            }
        }

        public void DrawStyleBox(StyleBox styleBox, UIBox2 box)
        {
            CheckDisposed();
            styleBox.GodotStyleBox.Draw(Item, box.Convert());
        }

        private static Godot.Vector2 ToPixelCoords(Vector2 vec)
        {
            return (vec * new Vector2(1, -1) * PPM).Convert();
        }

        private static Godot.Rect2 ToPixelCoords(Box2 box)
        {
            return new Godot.Rect2(box.Left * PPM, -box.Top * PPM, box.Width * PPM, box.Height * PPM);
        }
    }

    public sealed class DrawingHandleScreen : DrawingHandle
    {
        internal DrawingHandleScreen(Godot.RID item) : base(item)
        {
        }

        public override void DrawCircle(Vector2 position, float radius, Color color)
        {
            CheckDisposed();
            VS.CanvasItemAddCircle(Item, position.Convert(), radius, color.Convert());
        }

        public void DrawStyleBox(StyleBox styleBox, UIBox2 box)
        {
            CheckDisposed();
            styleBox.GodotStyleBox.Draw(Item, box.Convert());
        }

        public override void DrawLine(Vector2 from, Vector2 to, Color color, float width = 1, bool antiAliased = false)
        {
            CheckDisposed();
            VS.CanvasItemAddLine(Item, from.Convert(), to.Convert(), color.Convert(), width, antiAliased);
        }

        public void DrawRect(UIBox2 rect, Color color, bool filled = true)
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

        public override void DrawTexture(Texture texture, Vector2 position, Color? modulate = null,
            Texture normalMap = null)
        {
            CheckDisposed();
            texture.GodotTexture.Draw(Item, position.Convert(), modulate?.Convert(), false, normalMap);
        }

        public void DrawTextureRect(Texture texture, UIBox2 rect, bool tile, Color? modulate = null,
            bool transpose = false, Texture normalMap = null)
        {
            CheckDisposed();
            texture.GodotTexture.DrawRect(Item, rect.Convert(), tile, modulate?.Convert(), transpose, normalMap);
        }
    }
}
