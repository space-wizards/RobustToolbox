using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SFML.Graphics;
using ShaderClass = SFML.Graphics.Shader;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Sprite;
using SS14.Shared.Maths;
using SFML.System;

namespace SS14.Client.Graphics.Shader
{
    public class GLSLShader : ShaderClass
    {
        private string _resourceName;
    


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
            base.SetParameter(Parameter, Image.Texture);
        }

        public void SetParameter(string Parameter, Vector2 vec2)
        {
           base.SetParameter(Parameter, vec2.X, vec2.Y);
        }

        public void SetParameter(string Parameter, Vector3 vec3)
        {
            base.SetParameter(Parameter, vec3.X, vec3.Y, vec3.Z);
        }
        public void SetParameter(string Parameter, Vector4 vec4)
        {
            base.SetParameter(Parameter, vec4.X, vec4.Y, vec4.Z, vec4.W);
        }
        
        public void SetParameter(string Parameter, CluwneSprite Sprite)
        {
            base.SetParameter(Parameter, Sprite.Texture);
        }

        public void SetParameter(string Parameter, Transform mat4)
        {            
            base.SetParameter(Parameter,mat4);
        }

        public void SetParameter(string Parameter, Vector2[] vec2array)
        {
            for (int i = 0; i < vec2array.Length; i++)
            {
                this.SetParameter(Parameter + i, vec2array[i]);
            }
        }

        public void SetParameter(string Parameter, Vector3[] vec3array)
        {
            for (int i = 0; i < vec3array.Length; i++)
            {
                this.SetParameter(Parameter + i, vec3array[i]);
            }
        }

        public void SetParameter(string Parameter, Vector4[] vec4array)
        {
            for (int i = 0; i < vec4array.Length; i++)
            {
               this.SetParameter(Parameter + i, vec4array[i]);
            }
        }

        public void SetParameter(string Parameter, Texture texture)
        {
            base.SetParameter(Parameter, texture);
        }
    }
}
