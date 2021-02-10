using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Shared.Physics.Controllers
{
    [MeansImplicitUse]
    public abstract class AetherController
    {
        [Dependency] protected readonly IComponentManager ComponentManager = default!;
        [Dependency] protected readonly IEntityManager EntityManager = default!;

        public virtual void Initialize()
        {
            IoCManager.InjectDependencies(this);
        }

        // Look I know doing it per map is pretty damn inefficient but I'll deal wit it later.
        public virtual void UpdateBeforeSolve(PhysicsMap map, float frameTime) {}

        public virtual void UpdateAfterSolve(PhysicsMap map, float frameTime) {}
    }
}
