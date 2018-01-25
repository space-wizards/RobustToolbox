using SFML.System;
using SFML.Graphics.Glsl;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Utility;
using SS14.Shared.Maths;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ShaderClass = SFML.Graphics.Shader;
using System; //TODO: Remove when all NotImplementedExcpetions are removed


namespace SS14.Client.Graphics.Shader
{
    [DebuggerDisplay("[GLSLShader] ResourceName = {_resourceName} | availible? {isAvalible}")]
    public class GLSLShader : IDisposable
    {
        public ShaderClass SFMLShader { get; }

        /// <summary>
        /// Class used to trick C# into calling a different overload.
        /// </summary>
        public class CurrentTextureType { }
        public static readonly CurrentTextureType CurrentTexture = null;

        public GLSLShader(string vertexShaderFilename, string fragmentShaderFilename)
        {
            SFMLShader = new ShaderClass(vertexShaderFilename, null, fragmentShaderFilename);
        }

        public GLSLShader(Stream vertexShaderStream, Stream fragmentShaderStream)
        {
            SFMLShader = new ShaderClass(vertexShaderStream, null, fragmentShaderStream);
        }

        public void Dispose() => SFMLShader.Dispose();

        public string ResourceName { get; set; }

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
            throw new NotImplementedException();
        }

        public void SetUniform(string Parameter, CurrentTextureType dummy)
        {
            SFMLShader.SetUniform(Parameter, ShaderClass.CurrentTexture);
        }

        public void SetUniform(string Parameter, float x)
        {
            SFMLShader.SetUniform(Parameter, x);
        }

        public void SetUniform(string Parameter, int x)
        {
            SFMLShader.SetUniform(Parameter, x);
        }

        public void SetUniform(string Parameter, bool x)
        {
            SFMLShader.SetUniform(Parameter, x);
        }

        public void SetUniform(string Parameter, Textures.Texture texture)
        {
            SFMLShader.SetUniform(Parameter, texture.SFMLTexture);
        }

        public void SetUniform(string Parameter, RenderImage Image)
        {
            SFMLShader.SetUniform(Parameter, Image.Texture.SFMLTexture);
        }

        internal void SetUniform(string Parameter, Vector2f vec2)
        {
            SFMLShader.SetUniform(Parameter, vec2);
        }

        public void SetUniform(string Parameter, Vector2 vec2)
        {
            SFMLShader.SetUniform(Parameter, vec2.Convert());
        }

        internal void SetUniform(string Parameter, Vector3f vec3)
        {
            SFMLShader.SetUniform(Parameter, vec3);
        }

        public void SetUniform(string Parameter, Vector3 vec3)
        {
            SFMLShader.SetUniform(Parameter, new Vec3(vec3.X, vec3.Y, vec3.Z));
        }

        public void SetUniform(string Parameter, Vector4 vec4)
        {
            SFMLShader.SetUniform(Parameter, new Vec4(vec4.X, vec4.Y, vec4.Z, vec4.W));
        }

        internal void SetUniformArray(string Parameter, Vector2f[] vec2array)
        {
            SFMLShader.SetUniformArray(Parameter, vec2array.Select(v => (Vec2)v).ToArray());
        }

        internal void SetUniformArray(string Parameter, Vector3f[] vec3array)
        {
            SFMLShader.SetUniformArray(Parameter, vec3array.Select(v => (Vec3)v).ToArray());
        }

        public void SetUniformArray(string Parameter, Vector4[] vec4array)
        {
            SFMLShader.SetUniformArray(Parameter, vec4array.Select(v => new Vec4(v.X, v.Y, v.Z, v.W)).ToArray());
        }

        public void SetUniformArray(string Parameter, Vector2[] vec2array)
        {
            SFMLShader.SetUniformArray(Parameter, vec2array.Select(v => new Vec2(v.X, v.Y)).ToArray());
        }

        public void SetUniformArray(string Parameter, Vector3[] vec3array)
        {
            SFMLShader.SetUniformArray(Parameter, vec3array.Select(v => new Vec3(v.X, v.Y, v.Z)).ToArray());
        }
    }
}
