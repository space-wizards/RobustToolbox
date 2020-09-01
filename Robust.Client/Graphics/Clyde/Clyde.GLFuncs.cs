using System;
using System.Runtime.InteropServices;
using OpenToolkit.Graphics.OpenGL4;

namespace Robust.Client.Graphics.Clyde
{
    internal sealed partial class Clyde
    {
        // OpenTK doesn't have these extension functions for some reason so let's work around that.

        private unsafe delegate void* GLMapBufferOes(BufferTarget buffer, BufferAccess access);
        private delegate byte GLUnmapBufferOes(BufferTarget target);

        private GLMapBufferOes _glMapBufferOes = default!;
        private GLUnmapBufferOes _glUnmapBufferOes = default!;

        private void InitMissingGLFunctions()
        {
            // ReSharper disable once CommentTypo
            // If GL_OES_mapbuffer is the only way we have to map buffers (god please why) then load it.
            if (_hasGLMapBufferOes && !(_hasGLMapBuffer || _hasGLMapBufferRange))
            {
                LoadProc(ref _glMapBufferOes, "glMapBufferOES");
                LoadProc(ref _glUnmapBufferOes, "glUnmapBufferOES");
            }

            void LoadProc<T>(ref T field, string name) where T : Delegate
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
}
