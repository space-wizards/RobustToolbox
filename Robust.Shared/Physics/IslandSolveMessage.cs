using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;

namespace Robust.Shared.Physics
{
    internal sealed class IslandSolveMessage : EntityEventArgs
    {
        public List<PhysicsComponent> Bodies { get; }

        public IslandSolveMessage(List<PhysicsComponent> bodies)
        {
            Bodies = bodies;
        }
    }
}
