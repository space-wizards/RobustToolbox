using System;
using System.Collections.Generic;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Robust.Client.Graphics
{
    [Prototype("shader")]
    public sealed class ShaderPrototype : IPrototype, ISerializationHooks
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        [ViewVariables]
        [field: DataField("id", required: true)]
        public string ID { get; } = default!;

        [ViewVariables]
        [field: DataField("parent")]
        public string? Parent { get; }

        private ShaderKind Kind;

        // Source shader variables.
        private ShaderSourceResource? Source;
        private Dictionary<string, object>? ShaderParams;

        // Canvas shader variables.
        private ClydeHandle CompiledCanvasShader;

        private ShaderInstance? _cachedInstance;

        private bool _stencilEnabled;
        private int _stencilRef => StencilDataHolder?.StencilRef ?? 0;
        private int _stencilReadMask => StencilDataHolder?.ReadMask ?? unchecked((int) uint.MaxValue);
        private int _stencilWriteMask => StencilDataHolder?.WriteMask ?? unchecked((int) uint.MaxValue);
        private StencilFunc _stencilFunc => StencilDataHolder?.StencilFunc ?? StencilFunc.Always;
        private StencilOp _stencilOp => StencilDataHolder?.StencilOp ?? StencilOp.Keep;

        [DataField("stencil")]
        private StencilData? StencilDataHolder;

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
                    instance = IoCManager.Resolve<IClydeInternal>().InstanceShader(Source!.ClydeHandle);
                    _applyDefaultParameters(instance);
                    break;

                case ShaderKind.Canvas:
                    instance = IoCManager.Resolve<IClydeInternal>().InstanceShader(CompiledCanvasShader);
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

        [DataField("kind", readOnly: true, required: true)] private string _rawKind = default!;
        [DataField("path", readOnly: true)] private ResourcePath? path;
        [DataField("params", readOnly: true)] private Dictionary<string, string>? paramMapping;
        [DataField("light_mode", readOnly: true)] private string? rawMode;
        [DataField("blend_mode", readOnly: true)] private string? rawBlendMode;

        void ISerializationHooks.AfterDeserialization()
        {
            switch (_rawKind)
            {
                case "source":
                    Kind = ShaderKind.Source;
                    if (path == null) throw new InvalidOperationException();
                    Source = IoCManager.Resolve<IResourceCache>().GetResource<ShaderSourceResource>(path);

                    if (paramMapping != null)
                    {
                        ShaderParams = new Dictionary<string, object>();
                        foreach (var item in paramMapping!)
                        {
                            var name = item.Key;
                            if (!Source.ParsedShader.Uniforms.TryGetValue(name, out var uniformDefinition))
                            {
                                Logger.ErrorS("shader", "Shader param '{0}' does not exist on shader '{1}'", name, path);
                                continue;
                            }

                            var value = _parseUniformValue(item.Value, uniformDefinition.Type.Type);
                            ShaderParams.Add(name, value);
                        }
                    }
                    break;

                case "canvas":
                    Kind = ShaderKind.Canvas;
                    var source = "";

                    if(rawMode != null)
                    {
                        switch (rawMode)
                        {
                            case "normal":
                                break;

                            case "unshaded":
                                source += "light_mode unshaded;\n";
                                break;

                            default:
                                throw new InvalidOperationException($"Invalid light mode: '{rawMode}'");
                        }
                    }

                    if(rawBlendMode != null){
                        switch (rawBlendMode)
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
                                throw new InvalidOperationException($"Invalid blend mode: '{rawBlendMode}'");
                        }
                    }

                    source += "void fragment() {\n    COLOR = zTexture(UV);\n}";

                    var preset = ShaderParser.Parse(source, _resourceCache);
                    CompiledCanvasShader = IoCManager.Resolve<IClydeInternal>().LoadShader(preset, $"canvas_preset_{ID}");
                    break;

                default:
                    throw new InvalidOperationException($"Invalid shader kind: '{_rawKind}'");
            }

            if (StencilDataHolder != null) _stencilEnabled = true;
        }

        [DataDefinition]
        public class StencilData
        {
            [DataField("ref")] public int StencilRef;

            [DataField("op")] public StencilOp StencilOp;

            [DataField("func")] public StencilFunc StencilFunc;

            [DataField("readMask")] public int ReadMask = unchecked((int) uint.MaxValue);

            [DataField("writeMask")] public int WriteMask = unchecked((int) uint.MaxValue);
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
