using OpenTK.Graphics.OpenGL4;

namespace SS14.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private class Shader
        {
            public Shader(ShaderType type, string shaderSource)
            {
                Compile(type, shaderSource);
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
                    Delete();
                    throw new ShaderCompilationException(GL.GetShaderInfoLog(Handle));
                }
            }

            public void Delete()
            {
                GL.DeleteShader(Handle);
                Handle = -1;
            }
        }
    }
}
