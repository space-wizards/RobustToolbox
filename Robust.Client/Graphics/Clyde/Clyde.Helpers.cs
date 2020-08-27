using System;
using System.Runtime.CompilerServices;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Shared.Log;
using Robust.Shared.Maths;

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
    }
}
