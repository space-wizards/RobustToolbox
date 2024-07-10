using System.Numerics;
using OpenToolkit.Graphics.OpenGL4;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Robust.Shared.Maths;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Robust.Client.Graphics.Clyde
{
    // Contains various layout/rendering structs used inside Clyde.
    internal partial class Clyde
    {
        /// <summary>
        ///     Sets up VAO layout for Vertex2D for base and raw shader types.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void SetupVAOLayout()
        {
            // Vertex Coords
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, sizeof(Vertex2D), 0);
            GL.EnableVertexAttribArray(0);
            // Texture Coords.
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, sizeof(Vertex2D), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            // Texture Coords (2).
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, sizeof(Vertex2D), 4 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            // Colour Modulation.
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, sizeof(Vertex2D), 6 * sizeof(float));
            GL.EnableVertexAttribArray(3);
        }

        // NOTE: This is:
        // + Directly cast from DrawVertexUV2DColor!!!
        // + GLContextWindow does it's own thing with this for winblit, be careful!
        [StructLayout(LayoutKind.Sequential)]
        [PublicAPI]
        private readonly struct Vertex2D
        {
            public readonly Vector2 Position;
            public readonly Vector2 TextureCoordinates;
            public readonly Vector2 TextureCoordinates2;
            // Note that this color is in linear space.
            public readonly Color Modulate;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vertex2D(Vector2 position, Vector2 textureCoordinates, Color modulate)
            {
                Position = position;
                TextureCoordinates = textureCoordinates;
                Modulate = modulate;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vertex2D(Vector2 position, Vector2 textureCoordinates, Vector2 textureCoordinates2, Color modulate)
            {
                Position = position;
                TextureCoordinates = textureCoordinates;
                TextureCoordinates2 = textureCoordinates2;
                Modulate = modulate;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vertex2D(float x, float y, float u, float v, float r, float g, float b, float a)
                : this(new Vector2(x, y), new Vector2(u, v), new Color(r, g, b, a))
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vertex2D(float x, float y, float u, float v, Color modulate)
                : this(new Vector2(x, y), new Vector2(u, v), modulate)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vertex2D(Vector2 position, float u, float v, Color modulate)
                : this(position, new Vector2(u, v), modulate)
            {
            }

            public override string ToString()
            {
                return $"Vertex2D: {Position}, {TextureCoordinates}, {Modulate}";
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = 28 * sizeof(float))]
        [PublicAPI]
        private struct ProjViewMatrices : IAppliableUniformSet
        {
            [FieldOffset(0 * sizeof(float))] public Vector3 ProjMatrixC0;
            [FieldOffset(4 * sizeof(float))] public Vector3 ProjMatrixC1;
            [FieldOffset(8 * sizeof(float))] public Vector3 ProjMatrixC2;

            [FieldOffset(12 * sizeof(float))] public Vector3 ViewMatrixC0;
            [FieldOffset(16 * sizeof(float))] public Vector3 ViewMatrixC1;
            [FieldOffset(20 * sizeof(float))] public Vector3 ViewMatrixC2;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ProjViewMatrices(in Matrix3x2 projMatrix, in Matrix3x2 viewMatrix)
            {
                // We put the rows of the input matrix into the columns of our GPU matrices
                // this transpose is required, as in C#, we premultiply vectors with matrices
                // (vM) while GL postmultiplies vectors with matrices (Mv); however, since
                // the Matrix3x2 data is stored row-major, and GL uses column-major, the
                // memory layout is the same (or would be, if Matrix3x2 didn't have an
                // implicit column)
                ProjMatrixC0 = new Vector3(projMatrix.M11, projMatrix.M12, 0);
                ProjMatrixC1 = new Vector3(projMatrix.M21, projMatrix.M22, 0);
                ProjMatrixC2 = new Vector3(projMatrix.M31, projMatrix.M32, 1);

                ViewMatrixC0 = new Vector3(viewMatrix.M11, viewMatrix.M12, 0);
                ViewMatrixC1 = new Vector3(viewMatrix.M21, viewMatrix.M22, 0);
                ViewMatrixC2 = new Vector3(viewMatrix.M31, viewMatrix.M32, 1);
            }

            public void Apply(Clyde clyde, GLShaderProgram program)
            {
                program.SetUniformMaybe("projectionMatrix", new Matrix3x2(
                    ProjMatrixC0.X, ProjMatrixC0.Y, // Implicit 0
                    ProjMatrixC1.X, ProjMatrixC1.Y, // Implicit 0
                    ProjMatrixC2.X, ProjMatrixC2.Y  // Implicit 1
                ));
                program.SetUniformMaybe("viewMatrix", new Matrix3x2(
                    ViewMatrixC0.X, ViewMatrixC0.Y, // Implicit 0
                    ViewMatrixC1.X, ViewMatrixC1.Y, // Implicit 0
                    ViewMatrixC2.X, ViewMatrixC2.Y  // Implicit 1
                ));
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = sizeof(float) * 4)]
        [PublicAPI]
        private struct UniformConstants : IAppliableUniformSet
        {
            [FieldOffset(0)] public Vector2 ScreenPixelSize;
            [FieldOffset(2 * sizeof(float))] public float Time;

            public UniformConstants(Vector2 screenPixelSize, float time)
            {
                ScreenPixelSize = screenPixelSize;
                Time = time;
            }

            public void Apply(Clyde clyde, GLShaderProgram program)
            {
                program.SetUniformMaybe("SCREEN_PIXEL_SIZE", ScreenPixelSize);
                program.SetUniformMaybe("TIME", Time);
            }
        }
    }
}
