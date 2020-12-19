using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement.ResourceTypes;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Maths;
using YamlDotNet.RepresentationModel;

namespace Robust.Client.Graphics.Shaders
{
    [Prototype("shader")]
    public sealed class ShaderPrototype : IPrototype, IIndexedPrototype
    {
        [Dependency] private readonly IClydeInternal _clyde = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        public string ID { get; private set; } = default!;

        private ShaderKind Kind;

        // Source shader variables.
        private ShaderSourceResource? Source;
        private Dictionary<string, object>? ShaderParams;

        public IReadOnlyDictionary<string, object>? Parameters
            => ShaderParams == null ? null : new ReadOnlyDictionary<string, object>(ShaderParams);

        // Canvas shader variables.
        private ClydeHandle CompiledCanvasShader;

        private ShaderInstance? _cachedInstance;

        private bool _stencilEnabled;
        private int _stencilRef;
        private int _stencilReadMask = unchecked((int) uint.MaxValue);
        private int _stencilWriteMask = unchecked((int) uint.MaxValue);
        private StencilFunc _stencilFunc = StencilFunc.Always;
        private StencilOp _stencilOp = StencilOp.Keep;

        /// <summary>
        ///     Retrieves a ready-to-use instance of this shader.
        /// </summary>
        /// <remarks>
        ///     This instance is shared. As such, it is immutable.
        ///     Use <see cref="InstanceUnique"/> to get a mutable and unique shader instance.
        /// </remarks>
        public ShaderInstance Instance()
        {
            if (_cachedInstance == null)
            {
                _cacheInstance();
            }

            DebugTools.AssertNotNull(_cachedInstance);

            return _cachedInstance!;
        }

        private void _cacheInstance()
        {
            DebugTools.AssertNull(_cachedInstance);

            ShaderInstance instance;
            switch (Kind)
            {
                case ShaderKind.Source:
                    instance = _clyde.InstanceShader(Source!.ClydeHandle);
                    _applyDefaultParameters(instance);
                    break;

                case ShaderKind.Canvas:
                    instance = _clyde.InstanceShader(CompiledCanvasShader);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_stencilEnabled)
            {
                instance.StencilTestEnabled = true;
                instance.StencilRef = _stencilRef;
                instance.StencilFunc = _stencilFunc;
                instance.StencilOp = _stencilOp;
                instance.StencilReadMask = _stencilReadMask;
                instance.StencilWriteMask = _stencilWriteMask;
            }

            instance.MakeImmutable();
            _cachedInstance = instance;
        }

        public ShaderInstance InstanceUnique()
        {
            return Instance().Duplicate();
        }

        public void LoadFrom(YamlMappingNode mapping)
        {
            ID = mapping.GetNode("id").ToString();

            var kind = mapping.GetNode("kind").AsString();
            switch (kind)
            {
                case "source":
                    Kind = ShaderKind.Source;
                    ReadSourceKind(mapping);
                    break;

                case "canvas":
                    Kind = ShaderKind.Canvas;
                    ReadCanvasKind(mapping);
                    break;

                default:
                    throw new InvalidOperationException($"Invalid shader kind: '{kind}'");
            }

            // Load stencil data.
            if (mapping.TryGetNode("stencil", out YamlMappingNode? stencilData))
            {
                ReadStencilData(stencilData);
            }
        }

        private void ReadStencilData(YamlMappingNode stencilData)
        {
            _stencilEnabled = true;

            if (stencilData.TryGetNode("ref", out var dataNode))
            {
                _stencilRef = dataNode.AsInt();
            }

            if (stencilData.TryGetNode("op", out dataNode))
            {
                _stencilOp = dataNode.AsEnum<StencilOp>();
            }

            if (stencilData.TryGetNode("func", out dataNode))
            {
                _stencilFunc = dataNode.AsEnum<StencilFunc>();
            }

            if (stencilData.TryGetNode("readMask", out dataNode))
            {
                _stencilReadMask = dataNode.AsInt();
            }

            if (stencilData.TryGetNode("writeMask", out dataNode))
            {
                _stencilWriteMask = dataNode.AsInt();
            }
        }

        private void ReadSourceKind(YamlMappingNode mapping)
        {
            var path = mapping.GetNode("path").AsResourcePath();
            Source = _resourceCache.GetResource<ShaderSourceResource>(path);
            if (mapping.TryGetNode<YamlMappingNode>("params", out var paramMapping))
            {
                ShaderParams = new Dictionary<string, object>();
                ParseMapping(this, paramMapping, ShaderParams, path.ToString());
            }
        }

        public static void ParseMapping(ShaderPrototype prototype, YamlMappingNode paramMapping, IDictionary<string, object> shaderParams, string? shaderName = null)
        {
            if (prototype.Source == null)
            {
                throw new ArgumentException("Shader prototype has no source.");
            }

            foreach (var (nameNode, valueNode) in paramMapping)
            {
                var name = nameNode.AsString();
                if (!prototype.Source.ParsedShader.Uniforms.TryGetValue(name, out var uniformDefinition))
                {
                    Logger.ErrorS("shader", shaderName != null
                        ? $"Shader param '{name}' does not exist on shader '{shaderName}'"
                        : $"Shader param '{name}' does not exist on this shader."
                    );
                    continue;
                }

                var value = _parseUniformValue(valueNode, uniformDefinition.Type.Type);
                shaderParams[name] = value;
            }
        }

