using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SFML.Graphics;
using BaseShader = SFML.Graphics.Shader;
using SS14.Client.Graphics.Render;

namespace SS14.Client.Graphics.Shader
{
    public class FXShader : BaseShader
    {
        private string _resourceName;
        private MemoryStream _memStream;



        public FXShader(string vertexShaderFilename, string fragmentShaderFilename) : base(vertexShaderFilename, fragmentShaderFilename)
        {
        }

        public FXShader(Stream vertexShaderStream, Stream fragmentShaderStream) : base(vertexShaderStream, fragmentShaderStream)
        {
        }

        public FXShader(IntPtr ptr) : base(ptr)
        {
        }

        public string ResourceName
        {
            get { return _resourceName; }
            set { _resourceName = value; }
        }

         public MemoryStream memStream
         {
            get
            {
                return _memStream;
            }
             set
             {
                 _memStream = value;
             }

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
