using SFML.Graphics;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace SS14.Client.GameObjects
{
    [IoCTarget]
    [Component("Icon")]
    public class IconComponent : Component
    {
        public Sprite Icon;

        public IconComponent()
        {
            Family = ComponentFamily.Icon;
        }

        public override void LoadParameters(Dictionary<string, YamlNode> mapping)
        {
            YamlNode node;
            if (mapping.TryGetValue("icon", out node))
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
