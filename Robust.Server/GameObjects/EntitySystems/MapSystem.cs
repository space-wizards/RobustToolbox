using System.Linq;
using Robust.Server.GameStates;
using Robust.Shared;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Events;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Server.GameObjects
{
    public sealed class MapSystem : SharedMapSystem
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly PvsSystem _pvs = default!;

        private bool _deleteEmptyGrids;

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
                var toDelete = new ValueList<EntityUid>();

                var query = AllEntityQuery<MapGridComponent>();
                while (query.MoveNext(out var uid, out var grid))
                {
                    if (!GridEmpty(grid)) continue;
                    toDelete.Add(uid);
                }

                foreach (var uid in toDelete)
                {
                    MapManager.DeleteGrid(uid);
                }
            }
        }

        private bool GridEmpty(MapGridComponent grid)
        {
            return !(grid.GetAllTiles().Any());
        }

        private void HandleGridEmpty(EntityUid uid, MapGridComponent component, EmptyGridEvent args)
        {
            if (!_deleteEmptyGrids || TerminatingOrDeleted(uid) || HasComp<MapComponent>(uid))
                return;

            MapManager.DeleteGrid(args.GridId);
        }
    }
}
