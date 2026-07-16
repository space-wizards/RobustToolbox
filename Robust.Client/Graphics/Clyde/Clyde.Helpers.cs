using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Shared.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using ES20 = OpenToolkit.Graphics.ES20;

namespace Robust.Client.Graphics.Clyde
{
    internal sealed partial class Clyde
    {
        private unsafe delegate* unmanaged<int, int, byte, float*, void> _glUniformMatrix3fv;

        private void GLClearColor(Color color)
        {
            GL.ClearColor(color.R, color.G, color.B, color.A);
            CheckGlError();
        }

        private void SetTexture(TextureUnit unit, Texture texture)
        {
            var ct = (ClydeTexture) texture;
            SetTexture(unit, ct.TextureId);
            CheckGlError();
        }

        private void SetTexture(TextureUnit unit, ClydeHandle textureId)
        {
            var glHandle = _loadedTextures[textureId].OpenGLObject;
            GL.ActiveTexture(unit);
            CheckGlError();
            GL.BindTexture(TextureTarget.Texture2D, glHandle.Handle);
            CheckGlError();
            GL.ActiveTexture(TextureUnit.Texture0);
        }

        private void CopyRenderTextureToTexture(RenderTexture source, ClydeTexture target) {
            LoadedRenderTarget sourceLoaded = RtToLoaded(source);
            bool pause = sourceLoaded != _currentBoundRenderTarget;
            FullStoredRendererState? store = null;
            if (pause) {
                store = PushRenderStateFull();
                BindRenderTargetFull(sourceLoaded);
                CheckGlError();
            }

            GL.BindTexture(TextureTarget.Texture2D, _loadedTextures[target.TextureId].OpenGLObject.Handle);
            CheckGlError();
            GL.CopyTexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 0, 0, source.Size.X, source.Size.Y);
            CheckGlError();

            if (pause && store != null) {
                PopRenderStateFull((FullStoredRendererState)store);
            }
        }

        private static long EstPixelSize(PixelInternalFormat format)
        {
            return format switch
            {
                PixelInternalFormat.Rgba8 => 4,
                PixelInternalFormat.Rgba16f => 8,
                PixelInternalFormat.Srgb8Alpha8 => 4,
                PixelInternalFormat.R11fG11fB10f => 4,
                PixelInternalFormat.R32f => 4,
                PixelInternalFormat.Rg32f => 8,
                PixelInternalFormat.R8 => 1,
                _ => 0
            };
        }

