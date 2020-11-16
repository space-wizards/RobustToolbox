using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using System.Linq;
using Robust.Shared.GameObjects.Components;

namespace Robust.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class PhysicsSystem : SharedPhysicsSystem
    {
        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            SimulateWorld(frameTime, false);
        }
    }
}
