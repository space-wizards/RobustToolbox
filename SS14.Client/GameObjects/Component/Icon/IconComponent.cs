using SFML.Graphics;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
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

        public override void LoadParameters(YamlMappingNode mapping)
        {
            YamlNode node;
            if (mapping.Children.TryGetValue(new YamlScalarNode("icon"), out node))
            {
                SetIcon(((YamlScalarNode)node).Value);
            }
        }

        public void SetIcon(string name)
        {
            Icon = IoCManager.Resolve<IResourceManager>().GetSprite(name);
        }
    }
}
