using JetBrains.Annotations;
using Robust.Shared.GameObjects;
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
        public virtual void UpdateBeforeSolve(bool prediction, PhysicsMap map, float frameTime) {}

        public virtual void UpdateAfterSolve(bool prediction, PhysicsMap map, float frameTime) {}
    }
}
