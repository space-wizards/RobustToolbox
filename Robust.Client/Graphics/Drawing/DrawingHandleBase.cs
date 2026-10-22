using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Used for doing direct drawing without sprite components, existing GUI controls, etc...
    /// </summary>
    public abstract class DrawingHandleBase : IDisposable
    {
        private protected readonly int _handleId;
        public bool Disposed { get; private set; }

        /// <summary>
        ///     Drawing commands that do NOT receive per-vertex modulation get modulated by this.
        ///     Specifically, *DrawPrimitives w/ DrawVertexUV2DColor IS NOT AFFECTED BY THIS*.
        ///     The only code that should ever be setting this is UserInterfaceManager.
        ///     It's absolutely evil statefulness.
        ///     I understand it's existence and operation.
        ///     I understand that removing it would require rewriting all the UI controls everywhere.
        ///     I still wish it a prolonged death - it's a performance nightmare. - 20kdc
        /// </summary>
        public Color Modulate { get; set; } = Color.White;

        protected Texture White;

        public DrawingHandleBase(Texture white)
        {
            White = white;
        }

        public void Dispose()
        {
            Disposed = true;
        }

        public void SetTransform(in Vector2 position, in Angle rotation, in Vector2 scale)
        {
            CheckDisposed();

            var matrix = Matrix3Helpers.CreateTransform(in position, in rotation, in scale);
            SetTransform(in matrix);
        }

        public void SetTransform(in Vector2 position, in Angle rotation)
        {
            var matrix = Matrix3Helpers.CreateTransform(in position, in rotation);
            SetTransform(in matrix);
        }

        public abstract void SetTransform(in Matrix3x2 matrix);

        public abstract Matrix3x2 GetTransform();

        public abstract void UseShader(ShaderInstance? shader);

        public abstract ShaderInstance? GetShader();

        // ---- DrawPrimitives: Vector2 API ----

        /// <summary>
        ///     Draws arbitrary geometry primitives with a flat color.
        /// </summary>
        /// <param name="primitiveTopology">The topology of the primitives to draw.</param>
        /// <param name="vertices">The list of vertices to render.</param>
        /// <param name="color">The color to draw with.</param>
        public void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, List<Vector2> vertices,
            Color color)
        {
            var span = CollectionsMarshal.AsSpan(vertices);
            DrawPrimitives(primitiveTopology, span, color);
        }

        /// <summary>
        ///     Draws arbitrary geometry primitives with a flat color.
        /// </summary>
        /// <param name="primitiveTopology">The topology of the primitives to draw.</param>
        /// <param name="vertices">The set of vertices to render.</param>
        /// <param name="color">The color to draw with.</param>
        public void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, ReadOnlySpan<Vector2> vertices,
            Color color)
        {
            var realColor = color * Modulate;

            // TODO: Maybe don't stackalloc if the data is too large.
            Span<DrawVertexUV2DColor> drawVertices = stackalloc DrawVertexUV2DColor[vertices.Length];
            PadVerticesV2(vertices, drawVertices, realColor);

            DrawPrimitives(primitiveTopology, White, drawVertices);
        }

        /// <summary>
        ///     Draws arbitrary indexed geometry primitives with a flat color.
        /// </summary>
        /// <param name="primitiveTopology">The topology of the primitives to draw.</param>
        /// <param name="indices">The indices into <paramref name="vertices"/> to render.</param>
        /// <param name="vertices">The set of vertices to render.</param>
        /// <param name="color">The color to draw with.</param>
        public void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, ReadOnlySpan<ushort> indices,
            ReadOnlySpan<Vector2> vertices, Color color)
        {
            var realColor = color * Modulate;

            // TODO: Maybe don't stackalloc if the data is too large.
            Span<DrawVertexUV2DColor> drawVertices = stackalloc DrawVertexUV2DColor[vertices.Length];
            PadVerticesV2(vertices, drawVertices, realColor);

            DrawPrimitives(primitiveTopology, White, indices, drawVertices);
        }

        private void PadVerticesV2(ReadOnlySpan<Vector2> input, Span<DrawVertexUV2DColor> output, Color color)
        {
            Color colorLinear = Color.FromSrgb(color);
            for (var i = 0; i < output.Length; i++)
            {
                output[i] = new DrawVertexUV2DColor(input[i], new Vector2(0.5f, 0.5f), colorLinear);
            }
        }

        // ---- DrawPrimitives: DrawVertexUV2D API ----

        /// <summary>
        ///     Draws arbitrary geometry primitives with a texture.
        /// </summary>
        /// <param name="primitiveTopology">The topology of the primitives to draw.</param>
        /// <param name="texture">The texture to render with.</param>
        /// <param name="vertices">The set of vertices to render.</param>
        /// <param name="color">The color to draw with.</param>
        public void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, Texture texture, ReadOnlySpan<DrawVertexUV2D> vertices,
            Color? color = null)
        {
            var realColor = (color ?? Color.White) * Modulate;

            // TODO: Maybe don't stackalloc if the data is too large.
            Span<DrawVertexUV2DColor> drawVertices = stackalloc DrawVertexUV2DColor[vertices.Length];
            PadVerticesUV(vertices, drawVertices, realColor);

            DrawPrimitives(primitiveTopology, texture, drawVertices);
        }

        /// <summary>
        ///     Draws arbitrary geometry primitives with a texture.
        /// </summary>
        /// <param name="primitiveTopology">The topology of the primitives to draw.</param>
        /// <param name="texture">The texture to render with.</param>
        /// <param name="vertices">The set of vertices to render.</param>
        /// <param name="indices">The indices into <paramref name="vertices"/> to render.</param>
        /// <param name="color">The color to draw with.</param>
        public void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, Texture texture, ReadOnlySpan<ushort> indices,
            ReadOnlySpan<DrawVertexUV2D> vertices, Color? color = null)
        {
            var realColor = (color ?? Color.White) * Modulate;

            // TODO: Maybe don't stackalloc if the data is too large.
            Span<DrawVertexUV2DColor> drawVertices = stackalloc DrawVertexUV2DColor[vertices.Length];
            PadVerticesUV(vertices, drawVertices, realColor);

            DrawPrimitives(primitiveTopology, texture, indices, drawVertices);
        }

        private void PadVerticesUV(ReadOnlySpan<DrawVertexUV2D> input, Span<DrawVertexUV2DColor> output, Color color)
        {
            Color colorLinear = Color.FromSrgb(color);
            for (var i = 0; i < output.Length; i++)
            {
                output[i] = new DrawVertexUV2DColor(input[i], colorLinear);
            }
        }

        // ---- End wrappers ----

        /// <summary>
        ///     Draws arbitrary geometry primitives with a texture.
        ///     Be aware that this ignores the Modulate property! Apply it yourself if necessary.
        /// </summary>
        /// <param name="primitiveTopology">The topology of the primitives to draw.</param>
        /// <param name="texture">The texture to render with.</param>
        /// <param name="vertices">The set of vertices to render.</param>
        public abstract void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, Texture texture,
            ReadOnlySpan<DrawVertexUV2DColor> vertices);

        /// <summary>
        ///     Draws arbitrary geometry primitives with a flat color.
        ///     Be aware that this ignores the Modulate property! Apply it yourself if necessary.
        /// </summary>
        /// <param name="primitiveTopology">The topology of the primitives to draw.</param>
        /// <param name="texture">The texture to render with.</param>
        /// <param name="indices">The indices into <paramref name="vertices"/> to render.</param>
        /// <param name="vertices">The set of vertices to render.</param>
        public abstract void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, Texture texture,
            ReadOnlySpan<ushort> indices,
            ReadOnlySpan<DrawVertexUV2DColor> vertices);

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

        public abstract void RenderInRenderTarget(IRenderTarget target, Action a, Color? clearColor);

        public abstract void DrawTexture(Texture texture, Vector2 position, Color? modulate = null);
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

    /// <summary>
    ///     2D Vertex that contains position and UV coordinates, and a modulation colour (Linear!!!)
    ///     NOTE: This is directly cast into Clyde Vertex2D!!!!
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DrawVertexUV2DColor
    {
        public Vector2 Position;
        public Vector2 UV;
        public Vector2 UV2;

        /// <summary>
        ///     Modulation colour for this vertex.
        ///     Note that this color is in linear space.
        /// </summary>
        public Color Color;

        /// <param name="position">The location.</param>
        /// <param name="uv">The texture coordinate.</param>
        /// <param name="col">Modulation colour (In linear space, use Color.FromSrgb if needed)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DrawVertexUV2DColor(Vector2 position, Vector2 uv, Color col)
        {
            Position = position;
            UV = uv;
            Color = col;
        }

        /// <param name="position">The location.</param>
        /// <param name="col">Modulation colour (In linear space, use Color.FromSrgb if needed)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DrawVertexUV2DColor(Vector2 position, Color col)
        {
            Position = position;
            UV = new Vector2(0.5f, 0.5f);
            Color = col;
        }

        /// <param name="b">The existing position/UV pair.</param>
        /// <param name="col">Modulation colour (In linear space, use Color.FromSrgb if needed)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DrawVertexUV2DColor(DrawVertexUV2D b, Color col)
        {
            Position = b.Position;
            UV = b.UV;
            Color = col;
        }
    }
}
