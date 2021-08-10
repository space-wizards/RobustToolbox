using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.Server.GameObjects
{
    internal sealed class MapSystem : SharedMapSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MapGridComponent, EmptyGridEvent>(HandleGridEmpty);
        }

        private void HandleGridEmpty(EntityUid uid, MapGridComponent component, EmptyGridEvent args)
        {
            if (!EntityManager.TryGetEntity(uid, out var gridEnt) ||
                gridEnt.LifeStage >= EntityLifeStage.Terminating) return;

            MapManager.DeleteGrid(args.GridId);
        }
    }
}
