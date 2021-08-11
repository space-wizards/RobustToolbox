using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Server.GameObjects
{
    internal sealed class MapSystem : SharedMapSystem
    {
        private bool _deleteEmptyGrids;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MapGridComponent, EmptyGridEvent>(HandleGridEmpty);

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CVars.GameDeleteEmptyGrids, SetGridDeletion, true);
        }

        private void SetGridDeletion(bool value) => _deleteEmptyGrids = value;

        public override void Shutdown()
        {
            base.Shutdown();
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.UnsubValueChanged(CVars.GameDeleteEmptyGrids, SetGridDeletion);
        }

        private void HandleGridEmpty(EntityUid uid, MapGridComponent component, EmptyGridEvent args)
        {
            if (!_deleteEmptyGrids ||
                !EntityManager.TryGetEntity(uid, out var gridEnt) ||
                gridEnt.LifeStage >= EntityLifeStage.Terminating) return;

            MapManager.DeleteGrid(args.GridId);
        }
    }
}
