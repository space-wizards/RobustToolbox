using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Prometheus;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Shared.Physics.Controllers
{
    [MeansImplicitUse]
    public abstract class VirtualController
    {
        [Dependency]
        [Obsolete("Use EntityManager instead.")]
        protected readonly IComponentManager ComponentManager = default!;

        [Dependency] protected readonly IEntityManager EntityManager = default!;

        public Histogram.Child BeforeMonitor = default!;
        public Histogram.Child AfterMonitor = default!;

        public virtual List<Type> UpdatesBefore => new();

        public virtual List<Type> UpdatesAfter => new();

        public virtual void Initialize()
        {
            IoCManager.InjectDependencies(this);
        }

        public virtual void Shutdown() {}

        /// <summary>
        ///     Run before any map processing starts.
        /// </summary>
        /// <param name="prediction"></param>
        /// <param name="frameTime"></param>
        public virtual void UpdateBeforeSolve(bool prediction, float frameTime) {}

        /// <summary>
        ///     Run after all map processing has finished.
        /// </summary>
        /// <param name="prediction"></param>
        /// <param name="frameTime"></param>
        public virtual void UpdateAfterSolve(bool prediction, float frameTime) {}

        /// <summary>
        ///     Run before a particular map starts.
        /// </summary>
        /// <param name="prediction"></param>
        /// <param name="mapComponent"></param>
        /// <param name="frameTime"></param>
        public virtual void UpdateBeforeMapSolve(bool prediction, SharedPhysicsMapComponent mapComponent, float frameTime) {}

        /// <summary>
        ///     Run after a particular map finishes.
        /// </summary>
        /// <param name="prediction"></param>
        /// <param name="mapComponent"></param>
        /// <param name="frameTime"></param>
        public virtual void UpdateAfterMapSolve(bool prediction, SharedPhysicsMapComponent mapComponent, float frameTime) {}
    }
}
