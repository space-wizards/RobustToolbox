using OpenTK;
using SFML.System;
using SS14.Client.Graphics.Render;
using SS14.Shared.Maths;
using System.Diagnostics;
using System.IO;
using ShaderClass = SFML.Graphics.Shader;


namespace SS14.Client.Graphics.Shader
{
    [DebuggerDisplay("[GLSLShader] ResourceName = {_resourceName} | availible? {isAvalible}")]
    public class GLSLShader : ShaderClass
    {

        private string _resourceName;



        public GLSLShader(string vertexShaderFilename, string fragmentShaderFilename)
            : base(vertexShaderFilename, null, fragmentShaderFilename)
        {

        }

        public GLSLShader(Stream vertexShaderStream, Stream fragmentShaderStream)
            : base(vertexShaderStream, null, fragmentShaderStream)
        {

        }

        public string ResourceName
        {
            get { return _resourceName; }
            set { _resourceName = value; }
        }

        public void setAsCurrentShader()
        {
            CluwneLib.CurrentShader = this;
        }

        public void ResetCurrentShader()
        {
            CluwneLib.CurrentShader = null;
        }

        public void setDuration(float duration)
        {

        }

        public void SetParameter(string Parameter, RenderImage Image)
        {
            base.SetParameter(Parameter, Image.Texture);
        }

        public void SetParameter(string Parameter, Vector3f vec3)
        {
            base.SetParameter(Parameter, vec3.X, vec3.Y, vec3.Z);

        }

        public void SetParameter(string Parameter, Vector4 vec4)
        {
            base.SetParameter(Parameter, vec4.X, vec4.Y, vec4.Z, vec4.W);
        }


        public void SetParameter(string Parameter, Vector2f[] vec2array)
        {
            for (int i = 0; i < vec2array.Length; i++)
            {
                this.SetParameter(Parameter + i, vec2array[i]);
            }
        }

        public void SetParameter(string Parameter, Vector3f[] vec3array)
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
    }
}