        // Sets up uniforms (It'd be nice to move this, or make some contextual stuff implicit, but things got complicated.)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupGlobalUniformsImmediate(GLShaderProgram program, ClydeTexture? tex)
        {
            SetupGlobalUniformsImmediate(program, tex?.IsSrgb ?? false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupGlobalUniformsImmediate(GLShaderProgram program, bool texIsSrgb)
        {
            ProjViewUBO.Apply(program);
            UniformConstantsUBO.Apply(program);
            if (!_hasGLSrgb)
            {
                program.SetUniformMaybe("SRGB_EMU_CONFIG",
                    new Vector2(texIsSrgb ? 1 : 0, _currentBoundRenderTarget.IsSrgb ? 1 : 0));
            }
        }

        // Gets the primitive type required by QuadBatchIndexWrite.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BatchPrimitiveType GetQuadBatchPrimitiveType()
        {
            return _hasGLPrimitiveRestart ? BatchPrimitiveType.TriangleFan : BatchPrimitiveType.TriangleList;
        }

        // Gets the PrimitiveType version of GetQuadBatchPrimitiveType
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PrimitiveType GetQuadGLPrimitiveType()
        {
            return _hasGLPrimitiveRestart ? PrimitiveType.TriangleFan : PrimitiveType.Triangles;
        }

        // Gets the amount of indices required by QuadBatchIndexWrite.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetQuadBatchIndexCount()
        {
            // PR: Need 5 indices per quad: 4 to draw the quad with triangle strips and another one as primitive restart.
            // no PR: Need 6 indices per quad: 2 triangles
            return _hasGLPrimitiveRestart ? 5 : 6;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void QuadBatchIndexWrite(Span<ushort> indexData, ref int nIdx, ushort tIdx)
        {
            QuadBatchIndexWrite(indexData, ref nIdx, tIdx, (ushort) (tIdx + 1), (ushort) (tIdx + 2),
                (ushort) (tIdx + 3));
        }

        // Writes a quad into the index buffer. Note that the 'middle line' is from tIdx0 to tIdx2.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void QuadBatchIndexWrite(
            Span<ushort> indexData,
            ref int nIdx,
            ushort tIdx0,
            ushort tIdx1,
            ushort tIdx2,
            ushort tIdx3)
        {
            var nIdxl = nIdx;
            if (_hasGLPrimitiveRestart)
            {
                // PJB's fancy triangle fan isolated to a quad with primitive restart
                indexData[nIdxl + 4] = PrimitiveRestartIndex;
                indexData[nIdxl + 3] = tIdx3;
                indexData[nIdxl + 2] = tIdx2;
                indexData[nIdxl + 1] = tIdx1;
                indexData[nIdxl + 0] = tIdx0;
                nIdx += 5;
            }
            else
            {
                // 20kdc's boring two separate triangles
                indexData[nIdxl + 5] = tIdx3;
                indexData[nIdxl + 4] = tIdx2;
                indexData[nIdxl + 3] = tIdx0;
                indexData[nIdxl + 2] = tIdx2;
                indexData[nIdxl + 1] = tIdx1;
                indexData[nIdxl + 0] = tIdx0;
                nIdx += 6;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckGlError([CallerFilePath] string? path = null, [CallerLineNumber] int line = default)
        {
            if (!_checkGLErrors)
            {
                return;
            }

            // Separate method to reduce code footprint and improve inlining of this method.
            CheckGlErrorInternal(path, line);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CheckGlErrorInternal(string? path, int line)
        {
            var err = GL.GetError();
            if (err != ErrorCode.NoError)
            {
                _sawmillOgl.Error($"OpenGL error: {err} at {path}:{line}\n{Environment.StackTrace}");
            }
        }

        // Both access and mask are specified because I like prematurely optimizing and this is the most performant.
        // And easiest.
        private unsafe void* MapFullBuffer(BufferTarget buffer, int length, BufferAccess access, BufferAccessMask mask)
        {
            DebugTools.Assert(HasGLAnyMapBuffer);

            void* ptr;

            if (_hasGLMapBufferRange)
            {
                ptr = (void*) GL.MapBufferRange(buffer, IntPtr.Zero, length, mask);
                CheckGlError();
            }
            else if (_hasGLMapBuffer)
            {
                ptr = (void*) GL.MapBuffer(buffer, access);
                CheckGlError();
            }
            else
            {
                DebugTools.Assert(_hasGLMapBufferOes);

                ptr = (void*) ES20.GL.Oes.MapBuffer((ES20.BufferTargetArb) buffer,
                    (ES20.BufferAccessArb) BufferAccess.ReadOnly);
                CheckGlError();
            }

            return ptr;
        }

        private void UnmapBuffer(BufferTarget buffer)
        {
            DebugTools.Assert(HasGLAnyMapBuffer);

            if (_hasGLMapBufferRange || _hasGLMapBuffer)
            {
                GL.UnmapBuffer(buffer);
                CheckGlError();
            }
            else
            {
                DebugTools.Assert(_hasGLMapBufferOes);

                ES20.GL.Oes.UnmapBuffer((ES20.BufferTarget) buffer);
                CheckGlError();
            }
        }

        private uint GenVertexArray()
        {
            DebugTools.Assert(HasGLAnyVertexArrayObjects);

            int value;
            if (_hasGLVertexArrayObject)
            {
                value = GL.GenVertexArray();
                CheckGlError();
            }
            else
            {
                DebugTools.Assert(_hasGLVertexArrayObjectOes);

                value = ES20.GL.Oes.GenVertexArray();
                CheckGlError();
            }

            return (uint) value;
        }

        private void BindVertexArray(uint vao)
        {
            DebugTools.Assert(HasGLAnyVertexArrayObjects);

            if (_hasGLVertexArrayObject)
            {
                GL.BindVertexArray(vao);
                CheckGlError();
            }
            else
            {
                DebugTools.Assert(_hasGLVertexArrayObjectOes);

                ES20.GL.Oes.BindVertexArray(vao);
                CheckGlError();
            }
        }

        private void DeleteVertexArray(uint vao)
        {
            DebugTools.Assert(HasGLAnyVertexArrayObjects);

            if (_hasGLVertexArrayObject)
            {
                GL.DeleteVertexArray(vao);
                CheckGlError();
            }
            else
            {
                DebugTools.Assert(_hasGLVertexArrayObjectOes);

                ES20.GL.Oes.DeleteVertexArray(vao);
                CheckGlError();
            }
        }

        private nint LoadGLProc(string name)
        {
            var proc = _glBindingsContext.GetProcAddress(name);
            if (proc == IntPtr.Zero || proc == new IntPtr(1) || proc == new IntPtr(2))
            {
                throw new InvalidOperationException($"Unable to load GL function '{name}'!");
            }

            return proc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void UniformMatrix3fv(int location, in Matrix3x2 value)
        {
            // OpenTK's pointer overload allocates a RuntimePtr on every call in debug builds.
            // Bypass the binding for this hot path until OpenTK is upgraded.
            var func = _glUniformMatrix3fv;
            if (func == null)
            {
                func = (delegate* unmanaged<int, int, byte, float*, void>) LoadGLProc("glUniformMatrix3fv");
                _glUniformMatrix3fv = func;
            }

            // We put the rows of the input matrix into the columns of our GPU matrices.
            // This transpose is required, as in C#, we premultiply vectors with matrices
            // (vM) while GL postmultiplies vectors with matrices (Mv); however, since
            // Matrix3x2 is stored row-major and GL uses column-major, the memory layout
            // is the same apart from Matrix3x2's implicit column.
            float* matrix = stackalloc float[9]
            {
                value.M11, value.M12, 0,
                value.M21, value.M22, 0,
                value.M31, value.M32, 1
            };

            func(location, 1, 0, matrix);
        }
    }
}
