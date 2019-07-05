using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenTK.Graphics.OpenGL;
using Robust.Client.Graphics.Shaders;
using Robust.Client.ResourceManagement.ResourceTypes;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private ClydeHandle _defaultShader;

        private string _shaderWrapCodeSpriteFrag;
        private string _shaderWrapCodeSpriteVert;

        private readonly List<LoadedShader> _loadedShaders = new List<LoadedShader>();

        private ShaderProgram _lightShader;

        private class LoadedShader
        {
            public ShaderProgram Program;
            public bool HasLighting = true;
            public ShaderBlendMode BlendMode;
        }

        public ClydeHandle LoadShader(ParsedShader shader, string name = null)
        {
            var vertexSource = _shaderWrapCodeSpriteVert;
            var fragmentSource = _shaderWrapCodeSpriteFrag;

            var (header, vertBody, fragBody) = _getShaderCode(shader);

            vertexSource = vertexSource.Replace("[SHADER_HEADER_CODE]", header);
            vertexSource = vertexSource.Replace("[SHADER_CODE]", vertBody);
            fragmentSource = fragmentSource.Replace("[SHADER_HEADER_CODE]", header);
            fragmentSource = fragmentSource.Replace("[SHADER_CODE]", fragBody);

            var program = _compileProgram(vertexSource, fragmentSource, name);

            program.BindBlock("projectionViewMatrices", ProjViewBindingIndex);
            program.BindBlock("uniformConstants", UniformConstantsBindingIndex);

            var loaded = new LoadedShader
            {
                Program = program,
                HasLighting = shader.LightMode != ShaderLightMode.Unshaded,
                BlendMode = shader.BlendMode
            };
            var ret = _loadedShaders.Count;
            _loadedShaders.Add(loaded);
            return new ClydeHandle(ret);
        }

        public ShaderInstance InstanceShader(ClydeHandle handle)
        {
            return new ClydeShaderInstance(handle);
        }

        private void _loadStockShaders()
        {
            _shaderWrapCodeSpriteFrag = _readFile("/Shaders/Internal/sprite.frag");
            _shaderWrapCodeSpriteVert = _readFile("/Shaders/Internal/sprite.vert");

            _defaultShader =
                _resourceCache.GetResource<ShaderSourceResource>("/Shaders/Internal/default-sprite.swsl").ClydeHandle;

            _currentShader = _defaultShader;

            var lightVert = _readFile("/Shaders/Internal/light.vert");
            var lightFrag = _readFile("/Shaders/Internal/light.frag");

            _lightShader = _compileProgram(lightVert, lightFrag, "_lightShader");
        }

        private string _readFile(string path)
        {
            using (var reader = new StreamReader(_resourceCache.ContentFileRead(path), EncodingHelpers.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private ShaderProgram _compileProgram(string vertexSource, string fragmentSource, string name = null)
        {
            Shader vertexShader = null;
            Shader fragmentShader = null;

            try
            {
                try
                {
                    vertexShader = new Shader(this, ShaderType.VertexShader, vertexSource);
                }
                catch (ShaderCompilationException e)
                {
                    throw new ShaderCompilationException(
                        "Failed to compile vertex shader, see inner for details.", e);
                }

                try
                {
                    fragmentShader = new Shader(this, ShaderType.FragmentShader, fragmentSource);
                }
                catch (ShaderCompilationException e)
                {
                    throw new ShaderCompilationException(
                        "Failed to compile fragment shader, see inner for details.", e);
                }

                var program = new ShaderProgram(this, name);
                program.Add(vertexShader);
                program.Add(fragmentShader);

                try
                {
                    program.Link();
                }
                catch (ShaderCompilationException e)
                {
                    program.Delete();

                    throw new ShaderCompilationException("Failed to link shaders. See inner for details.", e);
                }

                return program;
            }
            finally
            {
                vertexShader?.Delete();
                fragmentShader?.Delete();
            }
        }

        private static (string header, string vertBody, string fragBody)
            _getShaderCode(ParsedShader shader)
        {
            var header = new StringBuilder();

            foreach (var uniform in shader.Uniforms.Values)
            {
                if (uniform.DefaultValue != null)
                {
                    header.AppendFormat("uniform {0} {1} = {2};", uniform.Type.GetNativeType(), uniform.Name,
                        uniform.DefaultValue);
                }
                else
                {
                    header.AppendFormat("uniform {0} {1};", uniform.Type.GetNativeType(), uniform.Name);
                }
            }

            // TODO: Varyings.

            ShaderFunctionDefinition fragmentMain = null;
            ShaderFunctionDefinition vertexMain = null;

            foreach (var function in shader.Functions)
            {
                if (function.Name == "fragment")
                {
                    fragmentMain = function;
                    continue;
                }

                if (function.Name == "vertex")
                {
                    vertexMain = function;
                    continue;
                }

                header.AppendFormat("{0} {1}(", function.ReturnType.GetNativeType(), function.Name);
                var first = true;
                foreach (var parameter in function.Parameters)
                {
                    if (!first)
                    {
                        header.Append(", ");
                    }

                    first = false;

                    header.AppendFormat("{0} {1} {2}", parameter.Qualifiers.GetString(), parameter.Type.GetNativeType(),
                        parameter.Name);
                }

                header.AppendFormat(") {{\n{0}\n}}\n", function.Body);
            }

            return (header.ToString(), vertexMain?.Body ?? "", fragmentMain?.Body ?? "");
        }

        private sealed class ClydeShaderInstance : ShaderInstance
        {
            public readonly ClydeHandle ShaderHandle;

            public ClydeShaderInstance(ClydeHandle shaderHandle)
            {
                ShaderHandle = shaderHandle;
            }

            public override void SetParameter(string name, float value)
            {
//                throw new System.NotImplementedException();
            }

            public override void SetParameter(string name, Vector2 value)
            {
//                throw new System.NotImplementedException();
            }

            public override void SetParameter(string name, Vector3 value)
            {
//                throw new System.NotImplementedException();
            }

            public override void SetParameter(string name, Vector4 value)
            {
//                throw new System.NotImplementedException();
            }

            public override void SetParameter(string name, int value)
            {
//                throw new System.NotImplementedException();
            }

            public override void SetParameter(string name, Vector2i value)
            {
//                throw new System.NotImplementedException();
            }

            public override void SetParameter(string name, bool value)
            {
//                throw new System.NotImplementedException();
            }

            public override void SetParameter(string name, in Matrix3 matrix)
            {
//                throw new System.NotImplementedException();
            }

            public override void SetParameter(string name, in Matrix4 matrix)
            {
//                throw new System.NotImplementedException();
            }

            public override void SetParameter(string name, Texture texture)
            {
//                throw new System.NotImplementedException();
            }

            public override void Dispose()
            {
//                throw new System.NotImplementedException();
            }
        }
    }
}
