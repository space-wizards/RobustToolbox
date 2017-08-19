using OpenTK;
using SFML.System;
using SFML.Graphics.Glsl;
using SS14.Client.Graphics.Render;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        public void SetUniform(string Parameter, RenderImage Image)
        {
            base.SetUniform(Parameter, Image.Texture);
        }

        public void SetUniform(string Parameter, Vector2f vec2)
        {
            base.SetUniform(Parameter, vec2);
        }

        public void SetUniform(string Parameter, Vector2 vec2)
        {
            base.SetUniform(Parameter, vec2.Convert());
        }

        public void SetUniform(string Parameter, Vector3f vec3)
        {
            base.SetUniform(Parameter, vec3);
        }

        public void SetUniform(string Parameter, Vector3 vec3)
        {
            base.SetUniform(Parameter, vec3.Convert());
        }

        public void SetUniform(string Parameter, Vector4 vec4)
        {
            base.SetUniform(Parameter, new Vec4(vec4.X, vec4.Y, vec4.Z, vec4.W));
        }

        public void SetUniformArray(string Parameter, Vector2f[] vec2array)
        {
            SetUniformArray(Parameter, vec2array.Select(v => new Vec2(v.X, v.Y)).ToArray());
        }

        public void SetUniformArray(string Parameter, Vector3f[] vec3array)
        {
            SetUniformArray(Parameter, vec3array.Select(v => new Vec3(v.X, v.Y, v.Z)).ToArray());
        }

        public void SetUniformArray(string Parameter, Vector4[] vec4array)
        {
            SetUniformArray(Parameter, vec4array.Select(v => new Vec4(v.X, v.Y, v.Z, v.W)).ToArray());
        }

        public void SetUniformArray(string Parameter, Vector2[] vec2array)
        {
            SetUniformArray(Parameter, vec2array.Select(v => new Vec2(v.X, v.Y)).ToArray());
        }

        public void SetUniformArray(string Parameter, Vector3[] vec3array)
        {
            SetUniformArray(Parameter, vec3array.Select(v => new Vec3(v.X, v.Y, v.Z)).ToArray());
        }
    }
}
