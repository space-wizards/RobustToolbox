using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    /// <inheritdoc />
    public class PhysicsManager : IPhysicsManager
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public bool IsWeightless(EntityCoordinates coordinates)
        {
            var gridId = coordinates.GetGridId(_entityManager);
            if (!gridId.IsValid())
            {
                // Not on a grid = no gravity for now.
                // In the future, may want to allow maps to override to always have gravity instead.
                return true;
            }

            var tile = _mapManager.GetGrid(gridId).GetTileRef(coordinates).Tile;
            return !_mapManager.GetGrid(gridId).HasGravity || tile.IsEmpty;
        }

        /// <summary>
        ///     Calculates the normal vector for two colliding bodies
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Vector2 CalculateNormal(IPhysBody target, IPhysBody source)
        {
            var manifold = target.GetWorldAABB().Intersect(source.GetWorldAABB());
            if (manifold.IsEmpty()) return Vector2.Zero;
            if (manifold.Height > manifold.Width)
            {
                // X is the axis of seperation
                var leftDist = source.GetWorldAABB().Right - target.GetWorldAABB().Left;
                var rightDist = target.GetWorldAABB().Right - source.GetWorldAABB().Left;
                return new Vector2(leftDist > rightDist ? 1 : -1, 0);
            }
            else
            {
                // Y is the axis of seperation
                var bottomDist = source.GetWorldAABB().Top - target.GetWorldAABB().Bottom;
                var topDist = target.GetWorldAABB().Top - source.GetWorldAABB().Bottom;
                return new Vector2(0, bottomDist > topDist ? 1 : -1);
            }
        }

        public float CalculatePenetration(IPhysBody target, IPhysBody source)
        {
            var manifold = target.GetWorldAABB().Intersect(source.GetWorldAABB());
            if (manifold.IsEmpty()) return 0.0f;
            return manifold.Height > manifold.Width ? manifold.Width : manifold.Height;
        }
    }
}
