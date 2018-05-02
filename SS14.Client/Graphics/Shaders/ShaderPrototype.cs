using System;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement.ResourceTypes;
using SS14.Shared.IoC;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;
using LightModeEnum = Godot.CanvasItemMaterial.LightModeEnum;
using BlendModeEnum = Godot.CanvasItemMaterial.BlendModeEnum;

namespace SS14.Client.Graphics.Shaders
{
    [Prototype("shader")]
    public sealed class ShaderPrototype : IPrototype, IIndexedPrototype
    {
        public string ID { get; private set; }

        private ShaderKind Kind;

        private ShaderSourceResource Source;

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
                    mat = new Godot.ShaderMaterial
                    {
                        Shader = Source.GodotShader
                    };
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
                    var path = mapping.GetNode("path").AsResourcePath();
                    var resc = IoCManager.Resolve<IResourceCache>();
                    Source = resc.GetResource<ShaderSourceResource>(path);

                    // TODO: Handle shader parameters!
                    break;

                case "canvas":
                    Kind = ShaderKind.Canvas;
                    ReadCanvasKind(mapping);
                    break;

                default:
                    throw new InvalidOperationException($"Invalid shader kind: '{kind}'");

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

        enum ShaderKind
        {
            Source,
            Canvas
        }
    }
}
