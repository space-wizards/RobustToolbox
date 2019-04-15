using OpenTK.Graphics.OpenGL4;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private class Shader
        {
            private readonly Clyde _clyde;

            public Shader(Clyde clyde, ShaderType type, string shaderSource, string name=null)
            {
                _clyde = clyde;
                Compile(type, shaderSource);
                if (name != null)
                {
                    _clyde._objectLabelMaybe(ObjectLabelIdentifier.Shader, Handle, name);
                }
            }

            public int Handle { get; private set; } = -1;
            public ShaderType Type { get; private set; }

            private void Compile(ShaderType type, string shaderSource)
            {
                Handle = GL.CreateShader(type);
                Type = type;
                GL.ShaderSource(Handle, shaderSource);
                GL.CompileShader(Handle);

                GL.GetShader(Handle, ShaderParameter.CompileStatus, out var compiled);
                if (compiled != 1)
                {
                    var message = GL.GetShaderInfoLog(Handle);
                    Delete();
                    throw new ShaderCompilationException(message);
                }
            }

            public void Delete()
            {
                if (Handle == -1)
                {
                    return;
                }
                GL.DeleteShader(Handle);
                Handle = -1;
            }
        }
    }
}
