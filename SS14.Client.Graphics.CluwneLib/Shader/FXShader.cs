using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SFML.Graphics;
using BaseShader = SFML.Graphics.Shader;

namespace SS14.Client.Graphics.CluwneLib.Shader
{
    public class FXShader : BaseShader
    {
        public FXShader(string vertexShaderFilename, string fragmentShaderFilename) : base(vertexShaderFilename, fragmentShaderFilename)
        {
        }

        public FXShader(Stream vertexShaderStream, Stream fragmentShaderStream) : base(vertexShaderStream, fragmentShaderStream)
        {
        }

        public FXShader(IntPtr ptr) : base(ptr)
        {
        }
    }
}
