using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.ResourceManagement;
using Robust.Shared.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Vector3 = Robust.Shared.Maths.Vector3;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        [ViewVariables]
        private ClydeShaderInstance _defaultShader = default!;

        private string _shaderLibrary = default!;

        private string _shaderWrapCodeDefaultFrag = default!;
        private string _shaderWrapCodeDefaultVert = default!;

        private string _shaderWrapCodeRawFrag = default!;
        private string _shaderWrapCodeRawVert = default!;

        private string _winBlitShaderVert = default!;
        private string _winBlitShaderFrag = default!;

        [ViewVariables]
        private readonly Dictionary<ClydeHandle, LoadedShader> _loadedShaders =
            new();

        [ViewVariables]
        private readonly Dictionary<ClydeHandle, LoadedShaderInstance> _shaderInstances =
            new();

        private readonly ConcurrentQueue<ClydeHandle> _deadShaderInstances = new();

        private sealed class LoadedShader
        {
            [ViewVariables]
            public GLShaderProgram Program = default!;

            [ViewVariables]
            public string? Name;

            // Last instance that used this shader.
            // Used to ensure that shader uniforms get updated.
            public LoadedShaderInstance? LastInstance;
        }

        private sealed class LoadedShaderInstance
        {
            [ViewVariables]
            public ClydeHandle ShaderHandle;

            [ViewVariables]
            public bool HasLighting;

            [ViewVariables]
            public ShaderBlendMode BlendMode;

            [ViewVariables]
            public bool ParametersDirty = true;

            // TODO(perf): Maybe store these parameters not boxed with a tagged union.
            [ViewVariables]
            public readonly Dictionary<string, object> Parameters = new();

            [ViewVariables]
            public StencilParameters Stencil;

            public LoadedShaderInstance()
            {
            }

            public LoadedShaderInstance(LoadedShaderInstance toClone)
            {
                ShaderHandle = toClone.ShaderHandle;
                HasLighting = toClone.HasLighting;
                BlendMode = toClone.BlendMode;
                Stencil = toClone.Stencil;
                Parameters = toClone.Parameters.ShallowClone();
            }
        }

        public ClydeHandle LoadShader(ParsedShader shader, string? name = null, Dictionary<string,string>? defines = null)
        {
            var (vertBody, fragBody) = GetShaderCode(shader);

            var program = _compileProgram(vertBody, fragBody, BaseShaderAttribLocations, name, defines: defines);

            if (_hasGLUniformBuffers)
            {
                program.BindBlock(UniProjViewMatrices, BindingIndexProjView);
                program.BindBlock(UniUniformConstants, BindingIndexUniformConstants);
            }

            var loaded = new LoadedShader
            {
                Program = program,
                Name = name
            };
            var handle = AllocRid();
            _loadedShaders.Add(handle, loaded);
            return handle;
        }

        public void ReloadShader(ClydeHandle handle, ParsedShader newShader)
        {
            var loaded = _loadedShaders[handle];

            var (vertBody, fragBody) = GetShaderCode(newShader);

            var program = _compileProgram(vertBody, fragBody, BaseShaderAttribLocations, loaded.Name);

            loaded.Program.Delete();

            loaded.Program = program;

            if (_hasGLUniformBuffers)
            {
                program.BindBlock(UniProjViewMatrices, BindingIndexProjView);
                program.BindBlock(UniUniformConstants, BindingIndexUniformConstants);
            }
        }

        public ShaderInstance InstanceShader(ShaderSourceResource source, bool? lighting = null, ShaderBlendMode? mode = null)
        {
            var newHandle = AllocRid();
            var loaded = new LoadedShaderInstance
            {
                ShaderHandle = source.ClydeHandle,
                HasLighting = lighting ?? source.ParsedShader.LightMode != ShaderLightMode.Unshaded,
                BlendMode = mode ?? source.ParsedShader.BlendMode
            };
            var instance = new ClydeShaderInstance(newHandle, this);
            _shaderInstances.Add(newHandle, loaded);
            return instance;
        }

        private void LoadStockShaders()
        {
            _shaderLibrary = ReadEmbeddedShader("z-library.glsl");

            _shaderWrapCodeDefaultFrag = ReadEmbeddedShader("base-default.frag");
            _shaderWrapCodeDefaultVert = ReadEmbeddedShader("base-default.vert");

            _shaderWrapCodeRawVert = ReadEmbeddedShader("base-raw.vert");
            _shaderWrapCodeRawFrag = ReadEmbeddedShader("base-raw.frag");

            _winBlitShaderVert = ReadEmbeddedShader("winblit.vert");
            _winBlitShaderFrag = ReadEmbeddedShader("winblit.frag");

            var defaultLoadedShader = _resourceCache
                .GetResource<ShaderSourceResource>("/Shaders/Internal/default-sprite.swsl");

            _defaultShader = (ClydeShaderInstance) InstanceShader(defaultLoadedShader);

            _queuedShaderInstance = _defaultShader;
        }

        private string ReadEmbeddedShader(string fileName)
        {
            var assembly = typeof(Clyde).Assembly;
            using var stream = assembly.GetManifestResourceStream($"Robust.Client.Graphics.Clyde.Shaders.{fileName}")!;
            DebugTools.AssertNotNull(stream);
            using var reader = new StreamReader(stream, EncodingHelpers.UTF8);
            return reader.ReadToEnd();
        }

        private GLShaderProgram _compileProgram(string vertexSource, string fragmentSource,
            (string, uint)[] attribLocations, string? name = null, bool includeLib=true, Dictionary<string,string>? defines=null)
        {
            GLShader? vertexShader = null;
            GLShader? fragmentShader = null;

            var versionHeader = "#version 140\n#define HAS_MOD\n";

            if (_isGLES)
            {
                if (_hasGLES3Shaders)
                {
                    versionHeader = "#version 300 es\n";
                }
                else
                {
                    // GLES2 uses a different GLSL versioning scheme to desktop GL.
                    versionHeader = "#version 100\n#define HAS_VARYING_ATTRIBUTE\n";
                    if (_hasGLStandardDerivatives)
                    {
                        versionHeader += "#extension GL_OES_standard_derivatives : enable\n";
                    }

                    versionHeader += "#define NO_ARRAY_PRECISION\n";
                }

            }

            if (_hasGLStandardDerivatives)
            {
                versionHeader += "#define HAS_DFDX\n";
            }

            if (_hasGLFloatFramebuffers)
            {
                versionHeader += "#define HAS_FLOAT_TEXTURES\n";
            }

            if (_hasGLSrgb)
            {
                versionHeader += "#define HAS_SRGB\n";
            }

            if (_hasGLUniformBuffers)
            {
                versionHeader += "#define HAS_UNIFORM_BUFFERS\n";
            }

            if (defines is not null)
            {
                foreach (var k in defines.Keys)
                {
                    versionHeader += $"#define {k} {defines[k]}\n";
                }
            }

            var lib = includeLib ? _shaderLibrary : "";
            vertexSource = versionHeader + "#define VERTEX_SHADER\n" + lib + vertexSource;
            fragmentSource = versionHeader + "#define FRAGMENT_SHADER\n" + lib + fragmentSource;

            try
            {
                try
                {
                    vertexShader = new GLShader(this, ShaderType.VertexShader, vertexSource, name == null ? $"{name}-vertex" : null);
                }
                catch (ShaderCompilationException e)
                {
                    File.WriteAllText("error.glsl", vertexSource);
                    throw new ShaderCompilationException(
                        "Failed to compile vertex shader, see inner for details (and error.glsl for formatted source).", e);
                }

                try
                {
                    fragmentShader = new GLShader(this, ShaderType.FragmentShader, fragmentSource, name == null ? $"{name}-fragment" : null);
                }
                catch (ShaderCompilationException e)
                {
                    File.WriteAllText("error.glsl", fragmentSource);
                    throw new ShaderCompilationException(
                        "Failed to compile fragment shader, see inner for details (and error.glsl for formatted source).", e);
                }

                var program = new GLShaderProgram(this, name);
                program.Add(vertexShader);
                program.Add(fragmentShader);

                try
                {
                    program.Link(attribLocations);
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

        private (string vertBody, string fragBody) GetShaderCode(ParsedShader shader)
        {
            var headerUniforms = new StringBuilder();

            foreach (var constant in shader.Constants.Values)
            {
                headerUniforms.AppendFormat("const {0} {1} = {2};\n", constant.Type.GetNativeType(), constant.Name,
                    constant.Value);
            }

            foreach (var uniform in shader.Uniforms.Values)
            {
                if (uniform.DefaultValue != null)
                {
                    headerUniforms.AppendFormat("uniform {0} {1} = {2};\n", uniform.Type.GetNativeType(), uniform.Name,
                        uniform.DefaultValue);
                }
                else
                {
                    if (uniform.Type.IsArray)
                    {
                        headerUniforms.AppendFormat($"uniform {uniform.Type.GetNativeTypeWithoutArray()} {uniform.Name}[{uniform.Type.Count}];\n");
                    }
                    else
                    {
                        headerUniforms.AppendFormat("uniform {0} {1};\n", uniform.Type.GetNativeType(), uniform.Name);
                    }
                }
            }

            var varyingsFragment = new StringBuilder();
            var varyingsVertex = new StringBuilder();

            foreach (var (name, varying) in shader.Varyings)
            {
                varyingsFragment.AppendFormat("varying {0} {1};\n", varying.Type.GetNativeType(), name);
                varyingsVertex.AppendFormat("varying {0} {1};\n", varying.Type.GetNativeType(), name);
            }

            ShaderFunctionDefinition? fragmentMain = null;
            ShaderFunctionDefinition? vertexMain = null;

            var functionsBuilder = new StringBuilder();

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

                functionsBuilder.AppendFormat("{0} {1}(", function.ReturnType.GetNativeType(), function.Name);
                var first = true;
                foreach (var parameter in function.Parameters)
                {
                    if (!first)
                    {
                        functionsBuilder.Append(", ");
                    }

                    first = false;

                    functionsBuilder.AppendFormat("{0} {1} {2}", parameter.Qualifiers.GetString(), parameter.Type.GetNativeType(),
                        parameter.Name);
                }

                functionsBuilder.AppendFormat(") {{\n{0}\n}}\n", function.Body);
            }

            var (wrapVert, wrapFrag) = shader.Preset switch
            {
                ShaderPreset.Default => (_shaderWrapCodeDefaultVert, _shaderWrapCodeDefaultFrag),
                ShaderPreset.Raw => (_shaderWrapCodeRawVert, _shaderWrapCodeRawFrag),
                _ => throw new NotSupportedException()
            };

            var vertexHeader = $"{headerUniforms}\n{varyingsVertex}\n{functionsBuilder}";
            var fragmentHeader = $"{headerUniforms}\n{varyingsFragment}\n{functionsBuilder}";

            // These are prefixed with comments because the syntax highlighter I'm using doesn't like the brackets.
            // And it's producing a lot of squigly lines.
            var vertexSource = wrapVert.Replace("// [SHADER_HEADER_CODE]", vertexHeader);
            var fragmentSource = wrapFrag.Replace("// [SHADER_HEADER_CODE]", fragmentHeader);

            if (vertexMain != null)
            {
                vertexSource = vertexSource.Replace("// [SHADER_CODE]", vertexMain.Body);
            }

            if (fragmentMain != null)
            {
                fragmentSource = fragmentSource.Replace("// [SHADER_CODE]", fragmentMain.Body);
            }

            return (vertexSource, fragmentSource);
        }

        private void FlushShaderInstanceDispose()
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
                var newData = new LoadedShaderInstance(instanceData);
                var newHandle = Parent.AllocRid();
                Parent._shaderInstances.Add(newHandle, newData);
                return new ClydeShaderInstance(newHandle, Parent);
            }

            public override void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            ~ClydeShaderInstance()
            {
                Dispose(false);
            }

            private void Dispose(bool disposing)
            {
                if (Disposed)
                    return;

                Disposed = true;
                Parent._deadShaderInstances.Enqueue(Handle);
            }

            // TODO: Verify that parameters actually exist before assigning them like this.

            private protected override void SetParameterImpl(string name, float value)
            {
                var data = Parent._shaderInstances[Handle];
                data.ParametersDirty = true;
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, float[] value)
            {
                var data = Parent._shaderInstances[Handle];
                data.ParametersDirty = true;
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, Vector2 value)
            {
                var data = Parent._shaderInstances[Handle];
                data.ParametersDirty = true;
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, Vector2[] value)
            {
                var data = Parent._shaderInstances[Handle];
                data.ParametersDirty = true;
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, Vector3 value)
            {
                var data = Parent._shaderInstances[Handle];
                data.ParametersDirty = true;
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, Vector4 value)
            {
                var data = Parent._shaderInstances[Handle];
                data.ParametersDirty = true;
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, Color value)
            {
                var data = Parent._shaderInstances[Handle];
                data.ParametersDirty = true;
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, int value)
            {
                var data = Parent._shaderInstances[Handle];
                data.ParametersDirty = true;
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, Vector2i value)
            {
                var data = Parent._shaderInstances[Handle];
                data.ParametersDirty = true;
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, bool value)
            {
                var data = Parent._shaderInstances[Handle];
                data.ParametersDirty = true;
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, in Matrix3x2 value)
            {
                var data = Parent._shaderInstances[Handle];
                data.ParametersDirty = true;
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, in Matrix4 value)
            {
                var data = Parent._shaderInstances[Handle];
                data.ParametersDirty = true;
                data.Parameters[name] = value;
            }

            private protected override void SetParameterImpl(string name, Texture value)
            {
                var data = Parent._shaderInstances[Handle];
                data.ParametersDirty = true;
                data.Parameters[name] = value;
            }

            private protected override void SetStencilImpl(StencilParameters value)
            {
                var data = Parent._shaderInstances[Handle];
                data.Stencil = value;
            }
        }
    }
}
