using SFML.Graphics;
using SS14.Server.Interfaces.Map;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Hitbox;
using SS14.Shared.IoC;
using Component = SS14.Shared.GameObjects.Component;
using YamlDotNet.RepresentationModel;

namespace SS14.Server.GameObjects
{
    [IoCTarget]
    [Component("Hitbox")]
    public class HitboxComponent : Component
    {
        public FloatRect AABB { get; set; }

        public HitboxComponent()
        {
            Family = ComponentFamily.Hitbox;
            AABB = new FloatRect();
        }

        public override ComponentState GetComponentState()
        {
            return new HitboxComponentState(AABB);
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            var tileSize = IoCManager.Resolve<IMapManager>().TileSize;

            YamlNode node;
            if (mapping.Children.TryGetValue(new YamlScalarNode("sizeX"), out node))
            {
                var width = float.Parse(((YamlScalarNode)node).Value) / tileSize;
                AABB = new FloatRect(AABB.Left + (AABB.Width - width) / 2f, AABB.Top, width, AABB.Height);
            }

            if (mapping.Children.TryGetValue(new YamlScalarNode("sizeY"), out node))
            {
                var height = float.Parse(((YamlScalarNode)node).Value) / tileSize;
                AABB = new FloatRect(AABB.Left, AABB.Top + (AABB.Height - height) / 2f, AABB.Width, height);
            }

            if (mapping.Children.TryGetValue(new YamlScalarNode("offsetX"), out node))
            {
                var x = float.Parse(((YamlScalarNode)node).Value) / tileSize;
                AABB = new FloatRect(x - AABB.Width / 2f, AABB.Top, AABB.Width, AABB.Height);
            }

            if (mapping.Children.TryGetValue(new YamlScalarNode("offsetY"), out node))
            {
                var y = float.Parse(((YamlScalarNode)node).Value) / tileSize;
                AABB = new FloatRect(AABB.Left, y - AABB.Height / 2f, AABB.Width, AABB.Height);
            }
        }
    }
}
