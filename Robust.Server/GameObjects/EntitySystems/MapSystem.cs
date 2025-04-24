using System.Linq;
using Robust.Server.GameStates;
using Robust.Shared;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Events;

namespace Robust.Server.GameObjects
{
    public sealed class MapSystem : SharedMapSystem
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly PvsSystem _pvs = default!;

        private bool _deleteEmptyGrids;

        protected override MapId GetNextMapId()
        {
            var id = new MapId(++LastMapId);
            while (MapExists(id) || UsedIds.Contains(id))
            {
                id = new MapId(++LastMapId);
            }
            return id;
        }

        protected override void UpdatePvsChunks(Entity<TransformComponent, MetaDataComponent> grid)
        {
            _pvs.GridParentChanged(grid);
        }

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MapGridComponent, EmptyGridEvent>(HandleGridEmpty);

            Subs.CVar(_cfg, CVars.GameDeleteEmptyGrids, SetGridDeletion, true);
        }

        private void SetGridDeletion(bool value)
        {
            _deleteEmptyGrids = value;

            // If we have any existing empty ones then cull them on setting the cvar
            if (_deleteEmptyGrids)
            {
                var toDelete = new ValueList<EntityUid>();

                var query = AllEntityQuery<MapGridComponent>();
                while (query.MoveNext(out var uid, out var grid))
                {
                    if (!GridEmpty((uid, grid)))
                        continue;
                    toDelete.Add(uid);
                }

                foreach (var uid in toDelete)
                {
                    EntityManager.DeleteEntity(uid);
                }
            }
        }

        private bool GridEmpty(Entity<MapGridComponent> entity)
        {
            return !(GetAllTiles(entity, entity).Any());
        }

        private void HandleGridEmpty(EntityUid uid, MapGridComponent component, EmptyGridEvent args)
        {
            if (!_deleteEmptyGrids || TerminatingOrDeleted(uid) || HasComp<MapComponent>(uid))
                return;

            EntityManager.DeleteEntity(args.GridId);
        }
    }
}
