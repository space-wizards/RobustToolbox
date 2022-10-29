using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using System;

namespace Robust.Shared.Physics
{
    public static class PhysicsHelpers
    {
        [Obsolete("Wtf is this, this isn't how you calculate this???? Use the existing system method instead.")]
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
