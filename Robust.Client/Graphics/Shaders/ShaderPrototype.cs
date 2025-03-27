using System;
using System.Collections.Generic;
using System.Numerics;
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
using Vector3 = Robust.Shared.Maths.Vector3;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace Robust.Client.Graphics
{
    [Prototype]
    public sealed partial class ShaderPrototype : IPrototype, ISerializationHooks
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [ViewVariables] private ShaderKind Kind;
        [ViewVariables] private Dictionary<string, object>? _params;
        [ViewVariables] private ShaderSourceResource? _source;
        [ViewVariables] private ShaderInstance? _cachedInstance;
        [ViewVariables] private ParsedShader? _parsed => _source?.ParsedShader;

        [DataField("stencil")]
        private StencilParameters? _stencil;

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
                    instance = IoCManager.Resolve<IClydeInternal>().InstanceShader(_source!);
                    _applyDefaultParameters(instance);
                    break;

                case ShaderKind.Canvas:

                    var hasLight = _rawMode != "unshaded";
                    ShaderBlendMode? blend = null;
                    if (_rawBlendMode != null)
                    {
                        if (!Enum.TryParse<ShaderBlendMode>(_rawBlendMode, true, out var parsed))
                            Logger.Error($"invalid mode: {_rawBlendMode}");
                        else
                            blend = parsed;
                    }

                    instance = IoCManager.Resolve<IClydeInternal>().InstanceShader(_source!, hasLight, blend);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_stencil is {} data)
                instance.Stencil = data with { Enabled = true};

            instance.MakeImmutable();
            _cachedInstance = instance;
        }

        public ShaderInstance InstanceUnique()
        {
            return Instance().Duplicate();
        }

        [DataField("kind", required: true)]
        private string _rawKind = default!;

        [DataField("path")]
        private ResPath? _path;

        [DataField("params")]
        private Dictionary<string, string>? _paramMapping;

        [DataField("light_mode")]
        private string? _rawMode;

        [DataField("blend_mode")]
        private string? _rawBlendMode;

        void ISerializationHooks.AfterDeserialization()
        {
            switch (_rawKind)
            {
                case "source":
                    Kind = ShaderKind.Source;

                    // TODO use a custom type serializer.
                    if (_path == null)
                        throw new InvalidOperationException("Source shaders must specify a source file.");

                    _source = IoCManager.Resolve<IResourceCache>().GetResource<ShaderSourceResource>(_path.Value);

                    if (_paramMapping != null)
                    {
                        _params = new Dictionary<string, object>();
                        foreach (var item in _paramMapping!)
                        {
                            var name = item.Key;
                            if (!_source.ParsedShader.Uniforms.TryGetValue(name, out var uniformDefinition))
                            {
                                Logger.ErrorS("shader", "Shader param '{0}' does not exist on shader '{1}'", name, _path);
                                continue;
                            }

                            var value = _parseUniformValue(item.Value, uniformDefinition.Type.Type);
                            _params.Add(name, value);
                        }
                    }
                    break;

                case "canvas":
                    Kind = ShaderKind.Canvas;
                    _source = IoCManager.Resolve<IResourceCache>().GetResource<ShaderSourceResource>("/Shaders/Internal/default-sprite.swsl");
                    break;

                default:
                    throw new InvalidOperationException($"Invalid shader kind: '{_rawKind}'");
            }
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
                case ShaderDataType.Mat3:
                    return node.AsMatrix3x2();
                case ShaderDataType.Mat4:
                    return node.AsMatrix4();
                default:
                    throw new NotSupportedException($"Unsupported uniform type '{dataType}'.");
            }
        }

        private void _applyDefaultParameters(ShaderInstance instance)
        {
            if (_params == null)
            {
                return;
            }

            foreach (var (key, value) in _params)
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
                    case Matrix3x2 i:
                        instance.SetParameter(key, i);
                        break;
                    case Matrix4 i:
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
