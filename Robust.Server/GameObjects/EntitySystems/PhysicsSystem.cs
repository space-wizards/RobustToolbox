using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using System.Linq;
using Robust.Shared.GameObjects.Components;

namespace Robust.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class PhysicsSystem : SharedPhysicsSystem
    {
        [Dependency] private readonly IPauseManager _pauseManager = default!;

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            var collidableComponents = EntityManager.ComponentManager
                .EntityQuery<ICollidableComponent>()
                .ToList();

            SimulateWorld(frameTime, collidableComponents, false);
        }
    }
}
