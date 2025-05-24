using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;

namespace Robust.Shared.Physics
{
    internal sealed class IslandSolveMessage(List<Entity<PhysicsComponent>> bodies) : EntityEventArgs
    {
        public List<Entity<PhysicsComponent>> Bodies { get; } = bodies;
    }
}
