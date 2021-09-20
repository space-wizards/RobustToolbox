using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// A positional lookup system for entities.
    /// </summary>
    public sealed class QuerySystem : EntitySystem
    {
        [Dependency] private readonly IEntityLookup _lookup = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SharedBroadphaseSystem _broadphaseSystem = default!;

        #region AnyIntersecting

        /// <summary>
        /// True if we find any entities overlapping.
        /// Does not necessarily mean these are overlapping for physics purposes.
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public bool AnyEntitiesIntersecting(EntityUid uid, QueryFlags flags)
        {
            return GetEntitiesIntersecting(uid, flags).Any();
        }

        public bool AnyEntitiesIntersecting(MapId mapId, Box2 worldAABB, QueryFlags flags)
        {
            return GetEntitiesIntersecting(mapId, worldAABB, flags).Any();
        }

        public bool AnyEntitiesIntersecting(MapId mapId, Box2Rotated worldBounds, QueryFlags flags)
        {
            return GetEntitiesIntersecting(mapId, worldBounds, flags).Any();
        }

        public bool AnyEntitiesIntersecting(EntityCoordinates coordinates, QueryFlags flags)
        {
            return GetEntitiesIntersecting(coordinates, flags).Any();
        }

        public bool AnyEntitiesIntersecting(MapCoordinates coordinates, QueryFlags flags)
        {
            return GetEntitiesIntersecting(coordinates, flags).Any();
        }

        public bool AnyEntitiesIntersecting(TileRef tileRef, QueryFlags flags)
        {
            return GetEntitiesIntersecting(tileRef, flags).Any();
        }

        #endregion

        #region GetIntersecting

        public IEnumerable<EntityUid> GetEntitiesIntersecting(EntityUid entityUid, QueryFlags flags)
        {
            var xform = ComponentManager.GetComponent<TransformComponent>(entityUid);
            var bounds = GetBounds(entityUid);
            return GetEntitiesIntersecting(xform.MapID, bounds, flags);
        }

        public IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2 worldAABB, QueryFlags flags)
        {
            if (mapId == MapId.Nullspace) yield break;

            if ((flags & QueryFlags.Anchored) != 0x0)
            {
                foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
                {
                    foreach (var uid in grid.GetAnchoredEntities(worldAABB))
                    {
                        if (!EntityManager.EntityExists(uid)) continue;
                        yield return uid;
                    }
                }
            }

            if ((flags & QueryFlags.EntityLookup) != 0x0)
            {
                foreach (var uid in _lookup.GetEntitiesIntersecting(mapId, worldAABB))
                {
                    if (!EntityManager.EntityExists(uid)) continue;
                    yield return uid;
                }
            }

            if ((flags & QueryFlags.Physics) != 0x0)
            {
                // TODO: Query
            }
        }

        public IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2Rotated worldBounds, QueryFlags flags)
        {
            if (mapId == MapId.Nullspace) yield break;

            if ((flags & QueryFlags.Anchored) != 0x0)
            {
                foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
                {
                    foreach (var uid in grid.GetAnchoredEntities(worldBounds))
                    {
                        if (!EntityManager.EntityExists(uid)) continue;
                        yield return uid;
                    }
                }
            }

            if ((flags & QueryFlags.EntityLookup) != 0x0)
            {
                foreach (var uid in _lookup.GetEntitiesIntersecting(mapId, worldBounds))
                {
                    yield return uid;
                }
            }

            if ((flags & QueryFlags.Physics) != 0x0)
            {
                // TODO: Query
            }
        }

        public IEnumerable<EntityUid> GetEntitiesIntersecting(EntityCoordinates coordinates, QueryFlags flags)
        {
            var mapCoordinates = coordinates.ToMap(EntityManager);
            return GetEntitiesIntersecting(mapCoordinates, flags);
        }

        public IEnumerable<EntityUid> GetEntitiesIntersecting(MapCoordinates coordinates, QueryFlags flags)
        {
            if (coordinates.MapId == MapId.Nullspace) yield break;

            if ((flags & QueryFlags.Anchored) != 0x0 &&
                _mapManager.TryFindGridAt(coordinates, out var grid))
            {
                foreach (var uid in grid.GetAnchoredEntities(coordinates))
                {
                    if (!EntityManager.EntityExists(uid)) continue;
                    yield return uid;
                }
            }

            if ((flags & QueryFlags.EntityLookup) != 0x0)
            {
                // TODO: Lookup
            }

            if ((flags & QueryFlags.Physics) != 0x0)
            {
                // TODO: Query
            }
        }

        public IEnumerable<EntityUid> GetEntitiesIntersecting(TileRef tileRef, QueryFlags flags)
        {
            var bounds = GetBounds(tileRef);
            return GetEntitiesIntersecting(tileRef.MapIndex, bounds, flags);
        }

        #endregion

        #region GetInRange

        public IEnumerable<EntityUid> GetEntitiesInRange(EntityUid entityUid, float range, QueryFlags flags)
        {
            var xform = ComponentManager.GetComponent<TransformComponent>(entityUid);
            foreach (var uid in GetEntitiesInRange(xform.MapPosition, range, flags))
            {
                if (uid == entityUid) continue;
                yield return uid;
            }
        }

        public IEnumerable<EntityUid> GetEntitiesInRange(EntityCoordinates coordinates, float range, QueryFlags flags)
        {
            var mapCoordinates = coordinates.ToMap(EntityManager);

            return GetEntitiesInRange(mapCoordinates, range, flags);
        }

        public IEnumerable<EntityUid> GetEntitiesInRange(MapCoordinates coordinates, float range, QueryFlags flags)
        {
            // TODO: Technically we should consider the edges of things but we need CollisionManager to be implemented
            // and even then it would rely heavily upon physics.

            if ((flags & QueryFlags.Anchored) != 0x0)
            {

            }

            if ((flags & QueryFlags.EntityLookup) != 0x0)
            {

            }

            if ((flags & QueryFlags.Physics) != 0x0)
            {

            }

            throw new NotImplementedException();
        }

        public IEnumerable<EntityUid> GetEntitiesInRange(TileRef tileRef, float range, QueryFlags flags)
        {
            var coordinates = _mapManager.GetGrid(tileRef.GridIndex).InvWorldMatrix
                .Transform((Vector2) tileRef.GridIndices + 0.5f);

            return GetEntitiesInRange(new MapCoordinates(coordinates, tileRef.MapIndex), range, flags);
        }
        #endregion

        #region AABB methods

        /*
         * The reason we even consider Box2Rotated is because we may want "AABB" stuff to still function the same
         * regardless if a grid is rotated.
         */

        public Box2Rotated GetBounds(TileRef tileRef)
        {
            var grid = _mapManager.GetGrid(tileRef.GridIndex);
            var gridXform = ComponentManager.GetComponent<TransformComponent>(grid.GridEntityId);

            var center = gridXform.WorldMatrix.Transform((Vector2) tileRef.GridIndices + 0.5f);

            return new Box2Rotated(Box2.UnitCentered.Translated(center), -gridXform.WorldRotation, center);
        }

        /// <summary>
        /// Get the world bounds of this entity.
        /// Rotation will be relative to its grid; if this is default will be relative to the map.
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public Box2Rotated GetBounds(EntityUid uid)
        {
            var xform = ComponentManager.GetComponent<TransformComponent>(uid);
            TransformComponent parentXform;

            if (xform.GridID == GridId.Invalid)
            {
                parentXform = ComponentManager.GetComponent<TransformComponent>(_mapManager.GetMapEntity(xform.MapID).Uid);
            }
            else
            {
                parentXform = ComponentManager.GetComponent<TransformComponent>(_mapManager.GetGrid(xform.GridID).GridEntityId);
            }

            if (ComponentManager.TryGetComponent<PhysicsComponent>(uid, out var body))
            {
                var parentInvMatrix = parentXform.InvWorldMatrix;
                var localXform = parentInvMatrix.Transform(xform.WorldPosition);

                var aabb = new Box2();
                var localRot = xform.WorldRotation - parentXform.WorldRotation;
                var transform = new Transform(localXform, localRot);

                foreach (var fixture in body.Fixtures)
                {
                    for (var i = 0; i < fixture.Shape.ChildCount; i++)
                    {
                        aabb = aabb.IsEmpty() ?
                            fixture.Shape.ComputeAABB(transform, i) :
                            aabb.Union(fixture.Shape.ComputeAABB(transform, i));
                    }
                }

                var translatedAABB = parentXform.WorldMatrix.TransformBox(aabb);

                return new Box2Rotated(translatedAABB, -parentXform.WorldRotation, translatedAABB.Center);
            }

            var worldPos = xform.WorldPosition;

            return new Box2Rotated(Box2.UnitCentered.Translated(worldPos), -parentXform.WorldRotation, worldPos);
        }

        /// <summary>
        /// Gets the AABB of this entity relative to its grid.
        /// Will use the map if it is on the default grid.
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public Box2 GetGridAABB(EntityUid uid)
        {
            var xform = ComponentManager.GetComponent<TransformComponent>(uid);
            TransformComponent parentXform;

            if (xform.GridID == GridId.Invalid)
            {
                parentXform = ComponentManager.GetComponent<TransformComponent>(_mapManager.GetMapEntity(xform.MapID).Uid);
            }
            else
            {
                parentXform = ComponentManager.GetComponent<TransformComponent>(_mapManager.GetGrid(xform.GridID).GridEntityId);
            }

            var parentInvMatrix = parentXform.InvWorldMatrix;
            var localXform = parentInvMatrix.Transform(xform.WorldPosition);

            if (ComponentManager.TryGetComponent<PhysicsComponent>(uid, out var body))
            {
                var aabb = new Box2();
                var localRot = xform.WorldRotation - parentXform.WorldRotation;
                var transform = new Transform(localXform, localRot);

                foreach (var fixture in body.Fixtures)
                {
                    for (var i = 0; i < fixture.Shape.ChildCount; i++)
                    {
                        aabb = aabb.IsEmpty() ?
                            fixture.Shape.ComputeAABB(transform, i) :
                            aabb.Union(fixture.Shape.ComputeAABB(transform, i));
                    }
                }

                return aabb;
            }

            return Box2.UnitCentered.Translated(localXform);
        }

        #endregion
    }

    [Flags]
    public enum QueryFlags : ushort
    {
        None = 0,
        Anchored = 1 << 0,
        Physics = 1 << 1,
        EntityLookup = 1 << 2,
    }
}
