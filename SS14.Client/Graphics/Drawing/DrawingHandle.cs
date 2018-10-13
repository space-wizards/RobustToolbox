using System;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.Utility;
using SS14.Shared.Maths;

#if GODOT
using VS = Godot.VisualServer;
#endif

namespace SS14.Client.Graphics.Drawing
{
    /// <summary>
    ///     Used for doing direct drawing without entities/nodes/controls.
    /// </summary>
    public abstract class DrawingHandle : IDisposable
    {
        // Use RIDs in the theoretical case some nerd wants to draw something WITHOUT consulting the scene tree.
        // Also it's probably faster or some shit.
#if GODOT
        internal Godot.RID Item { get; private set; }

        internal DrawingHandle(Godot.RID item)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
        }
#endif

        public void Dispose()
        {
#if GODOT
            Item = null;
#endif
        }

        public void SetTransform(Vector2 position, Angle rotation, Vector2 scale)
        {
#if GODOT
            CheckDisposed();
            var transform = Godot.Transform2D.Identity.Rotated((float) rotation.Theta).Scaled(scale.Convert());
            SetTransform2DRotationAndScale(ref transform, rotation.Theta, scale);
            transform.o = position.Convert();
            VS.CanvasItemAddSetTransform(Item, transform);
#endif
        }

        public void SetTransform(Matrix3 matrix)
        {
#if GODOT
            CheckDisposed();
            VS.CanvasItemAddSetTransform(Item, matrix.Convert());
#endif
        }
#if GODOT
// Effectively equivalent to Godot's internal Transform2D::set_rotation_and_scale defined in math_2d.h.
        public static void SetTransform2DRotationAndScale(ref Godot.Transform2D transform, double rot, Vector2 scale)
        {
            transform.x = new Godot.Vector2((float) Math.Cos(rot), (float) Math.Sin(rot)) * scale.X;
            transform.y = new Godot.Vector2(-(float) Math.Sin(rot), (float) Math.Cos(rot)) * scale.Y;
        }
#endif
        protected void CheckDisposed()
        {
#if GODOT
            if (Item == null)
            {
                throw new ObjectDisposedException(nameof(DrawingHandle));
            }
#endif
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

#if GODOT
        internal DrawingHandleWorld(Godot.RID item) : base(item)
        {
        }
        #endif

        public override void DrawCircle(Vector2 position, float radius, Color color)
        {
#if GODOT
            CheckDisposed();
            VS.CanvasItemAddCircle(Item, ToPixelCoords(position), radius * PPM, color.Convert());
#endif
        }

        public override void DrawLine(Vector2 from, Vector2 to, Color color, float width = 1, bool antiAliased = false)
        {
#if GODOT
            CheckDisposed();
            VS.CanvasItemAddLine(Item, ToPixelCoords(from), ToPixelCoords(to), color.Convert(), width, antiAliased);
#endif
        }

        public override void DrawTexture(Texture texture, Vector2 position, Color? modulate = null,
            Texture normalMap = null)
        {
#if GODOT
            CheckDisposed();
            texture.GodotTexture.Draw(Item, ToPixelCoords(position), modulate?.Convert(), false, normalMap);
#endif
        }

        public void DrawTextureRect(Texture texture, Box2 rect, bool tile, Color? modulate = null,
            bool transpose = false, Texture normalMap = null)
        {
#if GODOT
            CheckDisposed();
            texture.GodotTexture.DrawRect(Item, ToPixelCoords(rect), tile, modulate?.Convert(), transpose, normalMap);
#endif
        }

        public void DrawRect(Box2 rect, Color color, bool filled = true)
        {
#if GODOT
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
#endif
        }

        public void DrawStyleBox(StyleBox styleBox, UIBox2 box)
        {
#if GODOT
            CheckDisposed();
            styleBox.GodotStyleBox.Draw(Item, box.Convert());
#endif
        }
#if GODOT
        private static Godot.Vector2 ToPixelCoords(Vector2 vec)
        {
            return (vec * new Vector2(1, -1) * PPM).Convert();
        }

        private static Godot.Rect2 ToPixelCoords(Box2 box)
        {
            return new Godot.Rect2(box.Left * PPM, -box.Top * PPM, box.Width * PPM, box.Height * PPM);
        }
#endif
    }

    public sealed class DrawingHandleScreen : DrawingHandle
    {
#if GODOT
        internal DrawingHandleScreen(Godot.RID item) : base(item)
        {
        }
#endif

        public override void DrawCircle(Vector2 position, float radius, Color color)
        {
#if GODOT
            CheckDisposed();
            VS.CanvasItemAddCircle(Item, position.Convert(), radius, color.Convert());
#endif
        }

        public void DrawStyleBox(StyleBox styleBox, UIBox2 box)
        {
#if GODOT
            CheckDisposed();
            styleBox.GodotStyleBox.Draw(Item, box.Convert());
#endif
        }

        public override void DrawLine(Vector2 from, Vector2 to, Color color, float width = 1, bool antiAliased = false)
        {
#if GODOT
            CheckDisposed();
            VS.CanvasItemAddLine(Item, from.Convert(), to.Convert(), color.Convert(), width, antiAliased);
#endif
        }

        public void DrawRect(UIBox2 rect, Color color, bool filled = true)
        {
#if GODOT
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
#endif
        }

        public override void DrawTexture(Texture texture, Vector2 position, Color? modulate = null,
            Texture normalMap = null)
        {
#if GODOT
            CheckDisposed();
            texture.GodotTexture.Draw(Item, position.Convert(), modulate?.Convert(), false, normalMap);
#endif
        }

        public void DrawTextureRect(Texture texture, UIBox2 rect, bool tile, Color? modulate = null,
            bool transpose = false, Texture normalMap = null)
        {
#if GODOT
            CheckDisposed();
            texture.GodotTexture.DrawRect(Item, rect.Convert(), tile, modulate?.Convert(), transpose, normalMap);
#endif
        }
    }
}
