using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Makes it possible to look this entity up with the snap grid.
    /// </summary>
    public class SnapGridComponent : Component, IComponentDebug
    {
        public sealed override string Name => "SnapGrid";

        internal bool IsSet;
        internal IMapManager _mapManager => IoCManager.Resolve<IMapManager>();

        internal GridId _lastGrid;
        internal Vector2i Position => GetTilePosition(_mapManager, Owner.Transform);

        public static Vector2i GetTilePosition(IMapManager mapManager, ITransformComponent transform)
        {
            var grid = mapManager.GetGrid(transform.GridID);
            return grid.SnapGridCellFor(transform.Coordinates);
        }

        public string GetDebugString()
        {
            return $"pos: {GetTilePosition(_mapManager, Owner.Transform)}";
        }
    }
}
