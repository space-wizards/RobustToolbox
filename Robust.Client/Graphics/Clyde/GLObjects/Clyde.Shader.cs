using OpenToolkit.Graphics.OpenGL4;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private class GLShader
        {
            private readonly Clyde _clyde;

            public GLShader(Clyde clyde, ShaderType type, string shaderSource, string? name=null)
            {
                _clyde = clyde;
                Compile(type, shaderSource);
                if (name != null)
                {
                    _clyde.ObjectLabelMaybe(ObjectLabelIdentifier.Shader, ObjectHandle, name);
                }
            }

            public uint ObjectHandle { get; private set; } = 0;
            public ShaderType Type { get; private set; }

            private void Compile(ShaderType type, string shaderSource)
            {
                ObjectHandle = (uint)GL.CreateShader(type);
                Type = type;
                GL.ShaderSource((int) ObjectHandle, shaderSource);
                _clyde.CheckGlError();
                GL.CompileShader(ObjectHandle);
                _clyde.CheckGlError();

                GL.GetShader(ObjectHandle, ShaderParameter.CompileStatus, out var compiled);
                _clyde.CheckGlError();
                if (compiled != 1)
                {
                    var message = GL.GetShaderInfoLog((int) ObjectHandle);
                    _clyde.CheckGlError();
                    Delete();
                    throw new ShaderCompilationException(message);
                }
            }

            public void Delete()
            {
                if (ObjectHandle == 0)
                {
                    return;
                }
                GL.DeleteShader(ObjectHandle);
                _clyde.CheckGlError();
                ObjectHandle = 0;
            }
        }
    }
}
