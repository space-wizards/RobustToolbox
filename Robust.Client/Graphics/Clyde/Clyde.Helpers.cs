using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp.PixelFormats;
using ES20 = OpenToolkit.Graphics.ES20;

namespace Robust.Client.Graphics.Clyde
{
    internal sealed partial class Clyde
    {
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
                BindRenderTargetFull(source);
                CheckGlError();
            }

            GL.BindTexture(TextureTarget.Texture2D, _loadedTextures[target.TextureId].OpenGLObject.Handle);
            CheckGlError();
            GL.CopyTexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 0, 0, _framebufferSize.X, _framebufferSize.Y);
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
                PixelInternalFormat.Rg32f => 4,
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
        private void QuadBatchIndexWrite(Span<ushort> indexData, ref int nIdx, ushort tIdx0, ushort tIdx1, ushort tIdx2,
            ushort tIdx3)
        {
            if (_hasGLPrimitiveRestart)
            {
                // PJB's fancy triangle fan isolated to a quad with primitive restart
                indexData[nIdx + 0] = tIdx0;
                indexData[nIdx + 1] = tIdx1;
                indexData[nIdx + 2] = tIdx2;
                indexData[nIdx + 3] = tIdx3;
                indexData[nIdx + 4] = PrimitiveRestartIndex;
                nIdx += 5;
            }
            else
            {
                // 20kdc's boring two separate triangles
                indexData[nIdx + 0] = tIdx0;
                indexData[nIdx + 1] = tIdx1;
                indexData[nIdx + 2] = tIdx2;
                indexData[nIdx + 3] = tIdx0;
                indexData[nIdx + 4] = tIdx2;
                indexData[nIdx + 5] = tIdx3;
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
        private static void CheckGlErrorInternal(string? path, int line)
        {
            var err = GL.GetError();
            if (err != ErrorCode.NoError)
            {
                Logger.ErrorS("clyde.ogl", $"OpenGL error: {err} at {path}:{line}\n{Environment.StackTrace}");
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

        private void LoadGLProc<T>(string name, out T field) where T : Delegate
        {
            var proc = _graphicsContext.GetProcAddress(name);
            if (proc == IntPtr.Zero || proc == new IntPtr(1) || proc == new IntPtr(2))
            {
                throw new InvalidOperationException($"Unable to load GL function '{name}'!");
            }

            field = Marshal.GetDelegateForFunctionPointer<T>(proc);
        }
    }
}
