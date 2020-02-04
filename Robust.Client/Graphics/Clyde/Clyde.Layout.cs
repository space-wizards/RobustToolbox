using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Clyde
{
    // Contains various layout/rendering structs used inside Clyde.
    internal partial class Clyde
    {
        [StructLayout(LayoutKind.Sequential)]
        [PublicAPI]
        private readonly struct Vertex2D
        {
            public static readonly int SizeOf;

            public readonly Vector2 Position;
            public readonly Vector2 TextureCoordinates;

            static Vertex2D()
            {
                unsafe
                {
                    SizeOf = sizeof(Vertex2D);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vertex2D(Vector2 position, Vector2 textureCoordinates)
            {
                Position = position;
                TextureCoordinates = textureCoordinates;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vertex2D(float x, float y, float u, float v)
                : this(new Vector2(x, y), new Vector2(u, v))
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vertex2D(Vector2 position, float u, float v)
                : this(position, new Vector2(u, v))
            {
            }

            public override string ToString()
            {
                return $"Vertex2D: {Position}, {TextureCoordinates}";
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = 28 * sizeof(float))]
        [PublicAPI]
        private struct ProjViewMatrices
        {
            [FieldOffset(0 * sizeof(float))] public Vector3 ProjMatrixC0;
            [FieldOffset(4 * sizeof(float))] public Vector3 ProjMatrixC1;
            [FieldOffset(8 * sizeof(float))] public Vector3 ProjMatrixC2;

            [FieldOffset(12 * sizeof(float))] public Vector3 ViewMatrixC0;
            [FieldOffset(16 * sizeof(float))] public Vector3 ViewMatrixC1;
            [FieldOffset(20 * sizeof(float))] public Vector3 ViewMatrixC2;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ProjViewMatrices(in Matrix3 projMatrix, in Matrix3 viewMatrix)
            {
                ProjMatrixC0 = new Vector3(projMatrix.R0C0, projMatrix.R1C0, projMatrix.R2C0);
                ProjMatrixC1 = new Vector3(projMatrix.R0C1, projMatrix.R1C1, projMatrix.R2C1);
                ProjMatrixC2 = new Vector3(projMatrix.R0C2, projMatrix.R1C2, projMatrix.R2C2);

                ViewMatrixC0 = new Vector3(viewMatrix.R0C0, viewMatrix.R1C0, viewMatrix.R2C0);
                ViewMatrixC1 = new Vector3(viewMatrix.R0C1, viewMatrix.R1C1, viewMatrix.R2C1);
                ViewMatrixC2 = new Vector3(viewMatrix.R0C2, viewMatrix.R1C2, viewMatrix.R2C2);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ProjViewMatrices(in ProjViewMatrices readProjMatrix, in Matrix3 viewMatrix)
            {
                ProjMatrixC0 = readProjMatrix.ProjMatrixC0;
                ProjMatrixC1 = readProjMatrix.ProjMatrixC1;
                ProjMatrixC2 = readProjMatrix.ProjMatrixC2;

                ViewMatrixC0 = new Vector3(viewMatrix.R0C0, viewMatrix.R1C0, viewMatrix.R2C0);
                ViewMatrixC1 = new Vector3(viewMatrix.R0C1, viewMatrix.R1C1, viewMatrix.R2C1);
                ViewMatrixC2 = new Vector3(viewMatrix.R0C2, viewMatrix.R1C2, viewMatrix.R2C2);
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = sizeof(float) * 4)]
        [PublicAPI]
        private struct UniformConstants
        {
            [FieldOffset(0)] public Vector2 ScreenPixelSize;
            [FieldOffset(2 * sizeof(float))] public float Time;

            public UniformConstants(Vector2 screenPixelSize, float time)
            {
                ScreenPixelSize = screenPixelSize;
                Time = time;
            }
        }
    }
}
