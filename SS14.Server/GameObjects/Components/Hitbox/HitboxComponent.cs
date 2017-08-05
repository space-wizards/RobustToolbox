using SFML.Graphics;
using SS14.Server.Interfaces.Map;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components;
using SS14.Shared.GameObjects.Components.Hitbox;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using Component = SS14.Shared.GameObjects.Component;
using YamlDotNet.RepresentationModel;
using System.Collections.Generic;

namespace SS14.Server.GameObjects
{
    public class HitboxComponent : Component
    {
        public override string Name => "Hitbox";
        public override uint? NetID => NetIDs.HITBOX;
        public FloatRect AABB { get; set; } = new FloatRect();

        public override ComponentState GetComponentState()
        {
            return new HitboxComponentState(AABB);
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            var tileSize = IoCManager.Resolve<IMapManager>().TileSize;

            YamlNode node;
            if (mapping.TryGetNode("sizeX", out node))
            {
                var width = node.AsFloat() / tileSize;
                AABB = new FloatRect(AABB.Left + (AABB.Width - width) / 2f, AABB.Top, width, AABB.Height);
            }

            if (mapping.TryGetNode("sizeY", out node))
            {
                var height = node.AsFloat() / tileSize;
                AABB = new FloatRect(AABB.Left, AABB.Top + (AABB.Height - height) / 2f, AABB.Width, height);
            }

            if (mapping.TryGetNode("offsetX", out node))
            {
                var x = node.AsFloat() / tileSize;
                AABB = new FloatRect(x - AABB.Width / 2f, AABB.Top, AABB.Width, AABB.Height);
            }

            if (mapping.TryGetNode("offsetY", out node))
            {
                var y = node.AsFloat() / tileSize;
                AABB = new FloatRect(AABB.Left, y - AABB.Height / 2f, AABB.Width, AABB.Height);
            }
        }
    }
}
