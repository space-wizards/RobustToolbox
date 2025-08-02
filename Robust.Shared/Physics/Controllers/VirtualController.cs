using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Prometheus;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Robust.Shared.Physics.Controllers
{
    [MeansImplicitUse]
    public abstract class VirtualController : EntitySystem
    {
        [Dependency] protected readonly SharedPhysicsSystem PhysicsSystem = default!;
        [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;

        private static readonly Stopwatch Stopwatch = new();

        public Histogram.Child BeforeMonitor = default!;
        public Histogram.Child AfterMonitor = default!;

        #region Boilerplate

        public override void Initialize()
        {
            base.Initialize();

            BeforeMonitor = SharedPhysicsSystem.TickUsageControllerBeforeSolveHistogram.WithLabels(GetType().Name);
            AfterMonitor = SharedPhysicsSystem.TickUsageControllerAfterSolveHistogram.WithLabels(GetType().Name);

            var updatesBefore = UpdatesBefore.ToArray();
            var updatesAfter = UpdatesAfter.ToArray();

            SubscribeLocalEvent<PhysicsUpdateBeforeSolveEvent>(OnBeforeSolve, updatesBefore, updatesAfter);
            SubscribeLocalEvent<PhysicsUpdateAfterSolveEvent>(OnAfterSolve, updatesBefore, updatesAfter);
        }

        private void OnBeforeSolve(ref PhysicsUpdateBeforeSolveEvent ev)
        {
            if(PhysicsSystem.MetricsEnabled)
                Stopwatch.Restart();

            UpdateBeforeSolve(ev.Prediction, ev.DeltaTime);

            if(PhysicsSystem.MetricsEnabled)
                BeforeMonitor.Observe(Stopwatch.Elapsed.TotalSeconds);
        }

        private void OnAfterSolve(ref PhysicsUpdateAfterSolveEvent ev)
        {
            if(PhysicsSystem.MetricsEnabled)
                Stopwatch.Restart();

            UpdateAfterSolve(ev.Prediction, ev.DeltaTime);

            if(PhysicsSystem.MetricsEnabled)
                AfterMonitor.Observe(Stopwatch.Elapsed.TotalSeconds);
        }

        #endregion

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
    }
}
