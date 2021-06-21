using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Client.GameObjects
{
    [RegisterComponent]
    public sealed class RenderingTreeComponent : Component
    {
        public override string Name => "RenderingTree";

        internal DynamicTree<SpriteComponent> SpriteTree { get; private set; } = new(SpriteAabbFunc);
        internal DynamicTree<PointLightComponent> LightTree { get; private set; } = new(LightAabbFunc);

        internal static Box2 SpriteAabbFunc(in SpriteComponent value)
        {
            var worldPos = value.Owner.Transform.WorldPosition;
            return new Box2(worldPos, worldPos);
        }

        internal static Box2 LightAabbFunc(in PointLightComponent value)
        {
            var worldPos = value.Owner.Transform.WorldPosition;

            var boxSize = value.Radius * 2;
            return Box2.CenteredAround(worldPos, (boxSize, boxSize));
        }
    }
}
