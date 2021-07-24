using JetBrains.Annotations;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Robust.Server.GameObjects
{
    [UsedImplicitly]
    public class PhysicsSystem : SharedPhysicsSystem
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

            if (!EntityManager.TryGetEntity(guid, out var gridEntity)) return;
            var collideComp = gridEntity.EnsureComponent<PhysicsComponent>();
            collideComp.CanCollide = true;
            collideComp.BodyType = BodyType.Static;
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            SimulateWorld(frameTime, false);
        }
    }
}
