using SS14.Shared.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.GameObjects;
using YamlDotNet.RepresentationModel;
using SS14.Shared.Utility;
using SS14.Client.ResourceManagement;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.IoC;
using SS14.Shared.Log;

namespace SS14.Client.GameObjects
{
    public class SpriteComponent : Component
    {
        public override string Name => "Sprite";

        private TextureResource texture;
        public string TextureName
        {
            get => textureName;
            set
            {
                if (value == textureName)
                {
                    return;
                }

                if (value != null)
                {
                    var mgr = IoCManager.Resolve<IResourceCache>();
                    var tex = mgr.GetResource<TextureResource>($"./Textures/{value}.png");
                    if (tex == null)
                    {
                        throw new ArgumentException($"Unable to load texture with name {value}");
                    }
                    texture = tex;

                    SceneSprite?.SetTexture(texture.Texture);
                }
                else
                {
                    texture = null;
                }
                textureName = value;
            }
        }

        private string textureName;

        public Godot.Sprite SceneSprite { get; private set; }
        private IClientTransformComponent transformComponent;

        public override void Initialize()
        {
            base.Initialize();
            transformComponent = Owner.GetComponent<IClientTransformComponent>();
            SceneSprite = new Godot.Sprite();
            SceneSprite.SetName("SpriteComponent");
            transformComponent.SceneNode.AddChild(SceneSprite);

            if (texture != null)
            {
                SceneSprite.SetTexture(texture.Texture);
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();
            SceneSprite.QueueFree();
            SceneSprite = null;
            transformComponent = null;
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            base.LoadParameters(mapping);

            if (mapping.TryGetNode("sprite", out var node))
            {
                TextureName = node.AsString();
            }
        }
    }
}
