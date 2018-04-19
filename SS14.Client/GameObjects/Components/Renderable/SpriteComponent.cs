using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Shaders;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;
using VS = Godot.VisualServer;

namespace SS14.Client.GameObjects
{
    public sealed class SpriteComponent : Component
    {
        public override string Name => "Sprite";
        public override uint? NetID => NetIDs.SPRITE;
        public override Type StateType => typeof(SpriteComponentState);

        public static readonly ResourcePath TextureRoot = new ResourcePath("/Textures");

        private List<Layer> Layers = new List<Layer>();
        private List<Godot.RID> CanvasItems = new List<Godot.RID>();

        private Godot.Node2D SceneNode;
        private IGodotTransformComponent TransformComponent;

        public override void OnAdd()
        {
            base.OnAdd();
            SceneNode = new Godot.Node2D()
            {
                Name = "Sprite"
            };
        }

        public override void OnRemove()
        {
            base.OnRemove();

            ClearDraw();
            SceneNode.QueueFree();
        }

        public override void Initialize()
        {
            base.Initialize();

            TransformComponent = Owner.GetComponent<IGodotTransformComponent>();
            TransformComponent.SceneNode.AddChild(SceneNode);
            Redraw();
        }

        private void ClearDraw()
        {
            foreach (var item in CanvasItems)
            {
                VS.FreeRid(item);
            }
            CanvasItems.Clear();
        }

        private void Redraw()
        {
            ClearDraw();

            Shader prevShader = null;
            Godot.RID currentItem = null;
            foreach (var layer in Layers)
            {
                var shader = layer.Shader;
                var texture = layer.Texture;
                if (currentItem == null || prevShader != shader)
                {
                    currentItem = VS.CanvasItemCreate();
                    VS.CanvasItemSetParent(currentItem, SceneNode.GetCanvasItem());
                    CanvasItems.Add(currentItem);
                    if (shader != null)
                    {
                        VS.CanvasItemSetMaterial(currentItem, shader.GodotMaterial.GetRid());
                    }
                    prevShader = shader;
                }

                texture.GodotTexture.Draw(currentItem, -texture.GodotTexture.GetSize() / 2);
            }
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            base.LoadParameters(mapping);

            var resc = IoCManager.Resolve<IResourceCache>();

            YamlNode node;
            if (mapping.TryGetNode("sprite", out node))
            {
                var path = node.AsResourcePath();
                var tex = resc.GetResource<TextureResource>(TextureRoot / path);
                var layer = new Layer
                {
                    Texture = tex
                };
                Layers.Add(layer);
            }

            if (mapping.TryGetNode("layers", out YamlSequenceNode layers))
            {
                foreach (var layernode in layers.Cast<YamlMappingNode>())
                {
                    var path = layernode.GetNode("sprite").AsResourcePath();
                    var tex = resc.GetResource<TextureResource>(TextureRoot / path);
                    Shader shader = null;
                    if (layernode.TryGetNode("shader", out node))
                    {
                        shader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>(node.AsString()).Instance();
                    }
                    var layer = new Layer
                    {
                        Texture = tex,
                        Shader = shader,
                    };
                    Layers.Add(layer);
                }
            }
        }

        private struct Layer
        {
            public Texture Texture;
            public Shader Shader;
        }
    }
}
