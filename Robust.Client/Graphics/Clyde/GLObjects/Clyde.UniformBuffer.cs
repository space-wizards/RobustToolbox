using System.Runtime.CompilerServices;
using OpenToolkit.Graphics.OpenGL4;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        /// <summary>
        ///     Represents some set of uniforms that can be backed by a uniform buffer or by regular uniforms.
        ///     Implies you're using this on a properly setup struct.
        /// </summary>
        private interface IAppliableUniformSet
        {
            /// <summary>
            ///     Applies the uniform set directly to a program.
            /// </summary>
            void Apply(Clyde clyde, GLShaderProgram program);
        }

        /// <summary>
        ///     Represents some set of uniforms that can be backed by a uniform buffer or by regular uniforms.
        /// </summary>
        private class GLUniformBuffer<T> where T : unmanaged, IAppliableUniformSet
        {
            private readonly Clyde _clyde;

            /// <summary>
            ///     GPU Buffer (only used when uniform buffers are available)
            /// </summary>
            private GLBuffer? _implUBO = null;

            /// <summary>
            ///     Mirror (only used when uniform buffers are unavailable)
            /// </summary>
            private T _implMirror;

            public GLUniformBuffer(Clyde clyde, int index, string? name = null)
            {
                _clyde = clyde;
                if (_clyde._hasGLUniformBuffers)
                {
                    _implUBO = new GLBuffer(_clyde, BufferTarget.UniformBuffer, BufferUsageHint.StreamDraw, name);
                    unsafe {
                        _implUBO.Reallocate(sizeof(T));
                    }
                    GL.BindBufferBase(BufferRangeTarget.UniformBuffer, index, (int) _implUBO.ObjectHandle);
                }
            }

            /// <summary>
            ///     Updates the buffer contents.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reallocate(in T data)
            {
                if (_implUBO != null)
                {
                    _implUBO.Reallocate(data);
                }
                else
                {
                    _implMirror = data;
                }
            }

            /// <summary>
            ///     This is important for use on GLES2 - it ensures the uniforms in the specific program are up-to-date.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Apply(GLShaderProgram program)
            {
                if (_implUBO == null)
                {
                    _implMirror.Apply(_clyde, program);
                }
            }
        }
    }
}
