using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;

namespace Robust.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// Updates the physics simulation.
    /// </summary>
    [UsedImplicitly]
    internal class PhysicsSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IPhysicsManager _physicsManager;
#pragma warning restore 649
        
        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            _physicsManager.UpdateSimulation(TimeSpan.FromSeconds(frameTime));
        }
    }
}
