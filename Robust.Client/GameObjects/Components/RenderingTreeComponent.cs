using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
            var (worldPos, worldRot) = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(value.Owner).GetWorldPositionRotation();
            var bounds = new Box2Rotated(value.CalculateBoundingBox(worldPos), worldRot, worldPos);
            var tree = RenderingTreeSystem.GetRenderTree(value.Owner);

            var localAABB = (tree != null ? IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(tree.Owner) : null).InvWorldMatrix.TransformBox(bounds) ?? bounds.CalcBoundingBox();

            return localAABB;
        }

        private static Box2 LightAabbFunc(in PointLightComponent value)
        {
            var worldPos = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(value.Owner).WorldPosition;
            var tree = RenderingTreeSystem.GetRenderTree(value.Owner);
            var boxSize = value.Radius * 2;

            var localPos = (tree != null ? IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(tree.Owner) : null).InvWorldMatrix.Transform(worldPos) ?? worldPos;

            return Box2.CenteredAround(localPos, (boxSize, boxSize));
        }

        internal static Box2 SpriteAabbFunc(SpriteComponent value, Vector2? worldPos = null, Angle? worldRot = null)
        {
            worldPos ??= IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(value.Owner).WorldPosition;
            worldRot ??= IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(value.Owner).WorldRotation;
            var bounds = new Box2Rotated(value.CalculateBoundingBox(worldPos.Value), worldRot.Value, worldPos.Value);
            var tree = RenderingTreeSystem.GetRenderTree(value.Owner);

            var localAABB = (tree != null ? IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(tree.Owner) : null).InvWorldMatrix.TransformBox(bounds) ?? bounds.CalcBoundingBox();

            return localAABB;
        }

        internal static Box2 LightAabbFunc(PointLightComponent value, Vector2? worldPos = null)
        {
            // Lights are circles so don't need entity's rotation
            worldPos ??= IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(value.Owner).WorldPosition;
            var tree = RenderingTreeSystem.GetRenderTree(value.Owner);
            var boxSize = value.Radius * 2;

            var localPos = (tree != null ? IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(tree.Owner) : null).InvWorldMatrix.Transform(worldPos.Value) ?? worldPos.Value;

            return Box2.CenteredAround(localPos, (boxSize, boxSize));
        }
    }
}
