using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;

namespace Robust.Shared.Physics
{
    internal sealed class IslandSolveMessage : EntityEventArgs
    {
        public List<Entity<PhysicsComponent, TransformComponent>> Bodies { get; }

        public IslandSolveMessage(List<Entity<PhysicsComponent, TransformComponent>> bodies)
        {
            Bodies = bodies;
        }
    }
}
