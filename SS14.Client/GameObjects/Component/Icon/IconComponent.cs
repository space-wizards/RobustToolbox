using SFML.Graphics;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace SS14.Client.GameObjects
{
    public class IconComponent : ClientComponent
    {
        public override string Name => "Icon";
        public Sprite Icon;

        public override void LoadParameters(YamlMappingNode mapping)
        {
            if (mapping.TryGetNode("icon", out YamlNode node))
            {
                SetIcon(node.AsString());
            }
        }

        public void SetIcon(string name)
        {
            Icon = IoCManager.Resolve<IResourceManager>().GetSprite(name);
        }
    }
}
