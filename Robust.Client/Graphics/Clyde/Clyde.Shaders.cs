using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenTK.Graphics.OpenGL;
using Robust.Client.Graphics.Shaders;
using Robust.Client.ResourceManagement.ResourceTypes;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using StencilOp = Robust.Client.Graphics.Shaders.StencilOp;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private ClydeShaderInstance _defaultShader;

        private string _shaderWrapCodeSpriteFrag;
        private string _shaderWrapCodeSpriteVert;

        private string _shaderWrapCodeModelFrag;
        private string _shaderWrapCodeModelVert;

        private readonly Dictionary<ClydeHandle, LoadedShader> _loadedShaders =
            new Dictionary<ClydeHandle, LoadedShader>();

        private readonly Dictionary<ClydeHandle, LoadedShaderInstance> _shaderInstances =
            new Dictionary<ClydeHandle, LoadedShaderInstance>();

        private readonly ConcurrentQueue<ClydeHandle> _deadShaderInstances = new ConcurrentQueue<ClydeHandle>();

        private ShaderProgram _lightShader;

        private class LoadedShader
        {
            public ShaderProgram Program;
            public bool HasLighting = true;
            public ShaderBlendMode BlendMode;
        }

        private class LoadedShaderInstance
        {
            public ClydeHandle ShaderHandle;

            // TODO(perf): Maybe store these parameters not boxed with a tagged union.
            public readonly Dictionary<string, object> Parameters = new Dictionary<string, object>();

            public StencilParameters Stencil = StencilParameters.Default;
        }

        public ClydeHandle LoadShader(ParsedShader shader, string name = null)
        {
            var vertexSource = shader.Kind == ShaderKind.Model ? _shaderWrapCodeModelVert : _shaderWrapCodeSpriteVert;
            var fragmentSource = shader.Kind == ShaderKind.Model ? _shaderWrapCodeModelFrag : _shaderWrapCodeSpriteFrag;

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
            var handle = AllocRid();
            _loadedShaders.Add(handle, loaded);
            return handle;
        }

        public ShaderInstance InstanceShader(ClydeHandle handle)
        {
            var newHandle = AllocRid();
            var loaded = new LoadedShaderInstance
            {
                ShaderHandle = handle
            };
            var instance = new ClydeShaderInstance(newHandle, this);
            _shaderInstances.Add(newHandle, loaded);
            return instance;
        }

        private void _loadStockShaders()
        {
            _shaderWrapCodeSpriteFrag = _readFile("/Shaders/Internal/sprite.frag");
            _shaderWrapCodeSpriteVert = _readFile("/Shaders/Internal/sprite.vert");

            var defaultLoadedShader = _resourceCache
                .GetResource<ShaderSourceResource>("/Shaders/Internal/default-sprite.swsl").ClydeHandle;

            _defaultShader = (ClydeShaderInstance)InstanceShader(defaultLoadedShader);

            _shaderWrapCodeModelFrag = _readFile("/Shaders/Internal/model.frag");
            _shaderWrapCodeModelVert = _readFile("/Shaders/Internal/model.vert");

            _queuedShader = _defaultShader.Handle;

            var lightVert = _readFile("/Shaders/Internal/light.vert");
            var lightFrag = _readFile("/Shaders/Internal/light.frag");

            _lightShader = _compileProgram(lightVert, lightFrag, "_lightShader");
        }

        private string _readFile(string path)
        {
            using var reader = new StreamReader(_resourceCache.ContentFileRead(path), EncodingHelpers.UTF8);
            return reader.ReadToEnd();
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

        private void ClearDeadShaderInstances()
        {
            while (_deadShaderInstances.TryDequeue(out var handle))
            {
                _shaderInstances.Remove(handle);
            }
        }

        private sealed class ClydeShaderInstance : ShaderInstance
        {
            public readonly ClydeHandle Handle;
            public readonly Clyde Parent;

            public ClydeShaderInstance(ClydeHandle handle, Clyde parent)
            {
                Handle = handle;
                Parent = parent;
            }

            private protected override ShaderInstance DuplicateImpl()
            {
                var instanceData = Parent._shaderInstances[Handle];
                var newData = new LoadedShaderInstance
                {
                    ShaderHandle = instanceData.ShaderHandle
                };

                foreach (var (name, value) in instanceData.Parameters)
                {
                    newData.Parameters.Add(name, value);
                }

                newData.Stencil = instanceData.Stencil;

                var newHandle = Parent.AllocRid();
                Parent._shaderInstances.Add(newHandle, newData);
                return new ClydeShaderInstance(newHandle, Parent);
            }

            protected override void Dispose(bool disposing)
            {
                Parent._deadShaderInstances.Enqueue(Handle);
            }

            // TODO: Verify that parameters actually exist before assigning them like this.

            private protected override void SetParameterImpl(string name, float value)
            {
                var data = Parent._shaderInstances[Handle];
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, Vector2 value)
            {
                var data = Parent._shaderInstances[Handle];
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, Vector3 value)
            {
                var data = Parent._shaderInstances[Handle];
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, Vector4 value)
            {
                var data = Parent._shaderInstances[Handle];
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, Color value)
            {
                var data = Parent._shaderInstances[Handle];
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, int value)
            {
                var data = Parent._shaderInstances[Handle];
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, Vector2i value)
            {
                var data = Parent._shaderInstances[Handle];
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, bool value)
            {
                var data = Parent._shaderInstances[Handle];
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, in Matrix3 value)
            {
                var data = Parent._shaderInstances[Handle];
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, in Matrix4 value)
            {
                var data = Parent._shaderInstances[Handle];
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, Texture value)
            {
                throw new NotImplementedException();
            }

            private protected override void SetStencilOpImpl(StencilOp op)
            {
                var data = Parent._shaderInstances[Handle];
                data.Stencil.Op = op;
            }

            private protected override void SetStencilFuncImpl(StencilFunc func)
            {
                var data = Parent._shaderInstances[Handle];
                data.Stencil.Func = func;
            }

            private protected override void SetStencilTestEnabledImpl(bool enabled)
            {
                var data = Parent._shaderInstances[Handle];
                data.Stencil.Enabled = enabled;
            }

            private protected override void SetStencilRefImpl(int @ref)
            {
                var data = Parent._shaderInstances[Handle];
                data.Stencil.Ref = @ref;
            }

            private protected override void SetStencilWriteMaskImpl(int mask)
            {
                var data = Parent._shaderInstances[Handle];
                data.Stencil.WriteMask = mask;
            }

            private protected override void SetStencilReadMaskRefImpl(int mask)
            {
                var data = Parent._shaderInstances[Handle];
                data.Stencil.ReadMask = mask;
            }
        }

        private struct StencilParameters
        {
            public static readonly StencilParameters Default = new StencilParameters
            {
                Enabled = false,
                Ref = 0,
                Op = StencilOp.Keep,
                Func = StencilFunc.Always,
                ReadMask = unchecked((int)uint.MaxValue),
                WriteMask = unchecked((int)uint.MaxValue),
            };

            public bool Enabled;
            public int Ref;
            public int WriteMask;
            public int ReadMask;
            public StencilOp Op;
            public StencilFunc Func;
        }
    }
}
