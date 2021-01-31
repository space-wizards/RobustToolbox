using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;

namespace Robust.Shared.Physics
{
    internal class IslandSolveMessage : EntitySystemMessage
    {
        public List<PhysicsComponent> Bodies { get; }

        public IslandSolveMessage(List<PhysicsComponent> bodies)
        {
            Bodies = bodies;
        }
    }
}
