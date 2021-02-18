using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
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
