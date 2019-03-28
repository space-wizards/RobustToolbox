using System;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.Graphics.Clyde;
using SS14.Client.Graphics.Shaders;
using SS14.Client.Utility;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using VS = Godot.VisualServer;

namespace SS14.Client.Graphics.Drawing
{
    /// <summary>
    ///     Used for doing direct drawing without sprite components, existing GUI controls, etc...
    /// </summary>
    public abstract class DrawingHandle : IDisposable
    {
        // Use RIDs in the theoretical case some nerd wants to draw something WITHOUT consulting the scene tree.
        // Also it's probably faster or some shit.

        internal Godot.RID Item { get; }
        private protected IRenderHandle _renderHandle;
        private protected readonly int _handleId;
        public bool Disposed { get; private set; }
        public Color Modulate { get; set; } = Color.White;

        internal DrawingHandle(Godot.RID item)
        {
            DebugTools.Assert(GameController.Mode == GameController.DisplayMode.Godot);
            Item = item ?? throw new ArgumentNullException(nameof(item));
        }

        internal DrawingHandle()
        {
            DebugTools.Assert(GameController.Mode == GameController.DisplayMode.Headless);
        }

        internal DrawingHandle(IRenderHandle handle, int handleId)
        {
            _renderHandle = handle;
            _handleId = handleId;
        }

        public void Dispose()
        {
            Disposed = true;
        }

        public void SetTransform(Vector2 position, Angle rotation, Vector2 scale)
        {
            CheckDisposed();
            if (_renderHandle != null)
            {
                var matrix = Matrix3.Identity;
                (matrix.R0C0, matrix.R1C1) = scale;
                matrix.Rotate(rotation);
                matrix.R0C2 += position.X;
                matrix.R1C2 += position.Y;
                _renderHandle.SetModelTransform(matrix, _handleId);
            }
            else if (Item != null)
            {
                var transform = Godot.Transform2D.Identity.Rotated((float) rotation.Theta).Scaled(scale.Convert());
                SetTransform2DRotationAndScale(ref transform, rotation.Theta, scale);
                transform.origin = position.Convert();
                VS.CanvasItemAddSetTransform(Item, transform);
            }
        }

        public void SetTransform(in Matrix3 matrix)
        {
            CheckDisposed();
            if (_renderHandle != null)
            {
                _renderHandle.SetModelTransform(matrix, _handleId);
            }
            else if (Item != null)
            {
                VS.CanvasItemAddSetTransform(Item, matrix.Convert());
            }
        }

        internal void UseShader(Shader shader)
        {
            CheckDisposed();
            _renderHandle?.UseShader(shader, _handleId);
        }

        // Effectively equivalent to Godot's internal Transform2D::set_rotation_and_scale defined in math_2d.h.
        internal static void SetTransform2DRotationAndScale(ref Godot.Transform2D transform, double rot, Vector2 scale)
        {
            transform.x = new Godot.Vector2((float) Math.Cos(rot), (float) Math.Sin(rot)) * scale.X;
            transform.y = new Godot.Vector2(-(float) Math.Sin(rot), (float) Math.Cos(rot)) * scale.Y;
        }

