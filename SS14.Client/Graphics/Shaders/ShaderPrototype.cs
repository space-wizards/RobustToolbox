using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Client.ResourceManagement.ResourceTypes;
using SS14.Client.Utility;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;
using BlendModeEnum = Godot.CanvasItemMaterial.BlendModeEnum;
using LightModeEnum = Godot.CanvasItemMaterial.LightModeEnum;

namespace SS14.Client.Graphics.Shaders
{
    [Prototype("shader")]
    public sealed class ShaderPrototype : IPrototype, IIndexedPrototype
    {
        public string ID { get; private set; }

        private ShaderKind Kind;

        // Source shader variables.
        private ShaderSourceResource Source;
        private Dictionary<string, object> ShaderParams;

        // Canvas shader variables.
        private LightModeEnum LightMode;
        private BlendModeEnum BlendMode;

        /// <summary>
        ///     Creates a new instance of this shader.
        /// </summary>
        public Shader Instance()
        {
            Godot.Material mat;

            switch (Kind)
            {
                case ShaderKind.Source:
                    var shaderMat = new Godot.ShaderMaterial
                    {
                        Shader = Source.GodotShader
                    };
                    mat = shaderMat;
                    if (ShaderParams != null)
                    {
                        foreach (var pair in ShaderParams)
                        {
                            shaderMat.SetShaderParam(pair.Key, pair.Value);
                        }
                    }
                    break;
                case ShaderKind.Canvas:
                    mat = new Godot.CanvasItemMaterial
                    {
                        LightMode = LightMode,
                        BlendMode = BlendMode,
                    };
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return new Shader(mat);
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
            var resc = IoCManager.Resolve<IResourceCache>();
            Source = resc.GetResource<ShaderSourceResource>(path);
            if (mapping.TryGetNode<YamlMappingNode>("params", out var paramMapping))
            {
                ShaderParams = new Dictionary<string, object>();
                foreach (var item in paramMapping)
                {
                    var name = item.Key.AsString();
                    if (!Source.TryGetShaderParamType(name, out var type))
                    {
                        Logger.ErrorS("shader", "Shader param '{0}' does not exist on shader '{1}'", name, path);
                        continue;
                    }

                    var value = ParseShaderParamFor(item.Value, type);
                    ShaderParams.Add(name, value);
                }
            }
        }

        private void ReadCanvasKind(YamlMappingNode mapping)
        {
            if (mapping.TryGetNode("light_mode", out var node))
            {
                switch (node.AsString())
                {
                    case "normal":
                        LightMode = LightModeEnum.Normal;
                        break;

                    case "unshaded":
                        LightMode = LightModeEnum.Unshaded;
                        break;

                    case "light_only":
                        LightMode = LightModeEnum.LightOnly;
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
                        BlendMode = BlendModeEnum.Mix;
                        break;

                    case "add":
                        BlendMode = BlendModeEnum.Add;
                        break;

                    case "subtract":
                        BlendMode = BlendModeEnum.Sub;
                        break;

                    case "multiply":
                        BlendMode = BlendModeEnum.Mul;
                        break;

                    case "premultiplied_alpha":
                        BlendMode = BlendModeEnum.PremultAlpha;
                        break;

                    default:
                        throw new InvalidOperationException($"Invalid blend mode: '{node.AsString()}'");
                }
            }
        }

        private object ParseShaderParamFor(YamlNode node, ShaderParamType type)
        {
            switch (type)
            {
                case ShaderParamType.Void:
                    throw new NotSupportedException();
                case ShaderParamType.Bool:
                    return node.AsBool();
                case ShaderParamType.Int:
                case ShaderParamType.UInt:
                    return node.AsInt();
                case ShaderParamType.Float:
                    return node.AsFloat();
                case ShaderParamType.Vec2:
                    return node.AsVector2().Convert();
                case ShaderParamType.Vec3:
                    return node.AsVector3().Convert();
                case ShaderParamType.Vec4:
                    try
                    {
                        return node.AsColor().Convert();
                    }
                    catch
                    {
                        var vec4 = node.AsVector4();
                        return new Godot.Quat(vec4.X, vec4.Y, vec4.Z, vec4.W);
                    }

                case ShaderParamType.Sampler2D:
                    var path = node.AsResourcePath();
                    var resc = IoCManager.Resolve<IResourceCache>();
                    return resc.GetResource<TextureResource>(path).Texture.GodotTexture;
                // If something's not handled here, then that's probably because I was lazy.
                default:
                    throw new NotImplementedException();
            }
        }

        enum ShaderKind
        {
            Source,
            Canvas
        }
    }
}
