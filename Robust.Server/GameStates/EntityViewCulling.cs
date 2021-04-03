using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.ObjectPool;
using Robust.Server.GameObjects;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates
{
    internal class EntityViewCulling
    {
        private const int ViewSetCapacity = 128; // starting number of entities that are in view
        private const int PlayerSetSize = 64; // Starting number of players
        private const int MaxVisPoolSize = 1024; // Maximum number of pooled objects

        private static readonly Vector2 Vector2NaN = new(float.NaN, float.NaN);

        private readonly IServerEntityManager _entMan;
        private readonly IComponentManager _compMan;
        private readonly IMapManager _mapManager;
        private IEntityLookup _lookup;

        private readonly Dictionary<ICommonSession, HashSet<EntityUid>> _playerVisibleSets = new(PlayerSetSize);

        private readonly ConcurrentDictionary<ICommonSession, GameTick> _playerLastFullMap = new();

        private readonly List<(GameTick tick, EntityUid uid)> _deletionHistory = new();

        private readonly ObjectPool<HashSet<EntityUid>> _visSetPool
            = new DefaultObjectPool<HashSet<EntityUid>>(new DefaultPooledObjectPolicy<HashSet<EntityUid>>(), MaxVisPoolSize);

        private readonly ObjectPool<HashSet<EntityUid>> _viewerEntsPool
            = new DefaultObjectPool<HashSet<EntityUid>>(new DefaultPooledObjectPolicy<HashSet<EntityUid>>(), MaxVisPoolSize);

        /// <summary>
        /// Is view culling enabled, or will we send the whole map?
        /// </summary>
        public bool CullingEnabled { get; set; }

        /// <summary>
        /// Size of the side of the view bounds square.
        /// </summary>
        public float ViewSize { get; set; }

        public EntityViewCulling(IServerEntityManager entMan, IMapManager mapManager)
        {
            _entMan = entMan;
            _compMan = entMan.ComponentManager;
            _mapManager = mapManager;
            _compMan = _entMan.ComponentManager;
            _lookup = _entMan.EntityLookup;
        }

        // Not thread safe
        public void EntityDeleted(EntityUid e)
        {
            // Not aware of prediction
            _deletionHistory.Add((_entMan.CurrentTick, e));
        }

        // Not thread safe
        public void CullDeletionHistory(GameTick oldestAck)
        {
            _deletionHistory.RemoveAll(hist => hist.tick < oldestAck);
        }

        private List<EntityUid> GetDeletedEntities(GameTick fromTick)
        {
            var list = new List<EntityUid>();
            foreach (var (tick, id) in _deletionHistory)
            {
                if (tick >= fromTick) list.Add(id);
            }

            return list;
        }

        // Not thread safe
        public void AddPlayer(ICommonSession session)
        {
            _playerVisibleSets.Add(session, new HashSet<EntityUid>(ViewSetCapacity));
        }

        // Not thread safe
        public void RemovePlayer(ICommonSession session)
        {
            _playerVisibleSets.Remove(session);
            _playerLastFullMap.Remove(session, out _);
        }

        // thread safe
        public bool IsPointVisible(ICommonSession session, in MapCoordinates position)
        {
            var viewables = GetSessionViewers(session);

            bool CheckInView(MapCoordinates mapCoordinates, HashSet<EntityUid> entityUids)
            {
                foreach (var euid in entityUids)
                {
                    var (viewBox, mapId) = CalcViewBounds(in euid);

                    if (mapId != mapCoordinates.MapId)
                        continue;

                    if (!CullingEnabled)
                        return true;

                    if (viewBox.Contains(mapCoordinates.Position))
                        return true;
                }

                return false;
            }

            bool result = CheckInView(position, viewables);

            viewables.Clear();
            _viewerEntsPool.Return(viewables);
            return result;
        }

        private HashSet<EntityUid> GetSessionViewers(ICommonSession session)
        {
            var viewers = _viewerEntsPool.Get();
            if (session.Status != SessionStatus.InGame || session.AttachedEntityUid is null)
                return viewers;

            var query = _compMan.EntityQuery<BasicActorComponent>();

            foreach (var actorComp in query)
            {
                if (actorComp.playerSession == session)
                    viewers.Add(actorComp.Owner.Uid);
            }

            return viewers;
        }

        // thread safe
        public (List<EntityState>? updates, List<EntityUid>? deletions) CalculateEntityStates(ICommonSession session, GameTick fromTick, GameTick toTick)
        {
            DebugTools.Assert(session.Status == SessionStatus.InGame);

            //TODO: Stop sending all entities to every player first tick
            List<EntityUid>? deletions;
            if (!CullingEnabled || fromTick == GameTick.Zero)
            {
                var allStates = ServerGameStateManager.GetAllEntityStates(_entMan, session, fromTick);
                deletions = GetDeletedEntities(fromTick);
                _playerLastFullMap.AddOrUpdate(session, toTick, (_, _) => toTick);
                return (allStates, deletions);
            }

            var lastMapUpdate = _playerLastFullMap.GetValueOrDefault(session);
            var currentViewSet = CalcCurrentViewSet(session);

            deletions = GetDeletedEntities(fromTick);
            var entityStates = GenerateEntityStates(session, fromTick, currentViewSet, deletions, lastMapUpdate);

            // no point sending an empty collection
            entityStates = entityStates.Count == 0 ? default : entityStates;
            deletions = deletions.Count == 0 ? default : deletions;

            return (entityStates, deletions);
        }

        private List<EntityState> GenerateEntityStates(ICommonSession session, GameTick fromTick, HashSet<EntityUid> currentSet, List<EntityUid> deletions, GameTick lastMapUpdate)
        {
            // pretty big allocations :(
            List<EntityState> entityStates = new(currentSet.Count);
            var previousSet = _playerVisibleSets[session];

            // complement set
            foreach (var entityUid in previousSet)
            {
                // Still inside PVS
                if (currentSet.Contains(entityUid))
                    continue;

                // it was deleted, so we don't need to exit PVS
                if (deletions.Contains(entityUid))
                    continue;

                //TODO: HACK: somehow an entity left the view, transform does not exist (deleted?), but was not in the
                // deleted list. This seems to happen with the map entity on round restart.
                if (!_entMan.EntityExists(entityUid))
                    continue;

                // Anchored entities don't ever leave
                if (_compMan.HasComponent<SnapGridComponent>(entityUid))
                    continue;

                // PVS leave message
                //TODO: Remove NaN as the signal to leave PVS
                var xform = _compMan.GetComponent<ITransformComponent>(entityUid);
                var oldState = (TransformComponent.TransformComponentState) xform.GetComponentState(session);

                entityStates.Add(new EntityState(entityUid,
                    new ComponentChanged[]
                    {
                        new(false, NetIDs.TRANSFORM, "Transform")
                    },
                    new ComponentState[]
                    {
                        new TransformComponent.TransformComponentState(Vector2NaN,
                            oldState.Rotation,
                            oldState.ParentID,
                            oldState.NoLocalRotation)
                    }));
            }

            foreach (var entityUid in currentSet)
            {
                if (previousSet.Contains(entityUid))
                {
                    //Still Visible
                    // only send new changes
                    var newState = ServerGameStateManager.GetEntityState(_entMan.ComponentManager, session, entityUid, fromTick);

                    if (!newState.Empty)
                        entityStates.Add(newState);
                }
                else
                {
                    // PVS enter message

                    // skip sending anchored entities (walls)
                    if (_compMan.HasComponent<SnapGridComponent>(entityUid) && _entMan.GetEntity(entityUid).LastModifiedTick <= lastMapUpdate)
                        continue;

                    // don't assume the client knows anything about us
                    var newState = ServerGameStateManager.GetEntityState(_entMan.ComponentManager, session, entityUid, GameTick.Zero);
                    entityStates.Add(newState);
                }
            }

            // swap out vis sets
            _playerVisibleSets[session] = currentSet;
            previousSet.Clear();
            _visSetPool.Return(previousSet);
            return entityStates;
        }

        private HashSet<EntityUid> CalcCurrentViewSet(ICommonSession session)
        {
            var visibleEnts = _visSetPool.Get();

            //TODO: Refactor map system to not require every map and grid entity to function.
            IncludeMapCriticalEntities(visibleEnts);

            // if you don't have an attached entity, you don't see the world.
            if (session.AttachedEntityUid is null)
                return visibleEnts;

            var viewers = GetSessionViewers(session);

            foreach (var eyeEuid in viewers)
            {
                var (viewBox, mapId) = CalcViewBounds(in eyeEuid);

                uint visMask = 0;
                if (_compMan.TryGetComponent<EyeComponent>(eyeEuid, out var eyeComp))
                    visMask = eyeComp.VisibilityMask;

                //Always include the map entity of the eye
                //TODO: Add Map entity here

                //Always include viewable ent itself
                visibleEnts.Add(eyeEuid);

                // grid entity should be added through this
                // assume there are no deleted ents in here, cull them first in ent/comp manager
                _lookup.FastEntitiesIntersecting(in mapId, ref viewBox, entity => RecursiveAdd((TransformComponent) entity.Transform, visibleEnts, visMask));
            }

            viewers.Clear();
            _viewerEntsPool.Return(viewers);

            return visibleEnts;
        }

        private void IncludeMapCriticalEntities(ISet<EntityUid> set)
        {
            foreach (var mapId in _mapManager.GetAllMapIds())
            {
                if (_mapManager.HasMapEntity(mapId))
                {
                    set.Add(_mapManager.GetMapEntityId(mapId));
                }
            }

            foreach (var grid in _mapManager.GetAllGrids())
            {
                if (grid.GridEntityId != EntityUid.Invalid)
                {
                    set.Add(grid.GridEntityId);
                }
            }
        }

        // Read Safe

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private bool RecursiveAdd(TransformComponent xform, HashSet<EntityUid> visSet, uint visMask)
        {
            var xformUid = xform.Owner.Uid;

            // we are done, this ent has already been checked and is visible
            if (visSet.Contains(xformUid))
                return true;

            // if we are invisible, we are not going into the visSet, so don't worry about parents, and children are not going in
            if (_compMan.TryGetComponent<VisibilityComponent>(xformUid, out var visComp))
            {
                if ((visMask & visComp.Layer) == 0)
                    return false;
            }

            var xformParentUid = xform.ParentUid;

            // this is the world entity, it is always visible
            if (!xformParentUid.IsValid())
            {
                visSet.Add(xformUid);
                return true;
            }

            // parent is already in the set
            if (visSet.Contains(xformParentUid))
            {
                visSet.Add(xformUid);
                return true;
            }

            // parent was not added, so we are not either
            var xformParent = _compMan.GetComponent<TransformComponent>(xformParentUid);
            if (!RecursiveAdd(xformParent, visSet, visMask))
                return false;

            // add us
            visSet.Add(xformUid);
            return true;
        }

        // Read Safe
        private (Box2 view, MapId mapId) CalcViewBounds(in EntityUid euid)
        {
            var xform = _compMan.GetComponent<ITransformComponent>(euid);

            var view = Box2.UnitCentered.Scale(ViewSize).Translated(xform.WorldPosition);
            var map = xform.MapID;

            return (view, map);
        }
    }
}
