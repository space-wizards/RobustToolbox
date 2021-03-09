using System;
using System.Diagnostics;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
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

        public void SetTransform(in Vector2 position, in Angle rotation, in Vector2 scale)
        {
            CheckDisposed();

            var matrix = Matrix3.CreateTransform(in position, in rotation, in scale);
            SetTransform(in matrix);
        }

        public void SetTransform(in Vector2 position, in Angle rotation)
        {
            var matrix = Matrix3.CreateTransform(in position, in rotation);
            SetTransform(in matrix);
        }

        public abstract void SetTransform(in Matrix3 matrix);

        public abstract void UseShader(ShaderInstance? shader);

        /// <summary>
        ///     Draws arbitrary geometry primitives with a flat color.
        /// </summary>
        /// <param name="primitiveTopology">The topology of the primitives to draw.</param>
        /// <param name="vertices">The set of vertices to render.</param>
        /// <param name="color">The color to draw with.</param>
        public abstract void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, ReadOnlySpan<Vector2> vertices,
            Color color);

        /// <summary>
        ///     Draws arbitrary indexed geometry primitives with a flat color.
        /// </summary>
        /// <param name="primitiveTopology">The topology of the primitives to draw.</param>
        /// <param name="indices">The indices into <paramref name="vertices"/> to render.</param>
        /// <param name="vertices">The set of vertices to render.</param>
        /// <param name="color">The color to draw with.</param>
        public abstract void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, ReadOnlySpan<ushort> indices,
            ReadOnlySpan<Vector2> vertices, Color color);

        /// <summary>
        ///     Draws arbitrary geometry primitives with a texture.
        /// </summary>
        /// <param name="primitiveTopology">The topology of the primitives to draw.</param>
        /// <param name="texture">The texture to render with.</param>
        /// <param name="vertices">The set of vertices to render.</param>
        /// <param name="color">The color to draw with.</param>
        public abstract void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, Texture texture,
            ReadOnlySpan<DrawVertexUV2D> vertices, Color? color = null);

        /// <summary>
        ///     Draws arbitrary geometry primitives with a flat color.
        /// </summary>
        /// <param name="primitiveTopology">The topology of the primitives to draw.</param>
        /// <param name="texture">The texture to render with.</param>
        /// <param name="indices">The indices into <paramref name="vertices"/> to render.</param>
        /// <param name="vertices">The set of vertices to render.</param>
        /// <param name="color">The color to draw with.</param>
        public abstract void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, Texture texture,
            ReadOnlySpan<ushort> indices,
            ReadOnlySpan<DrawVertexUV2D> vertices, Color? color = null);

        [DebuggerStepThrough]
        protected void CheckDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(DrawingHandleBase));
            }
        }

        public abstract void DrawCircle(Vector2 position, float radius, Color color, bool filled = true);

        public abstract void DrawLine(Vector2 from, Vector2 to, Color color);
    }

    /// <summary>
    ///     2D Vertex that contains both position and UV coordinates.
    /// </summary>
    public struct DrawVertexUV2D
    {
        public Vector2 Position;
        public Vector2 UV;

        public DrawVertexUV2D(Vector2 position, Vector2 uv)
        {
            Position = position;
            UV = uv;
        }
    }
}
