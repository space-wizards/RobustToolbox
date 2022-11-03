using System.Collections.Generic;
using System.Linq;
using Robust.Server.Physics;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Robust.Server.GameObjects
{
    public sealed partial class MapSystem : SharedMapSystem
    {
        [Dependency] private readonly IComponentFactory _factory = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IResourceManager _resourceManager = default!;
        [Dependency] private readonly ISerializationManager _serManager = default!;
                     private          IServerEntityManagerInternal _serverEntityManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
        [Dependency] private readonly MetaDataSystem _meta = default!;

        private bool _deleteEmptyGrids;

        public override void Initialize()
        {
            base.Initialize();
            InitializeLoader();
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
                var toDelete = new List<IMapGrid>();

                foreach (var grid in MapManager.GetAllGrids())
                {
                    if (!GridEmpty(grid)) continue;
                    toDelete.Add(grid);
                }

                foreach (var grid in toDelete)
                {
                    MapManager.DeleteGrid(grid.GridEntityId);
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
            if (!_deleteEmptyGrids) return;
            if (!EntityManager.EntityExists(uid)) return;
            if (EntityManager.GetComponent<MetaDataComponent>(uid).EntityLifeStage >= EntityLifeStage.Terminating) return;

            MapManager.DeleteGrid(args.GridId);
        }
    }
}
