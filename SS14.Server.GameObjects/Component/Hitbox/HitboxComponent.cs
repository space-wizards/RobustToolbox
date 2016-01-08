using SFML.Graphics;
using SS14.Server.Interfaces.Map;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Hitbox;
using SS14.Shared.IoC;
using System.Drawing;
using Component = SS14.Shared.GameObjects.Component;

namespace SS14.Server.GameObjects
{
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

        /// <summary>
        /// Set parameters :)
        /// </summary>
        /// <param name="parameter"></param>
        public override void SetParameter(ComponentParameter parameter)
        {
            var tileSize = IoCManager.Resolve<IMapManager>().TileSize;

            //base.SetParameter(parameter);
            switch (parameter.MemberName)
            {
                case "SizeX":
                    var width = parameter.GetValue<float>() / tileSize;
                    AABB = new FloatRect(AABB.Left + (AABB.Width - width) / 2f, AABB.Top, width, AABB.Height);
                    break;
                case "SizeY":
                    var height = parameter.GetValue<float>() / tileSize;
                    AABB = new FloatRect(AABB.Left, AABB.Top + (AABB.Height - height) / 2f, AABB.Width, height);
                    break;
                case "OffsetX":
                    var x = parameter.GetValue<float>() / tileSize;
                    AABB = new FloatRect(x - AABB.Width / 2f, AABB.Top, AABB.Width, AABB.Height);
                    break;
                case "OffsetY":
                    var y = parameter.GetValue<float>() / tileSize;
                    AABB = new FloatRect(AABB.Left, y - AABB.Height / 2f, AABB.Width, AABB.Height);
                    break;
            }
        }
    }
}
