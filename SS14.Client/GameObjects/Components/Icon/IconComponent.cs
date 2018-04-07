using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace SS14.Client.GameObjects
{
    public class IconComponent : Component
    {
        public override string Name => "Icon";
        public TextureResource Icon { get; private set; }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            if (mapping.TryGetNode("icon", out YamlNode node))
            {
                SetIcon(node.AsString());
            }
        }

        public void SetIcon(string name)
        {
            Icon = IoCManager.Resolve<IResourceCache>().GetResource<TextureResource>($@"./Textures/{name}");
        }
    }
}