        public static void ExportUniforms(ShaderPrototype prototype, IDictionary<string, object> shaderParams)
        {
            if (prototype.Source == null)
            {
                throw new ArgumentException("Shader prototype has no source.");
            }

            foreach (var (name, def) in prototype.Source.ParsedShader.Uniforms)
            {
                shaderParams.TryAdd(name, (def.DefaultValue != null
                    ? _parseUniformValue(def.DefaultValue, def.Type.Type)
                    : def.Type.Type switch
                    {
                        ShaderDataType.Void => null,
                        ShaderDataType.Bool => false,
                        ShaderDataType.BVec2 => (false, false),
                        ShaderDataType.BVec3 => (false, false, false),
                        ShaderDataType.BVec4 => (false, false, false, false),
                        ShaderDataType.Int => 0,
                        ShaderDataType.IVec2 => default(Vector2i),
                        ShaderDataType.IVec3 => default(Vector3i),
                        ShaderDataType.IVec4 => default(Vector4i),
                        ShaderDataType.UInt => 0u,
                        ShaderDataType.UVec2 => default(Vector2u),
                        ShaderDataType.UVec3 => (0u, 0u, 0u),
                        ShaderDataType.UVec4 => (0u, 0u, 0u, 0u),
                        ShaderDataType.Float => 0f,
                        ShaderDataType.Vec2 => default(Vector2),
                        ShaderDataType.Vec3 => default(Vector3),
                        ShaderDataType.Vec4 => default(Vector4),
                        ShaderDataType.Mat2 => (0u, 0u, 0u, 0u),
                        ShaderDataType.Mat3 => default(Matrix3),
                        ShaderDataType.Mat4 => default(Matrix4),
                        ShaderDataType.Sampler2D => throw new NotImplementedException(),
                        ShaderDataType.ISampler2D => throw new NotImplementedException(),
                        ShaderDataType.USampler2D => throw new NotImplementedException(),
                        _ => throw new NotImplementedException()
                    })!);
            }
        }

        private void ReadCanvasKind(YamlMappingNode mapping)
        {
            var source = "";

            if (mapping.TryGetNode("light_mode", out var node))
            {
                switch (node.AsString())
                {
                    case "normal":
                        break;

                    case "unshaded":
                        source += "light_mode unshaded;\n";
                        break;

                    default:
                        throw new InvalidOperationException($"Invalid light mode: '{node.AsString()}'");
                }
            }

            if (mapping.TryGetNode("blend_mode", out node))
            {
                switch (node.AsString())
                {
                    case "mix":
                        source += "blend_mode mix;\n";
                        break;

                    case "add":
                        source += "blend_mode add;\n";
                        break;

                    case "subtract":
                        source += "blend_mode subtract;\n";
                        break;

                    case "multiply":
                        source += "blend_mode multiply;\n";
                        break;

                    default:
                        throw new InvalidOperationException($"Invalid blend mode: '{node.AsString()}'");
                }
            }

            source += "void fragment() {\n    COLOR = zTexture(UV);\n}";

            var preset = ShaderParser.Parse(source, _resourceCache);
            CompiledCanvasShader = _clyde.LoadShader(preset, $"canvas_preset_{ID}");
        }

        private static object _parseUniformValue(YamlNode node, ShaderDataType dataType)
        {
            switch (dataType)
            {
                case ShaderDataType.Bool:
                    return node.AsBool();
                case ShaderDataType.Int:
                    return node.AsInt();
                case ShaderDataType.IVec2:
                    return node.AsVector2i();
                case ShaderDataType.Float:
                    return node.AsFloat();
                case ShaderDataType.Vec2:
                    return node.AsVector2();
                case ShaderDataType.Vec3:
                    return node.AsVector3();
                case ShaderDataType.Vec4:
                    try
                    {
                        return node.AsColor();
                    }
                    catch
                    {
                        return node.AsVector4();
                    }
                default:
                    throw new NotSupportedException("Unsupported uniform type.");
            }
        }

        private void _applyDefaultParameters(ShaderInstance instance)
        {
            if (ShaderParams == null)
            {
                return;
            }

            foreach (var (key, value) in ShaderParams)
            {
                switch (value)
                {
                    case int i:
                        instance.SetParameter(key, i);
                        break;
                    case Vector2i i:
                        instance.SetParameter(key, i);
                        break;
                    case float i:
                        instance.SetParameter(key, i);
                        break;
                    case Vector2 i:
                        instance.SetParameter(key, i);
                        break;
                    case Vector3 i:
                        instance.SetParameter(key, i);
                        break;
                    case Vector4 i:
                        instance.SetParameter(key, i);
                        break;
                    case Color i:
                        instance.SetParameter(key, i);
                        break;
                    case bool i:
                        instance.SetParameter(key, i);
                        break;
                }
            }
        }

        private enum ShaderKind : byte
        {
            Source,
            Canvas
        }
    }
}
