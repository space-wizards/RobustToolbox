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

        private static Box2 SpriteAabbFunc(in SpriteComponent value)
        {
            var worldPos = value.Owner.Transform.WorldPosition;
            var bounds = value.CalculateBoundingBox(worldPos);
            var tree = RenderingTreeSystem.GetRenderTree(value.Owner);

            var offset = -tree?.Owner.Transform.WorldPosition ?? Vector2.Zero;

            return bounds.Translated(offset);
        }

        private static Box2 LightAabbFunc(in PointLightComponent value)
        {
            var worldPos = value.Owner.Transform.WorldPosition;
            var tree = RenderingTreeSystem.GetRenderTree(value.Owner);
            var boxSize = value.Radius * 2;

            var pos = worldPos - tree?.Owner.Transform.WorldPosition ?? Vector2.Zero + value.Offset;

            return Box2.CenteredAround(pos, (boxSize, boxSize));
        }

        internal static Box2 SpriteAabbFunc(SpriteComponent value, Vector2? worldPos = null)
        {
            worldPos ??= value.Owner.Transform.WorldPosition;
            var bounds = value.CalculateBoundingBox(worldPos.Value);
            var tree = RenderingTreeSystem.GetRenderTree(value.Owner);

            var offset = -tree?.Owner.Transform.WorldPosition ?? Vector2.Zero;

            return bounds.Translated(offset);
        }

        internal static Box2 LightAabbFunc(PointLightComponent value, Vector2? worldPos = null)
        {
            worldPos ??= value.Owner.Transform.WorldPosition;
            var tree = RenderingTreeSystem.GetRenderTree(value.Owner);
            var boxSize = value.Radius * 2;

            var pos = worldPos - tree?.Owner.Transform.WorldPosition ?? Vector2.Zero + value.Offset;

            return Box2.CenteredAround(pos, (boxSize, boxSize));
        }
    }
}
