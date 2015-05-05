using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SFML.Graphics;
using ShaderClass = SFML.Graphics.Shader;
using SS14.Client.Graphics.Render;

namespace SS14.Client.Graphics.Shader
{
    public class GLSLShader : ShaderClass
    {
        private string _resourceName;
        private MemoryStream _memStream;



        public GLSLShader(string vertexShaderFilename, string fragmentShaderFilename) : base(vertexShaderFilename, fragmentShaderFilename)
        {

        }

        public GLSLShader(Stream vertexShaderStream, Stream fragmentShaderStream) : base(vertexShaderStream, fragmentShaderStream)
        {
        }

    

        public string ResourceName
        {
            get { return _resourceName; }
            set { _resourceName = value; }
        }

      

      

        public void setDuration(float duration)
        { 
        
        }

        public void SetParameter(string Parameter, RenderImage Image)
        {
            throw new NotImplementedException();
        }
    }
}