        protected void CheckDisposed()
        {
            if (Disposed)
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

        internal DrawingHandleWorld(IRenderHandle handle, int handleId) : base(handle, handleId)
        {
        }

        internal DrawingHandleWorld()
        {
        }

        public override void DrawCircle(Vector2 position, float radius, Color color)
        {
            CheckDisposed();
            if (Item != null)
            {
                VS.CanvasItemAddCircle(Item, ToPixelCoords(position), radius * PPM, (Modulate * color).Convert());
            }
        }

        public override void DrawLine(Vector2 from, Vector2 to, Color color, float width = 1, bool antiAliased = false)
        {
            CheckDisposed();
            if (_renderHandle != null)
            {
                _renderHandle.DrawLine(from, to, color, _handleId);
            }
            else if (Item != null)
            {
                VS.CanvasItemAddLine(Item, ToPixelCoords(from), ToPixelCoords(to), (Modulate * color).Convert(), width,
                    antiAliased);
            }
        }

        public override void DrawTexture(Texture texture, Vector2 position, Color? modulate = null,
            Texture normalMap = null)
        {
            CheckDisposed();

            var actualModulate = (modulate ?? Color.White) * Modulate;
            if (_renderHandle != null)
            {
                _renderHandle.DrawTextureRect(texture, position,
                    position + (texture.Size / (float) EyeManager.PIXELSPERMETER), actualModulate, null, _handleId);
            }
            else if (Item != null)
            {
                texture.GodotTexture.Draw(Item, ToPixelCoords(position), actualModulate.Convert(), false, normalMap);
            }
        }

        public void DrawTextureRect(Texture texture, Box2 rect, bool tile, Color? modulate = null,
            bool transpose = false, Texture normalMap = null)
        {
            CheckDisposed();
            var actualModulate = (modulate ?? Color.White) * Modulate;
            if (_renderHandle != null)
            {
                _renderHandle.DrawTextureRect(texture, rect.BottomLeft, rect.TopRight, actualModulate, null,
                    _handleId);
            }
            else if (Item != null)
            {
                texture.GodotTexture.DrawRect(Item, ToPixelCoords(rect), tile, actualModulate.Convert(), transpose,
                    normalMap);
            }
        }

        public void DrawRect(Box2 rect, Color color, bool filled = true)
        {
            CheckDisposed();
            color *= Modulate;
            if (filled)
            {
                if (_renderHandle != null)
                {
                    _renderHandle.DrawTextureRect(Texture.White, rect.BottomLeft, rect.TopRight, color, null,
                        _handleId);
                }
                else if (Item != null)
                {
                    VS.CanvasItemAddRect(Item, ToPixelCoords(rect), color.Convert());
                }
            }
            else
            {
                DrawLine(rect.TopLeft, rect.TopRight, color);
                DrawLine(rect.TopRight, rect.BottomRight, color);
                DrawLine(rect.BottomRight, rect.BottomLeft, color);
                DrawLine(rect.BottomLeft, rect.TopLeft, color);
            }
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

        internal DrawingHandleScreen(IRenderHandle handle, int handleId) : base(handle, handleId)
        {
        }

        internal DrawingHandleScreen() : base()
        {
        }


        public override void DrawCircle(Vector2 position, float radius, Color color)
        {
            if (!GameController.OnGodot)
            {
                return;
            }

            CheckDisposed();
            VS.CanvasItemAddCircle(Item, position.Convert(), radius, (color * Modulate).Convert());
        }

        public void DrawStyleBox(StyleBox styleBox, UIBox2 box)
        {
            if (styleBox == null)
            {
                throw new ArgumentNullException(nameof(styleBox));
            }

            CheckDisposed();
            if (GameController.OnGodot)
            {
                styleBox.GodotStyleBox.Draw(Item, box.Convert());
            }
            else
            {
                styleBox.Draw(this, box);
            }
        }

        public override void DrawLine(Vector2 from, Vector2 to, Color color, float width = 1, bool antiAliased = false)
        {
            CheckDisposed();
            if (_renderHandle != null)
            {
                _renderHandle.DrawLine(from, to, color, _handleId);
            }
            else if (Item != null)
            {
                VS.CanvasItemAddLine(Item, from.Convert(), to.Convert(), (Modulate * color).Convert(), width,
                    antiAliased);
            }
        }

        public void DrawRect(UIBox2 rect, Color color, bool filled = true)
        {
            CheckDisposed();
            color *= Modulate;
            if (filled)
            {
                if (_renderHandle != null)
                {
                    _renderHandle.DrawTextureRect(Texture.White, rect.TopLeft, rect.BottomRight, color, null,
                        _handleId);
                }
                else if (Item != null)
                {
                    VS.CanvasItemAddRect(Item, rect.Convert(), color.Convert());
                }
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

            var actualModulate = (modulate ?? Color.White) * Modulate;

            if (_renderHandle != null)
            {
                _renderHandle.DrawTextureRect(texture, position, position + texture.Size, actualModulate, null,
                    _handleId);
            }
            else if (Item != null)
            {
                texture.GodotTexture.Draw(Item, position.Convert(), actualModulate.Convert(), false, normalMap);
            }
        }

        public void DrawTextureRect(Texture texture, UIBox2 rect, bool tile, Color? modulate = null,
            bool transpose = false, Texture normalMap = null)
        {
            CheckDisposed();
            var actualModulate = (modulate ?? Color.White) * Modulate;
            if (_renderHandle != null)
            {
                _renderHandle.DrawTextureRect(texture, rect.TopLeft, rect.BottomRight, actualModulate, null, _handleId);
            }
            else if (Item != null)
            {
                texture.GodotTexture.DrawRect(Item, rect.Convert(), tile, actualModulate.Convert(), transpose,
                    normalMap);
            }
        }

        public void DrawTextureRectRegion(Texture texture, UIBox2 rect, UIBox2 subRegion, Color? modulate = null)
        {
            CheckDisposed();
            var actualModulate = (modulate ?? Color.White) * Modulate;
            if (_renderHandle != null)
            {
                _renderHandle.DrawTextureRect(texture, rect.TopLeft, rect.BottomRight, actualModulate, subRegion,
                    _handleId);
            }
            else if (Item != null)
            {
                texture.GodotTexture.DrawRect(Item, rect.Convert(), false, actualModulate.Convert());
            }
        }

        public void SetScissor(in UIBox2i? scissorBox)
        {
            CheckDisposed();
            _renderHandle?.SetScissor(scissorBox, _handleId);
            // TODO: uh... How even to go about implementing this on Godot?
        }

        internal void DrawEntity(IEntity entity, Vector2 screenPosition)
        {
            CheckDisposed();

            _renderHandle?.DrawEntity(entity, screenPosition, _handleId);
        }
    }
}
