using System.Collections.Generic;
using System.Linq;
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

        private void SetGridDeletion(bool value)
        {
            _deleteEmptyGrids = value;

            // If we have any existing empty ones then cull them on setting the cvar
            if (_deleteEmptyGrids)
            {
                var toDelete = new List<IMapGrid>();

                foreach (var grid in MapManager.GetAllGrids())
                {
                    if (!GridEmpty(grid)) continue;
                    toDelete.Add(grid);
                }

                foreach (var grid in toDelete)
                {
                    MapManager.DeleteGrid(grid.Index);
                }
            }
        }

        private bool GridEmpty(IMapGrid grid)
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
            if (!_deleteEmptyGrids ||
                !EntityManager.TryGetEntity(uid, out var gridEnt) ||
                (!IoCManager.Resolve<IEntityManager>().EntityExists(gridEnt) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(gridEnt).EntityLifeStage) >= EntityLifeStage.Terminating) return;

            MapManager.DeleteGrid(args.GridId);
        }
    }
}
