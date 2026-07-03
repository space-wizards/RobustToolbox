using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Prometheus;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Profiling;
using Robust.Shared.Timing;

namespace Robust.Shared.Physics.Controllers
{
    [MeansImplicitUse]
    public abstract partial class VirtualController : EntitySystem
    {
        [Dependency] protected SharedPhysicsSystem PhysicsSystem = default!;
        [Dependency] protected SharedTransformSystem TransformSystem = default!;
        [Dependency] private ProfManager _prof = default!;

        private static readonly Stopwatch Stopwatch = new();

        // Cached alongside the histogram labels to skip the virtual GetType() call on the hot path.
        private string _zoneName = string.Empty;

        public Histogram.Child BeforeMonitor = default!;
        public Histogram.Child AfterMonitor = default!;

        #region Boilerplate

        public override void Initialize()
        {
            base.Initialize();

            BeforeMonitor = SharedPhysicsSystem.TickUsageControllerBeforeSolveHistogram.WithLabels(GetType().Name);
            AfterMonitor = SharedPhysicsSystem.TickUsageControllerAfterSolveHistogram.WithLabels(GetType().Name);
            _zoneName = GetType().Name;

            var updatesBefore = UpdatesBefore.ToArray();
            var updatesAfter = UpdatesAfter.ToArray();

            SubscribeLocalEvent<PhysicsUpdateBeforeSolveEvent>(OnBeforeSolve, updatesBefore, updatesAfter);
            SubscribeLocalEvent<PhysicsUpdateAfterSolveEvent>(OnAfterSolve, updatesBefore, updatesAfter);
        }

        private void OnBeforeSolve(ref PhysicsUpdateBeforeSolveEvent ev)
        {
            if(PhysicsSystem.MetricsEnabled)
                Stopwatch.Restart();

            using (_prof.Group(_zoneName))
            {
                UpdateBeforeSolve(ev.Prediction, ev.DeltaTime);
            }

            if(PhysicsSystem.MetricsEnabled)
                BeforeMonitor.Observe(Stopwatch.Elapsed.TotalSeconds);
        }

        private void OnAfterSolve(ref PhysicsUpdateAfterSolveEvent ev)
        {
            if(PhysicsSystem.MetricsEnabled)
                Stopwatch.Restart();

            using (_prof.Group(_zoneName))
            {
                UpdateAfterSolve(ev.Prediction, ev.DeltaTime);
            }

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
