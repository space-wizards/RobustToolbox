using System;
using System.Diagnostics;
using Robust.Client.Graphics.Shaders;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Drawing
{
    /// <summary>
    ///     Used for doing direct drawing without sprite components, existing GUI controls, etc...
    /// </summary>
    public abstract class DrawingHandleBase : IDisposable
    {
        //private protected IRenderHandle _renderHandle;
        private protected readonly int _handleId;
        public bool Disposed { get; private set; }
        public Color Modulate { get; set; } = Color.White;

        public void Dispose()
        {
            Disposed = true;
        }

        public void SetTransform(Vector2 position, Angle rotation, Vector2 scale)
        {
            CheckDisposed();

            var matrix = Matrix3.Identity;
            (matrix.R0C0, matrix.R1C1) = scale;
            matrix.Rotate(rotation);
            matrix.R0C2 += position.X;
            matrix.R1C2 += position.Y;

            SetTransform(matrix);
        }

        public abstract void SetTransform(in Matrix3 matrix);

        public abstract void UseShader(ShaderInstance shader);

        [DebuggerStepThrough]
        protected void CheckDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(DrawingHandleBase));
            }
        }

        public abstract void DrawCircle(Vector2 position, float radius, Color color);

        public abstract void DrawLine(Vector2 from, Vector2 to, Color color);
    }
}
