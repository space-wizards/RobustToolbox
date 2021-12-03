using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    public static class PhysicsHelpers
    {
        public static Vector2 GlobalLinearVelocity(this EntityUid entity)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            Vector2 result = new Vector2();

            for (TransformComponent transform = entMan.GetComponent<TransformComponent>(entity); transform.Parent != null; transform = transform.Parent)
            {
                if (entMan.TryGetComponent(transform.Owner, out PhysicsComponent? physicsComponent))
                {
                    result += physicsComponent.LinearVelocity;
                }
            }

            return result;
        }

    }
}
