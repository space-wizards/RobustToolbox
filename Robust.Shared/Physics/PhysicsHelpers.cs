using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    public static class PhysicsHelpers
    {
        public static Vector2 GlobalLinearVelocity(this IEntity entity)
        {
            Vector2 result = new Vector2();

            for (ITransformComponent transform = entity.Transform; transform.Parent != null; transform = transform.Parent)
            {
                if (transform.Owner.TryGetComponent(out PhysicsComponent? physicsComponent))
                {
                    result += physicsComponent.LinearVelocity;
                }
            }

            return result;
        }

    }
}
