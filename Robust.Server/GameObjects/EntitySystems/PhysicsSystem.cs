using JetBrains.Annotations;
using Robust.Server.Physics;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Robust.Server.GameObjects
{
    [UsedImplicitly]
    public sealed class PhysicsSystem : SharedPhysicsSystem
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GridInitializeEvent>(HandleGridInit);
            LoadMetricCVar();
            _configurationManager.OnValueChanged(CVars.MetricsEnabled, _ => LoadMetricCVar());
        }

        private void LoadMetricCVar()
        {
            MetricsEnabled = _configurationManager.GetCVar(CVars.MetricsEnabled);
        }

        private void HandleGridInit(GridInitializeEvent ev)
        {
            // If the map is also a grid then it doesn't have physics.
            if (ev.IsMap)
                return;

            var guid = ev.EntityUid;

            if (!EntityManager.EntityExists(guid)) return;
            var collideComp = guid.EnsureComponent<PhysicsComponent>();
            collideComp.CanCollide = true;
            collideComp.BodyType = BodyType.Static;
        }

        protected override void OnMapAdded(ref MapChangedEvent eventArgs)
        {
            if (eventArgs.Map == MapId.Nullspace) return;
            EnsureComp<PhysicsMapComponent>(MapManager.GetMapEntityId(eventArgs.Map));
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            SimulateWorld(frameTime, false);
        }
    }
}
