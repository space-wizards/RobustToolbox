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
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

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

                    var hasLight = rawMode != "unshaded";
                    ShaderBlendMode? blend = null;
                    if (rawBlendMode != null)
                    {
                        if (!Enum.TryParse<ShaderBlendMode>(rawBlendMode.ToUpper(), out var parsed))
                            Logger.Error($"invalid mode: {rawBlendMode}");
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

        [DataField("kind", readOnly: true, required: true)] private string _rawKind = default!;
        [DataField("path", readOnly: true)] private ResPath path;
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
                    _source = IoCManager.Resolve<IResourceCache>().GetResource<ShaderSourceResource>(path);

                    if (paramMapping != null)
                    {
                        _params = new Dictionary<string, object>();
                        foreach (var item in paramMapping!)
                        {
                            var name = item.Key;
                            if (!_source.ParsedShader.Uniforms.TryGetValue(name, out var uniformDefinition))
                            {
                                Logger.ErrorS("shader", "Shader param '{0}' does not exist on shader '{1}'", name, path);
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
                    return node.AsMatrix3();
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
                    case Matrix3 i:
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
