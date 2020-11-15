using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

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
