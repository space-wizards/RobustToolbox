using System.Collections.Generic;
using System.Linq;
using Robust.Server.Physics;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Server.GameObjects
{
    public sealed class MapSystem : SharedMapSystem
    {
        private bool _deleteEmptyGrids;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MapGridComponent, EmptyGridEvent>(HandleGridEmpty);

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CVars.GameDeleteEmptyGrids, SetGridDeletion, true);
        }

        protected override void OnMapAdd(EntityUid uid, MapComponent component, ComponentAdd args)
        {
            EnsureComp<PhysicsMapComponent>(uid);
        }

        private void SetGridDeletion(bool value)
        {
            _deleteEmptyGrids = value;

            // If we have any existing empty ones then cull them on setting the cvar
            if (_deleteEmptyGrids)
            {
                var toDelete = new List<MapGridComponent>();

                foreach (var grid in MapManager.EntityManager.EntityQuery<MapGridComponent>())
                {
                    if (!GridEmpty(grid)) continue;
                    toDelete.Add(grid);
                }

                foreach (var grid in toDelete)
                {
                    EntityManager.DeleteEntity(grid.Owner);
                }
            }
        }

        private bool GridEmpty(MapGridComponent grid)
        {
            return !(grid.GetAllTiles().Any());
        }

        public override void Shutdown()
        {
            base.Shutdown();
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.UnsubValueChanged(CVars.GameDeleteEmptyGrids, SetGridDeletion);
        }

        private void HandleGridEmpty(EntityUid uid, MapGridComponent component, EmptyGridEvent args)
        {
            if (!_deleteEmptyGrids) return;
            if (!EntityManager.EntityExists(uid)) return;
            if (EntityManager.GetComponent<MetaDataComponent>(uid).EntityLifeStage >= EntityLifeStage.Terminating) return;

            EntityManager.DeleteEntity(args.GridId);
        }
    }
}
