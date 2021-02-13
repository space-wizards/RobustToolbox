using System.Collections.Generic;
using Robust.Shared.GameObjects;

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
