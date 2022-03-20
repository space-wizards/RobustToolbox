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
            var guid = ev.EntityUid;

            if (!EntityManager.EntityExists(guid)) return;
            var collideComp = guid.EnsureComponent<PhysicsComponent>();
            collideComp.CanCollide = true;
            collideComp.BodyType = BodyType.Static;
        }

        protected override void HandleMapCreated(MapChangedEvent eventArgs)
        {
            if (eventArgs.Map == MapId.Nullspace) return;
            var mapUid = MapManager.GetMapEntityIdOrThrow(eventArgs.Map);
            EntityManager.AddComponent<PhysicsMapComponent>(mapUid);
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            SimulateWorld(frameTime, false);
        }
    }
}
