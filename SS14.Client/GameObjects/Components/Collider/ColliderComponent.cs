using SFML.Graphics;
using SFML.System;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace SS14.Client.GameObjects
{
    public class ColliderComponent : ClientComponent, IColliderComponent
    {
        public Color DebugColor { get; set; } = Color.Blue;

        private FloatRect AABB
        {
            get
            {
                if (Owner.HasComponent<BoundingBoxComponent>())
                    return Owner.GetComponent<BoundingBoxComponent>().AABB.Convert();
                if (Owner.HasComponent<IRenderableComponent>())
                    return Owner.GetComponent<IRenderableComponent>().AverageAABB;
                return new FloatRect();
            }
        }

        public FloatRect WorldAABB
        {
            get
            {
                var trans = Owner.GetComponent<ITransformComponent>();
                if (trans == null)
                    return AABB;
                return new FloatRect(
                    AABB.Left + trans.Position.X,
                    AABB.Top + trans.Position.Y,
                    AABB.Width,
                    AABB.Height);
            }
        }

        public override string Name => "Collider";
        public override uint? NetID => NetIDs.COLLIDER;

        public override void LoadParameters(YamlMappingNode mapping)
        {
            YamlNode node;
            if (mapping.TryGetNode("debugColor", out node))
                DebugColor = node.AsHexColor(Color.Blue);
        }

        public bool TryCollision(Vector2f offset, bool bump = false)
        {
            return IoCManager.Resolve<ICollisionManager>().TryCollide(Owner, offset, bump);
        }
    }
}
