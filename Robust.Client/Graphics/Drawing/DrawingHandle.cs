using System;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Graphics.Clyde;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Utility;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Drawing
{
    /// <summary>
    ///     Used for doing direct drawing without sprite components, existing GUI controls, etc...
    /// </summary>
    public abstract class DrawingHandle : IDisposable
    {
        private protected IRenderHandle _renderHandle;
        private protected readonly int _handleId;
        public bool Disposed { get; private set; }
        public Color Modulate { get; set; } = Color.White;

        internal DrawingHandle()
        {
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
        }

        public void SetTransform(in Matrix3 matrix)
        {
            CheckDisposed();
            _renderHandle?.SetModelTransform(matrix, _handleId);
        }

        internal void UseShader(Shader shader)
        {
            CheckDisposed();
            _renderHandle?.UseShader(shader, _handleId);
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

        internal DrawingHandleWorld(IRenderHandle handle, int handleId) : base(handle, handleId)
        {
        }

        internal DrawingHandleWorld()
        {
        }

        public override void DrawCircle(Vector2 position, float radius, Color color)
        {
            CheckDisposed();
            // TODO: Implement this.
        }

        public override void DrawLine(Vector2 from, Vector2 to, Color color, float width = 1, bool antiAliased = false)
        {
            CheckDisposed();
            if (_renderHandle != null)
            {
                _renderHandle.DrawLine(from, to, color, _handleId);
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
        }

        public void DrawRect(Box2 rect, Color color, bool filled = true)
        {
            CheckDisposed();
            color *= Modulate;
            if (filled && _renderHandle != null)
            {
                _renderHandle.DrawTextureRect(Texture.White, rect.BottomLeft, rect.TopRight, color, null,
                    _handleId);
            }
            else
            {
                DrawLine(rect.TopLeft, rect.TopRight, color);
                DrawLine(rect.TopRight, rect.BottomRight, color);
                DrawLine(rect.BottomRight, rect.BottomLeft, color);
                DrawLine(rect.BottomLeft, rect.TopLeft, color);
            }
        }
    }

    public sealed class DrawingHandleScreen : DrawingHandle
    {
        internal DrawingHandleScreen(IRenderHandle handle, int handleId) : base(handle, handleId)
        {
        }

        internal DrawingHandleScreen() : base()
        {
        }


        public override void DrawCircle(Vector2 position, float radius, Color color)
        {
            CheckDisposed();
            // TODO: Implement this.
        }

        public void DrawStyleBox(StyleBox styleBox, UIBox2 box)
        {
            if (styleBox == null)
            {
                throw new ArgumentNullException(nameof(styleBox));
            }

            CheckDisposed();
            styleBox.Draw(this, box);
        }

        public override void DrawLine(Vector2 from, Vector2 to, Color color, float width = 1, bool antiAliased = false)
        {
            CheckDisposed();
            if (_renderHandle != null)
            {
                _renderHandle.DrawLine(from, to, color, _handleId);
            }
        }

        public void DrawRect(UIBox2 rect, Color color, bool filled = true)
        {
            CheckDisposed();
            color *= Modulate;
            if (filled && _renderHandle != null)
            {
                    _renderHandle.DrawTextureRect(Texture.White, rect.TopLeft, rect.BottomRight, color, null,
                        _handleId);
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

            _renderHandle?.DrawTextureRect(texture, position, position + texture.Size, actualModulate, null,
                _handleId);
        }

        public void DrawTextureRect(Texture texture, UIBox2 rect, bool tile, Color? modulate = null,
            bool transpose = false, Texture normalMap = null)
        {
            CheckDisposed();
            var actualModulate = (modulate ?? Color.White) * Modulate;
            _renderHandle?.DrawTextureRect(texture, rect.TopLeft, rect.BottomRight, actualModulate, null, _handleId);
        }

        public void DrawTextureRectRegion(Texture texture, UIBox2 rect, UIBox2 subRegion, Color? modulate = null)
        {
            CheckDisposed();
            var actualModulate = (modulate ?? Color.White) * Modulate;
            _renderHandle?.DrawTextureRect(texture, rect.TopLeft, rect.BottomRight, actualModulate, subRegion,
                _handleId);
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
