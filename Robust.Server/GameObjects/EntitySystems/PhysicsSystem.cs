using JetBrains.Annotations;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using System.Linq;

namespace Robust.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class PhysicsSystem : SharedPhysicsSystem
    {
        [Dependency] private readonly IPauseManager _pauseManager = default!;

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            SimulateWorld(frameTime, RelevantEntities.Where(e => !e.Deleted && !_pauseManager.IsEntityPaused(e)).ToList());
        }
    }
}
