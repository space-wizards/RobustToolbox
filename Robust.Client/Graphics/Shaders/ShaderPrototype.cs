using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement.ResourceTypes;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Maths;
using YamlDotNet.RepresentationModel;

namespace Robust.Client.Graphics.Shaders
{
    [Prototype("shader")]
    public sealed class ShaderPrototype : IPrototype, IIndexedPrototype
    {
#pragma warning disable 649
        [Dependency] private readonly IClydeInternal _clyde;
        [Dependency] private readonly IResourceCache _resourceCache;
#pragma warning restore 649

        public string ID { get; private set; }

        private ShaderKind Kind;

        // Source shader variables.
        private ShaderSourceResource Source;
        private Dictionary<string, object> ShaderParams;

        // Canvas shader variables.
        private ClydeHandle CompiledCanvasShader;

        /// <summary>
        ///     Creates a new instance of this shader.
        /// </summary>
        public ShaderInstance Instance()
        {
            switch (Kind)
            {
                case ShaderKind.Source:
                    var instance = _clyde.InstanceShader(Source.ClydeHandle);
                    _applyDefaultParameters(instance);
                    return instance;

                case ShaderKind.Canvas:
                    return _clyde.InstanceShader(CompiledCanvasShader);

                default:
                    throw new ArgumentOutOfRangeException();
            }
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
        }

        private void ReadSourceKind(YamlMappingNode mapping)
        {
            var path = mapping.GetNode("path").AsResourcePath();
            Source = _resourceCache.GetResource<ShaderSourceResource>(path);
            if (mapping.TryGetNode<YamlMappingNode>("params", out var paramMapping))
            {
                ShaderParams = new Dictionary<string, object>();
                foreach (var item in paramMapping)
                {
                    var name = item.Key.AsString();
                    if (!Source.ParsedShader.Uniforms.TryGetValue(name, out var uniformDefinition))
                    {
                        Logger.ErrorS("shader", "Shader param '{0}' does not exist on shader '{1}'", name, path);
                        continue;
                    }

                    var value = _parseUniformValue(item.Value, uniformDefinition.Type);
                    ShaderParams.Add(name, value);
                }
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

            source += "void fragment() {\n    COLOR = texture(TEXTURE, UV);\n}";

            var preset = ShaderParser.Parse(source);
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
                    case bool i:
                        instance.SetParameter(key, i);
                        break;
                }
            }

        }

        private enum ShaderKind
        {
            Source,
            Canvas
        }
    }
}
