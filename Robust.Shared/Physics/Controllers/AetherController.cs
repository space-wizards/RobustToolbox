using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

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

        public virtual void UpdateBeforeSolve(float frameTime) {}

        public virtual void UpdateAfterSolve(float frameTime) {}
    }
}
