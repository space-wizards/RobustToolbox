using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.ObjectPool;
using Robust.Server.GameObjects;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates
{
    internal class EntityViewCulling
    {
        private const int ViewSetSize = 128; // starting number of entities that are in view
        private const int PlayerSetSize = 64; // Starting number of players
        private const int MaxVisPoolSize = 256; // Maximum number of pooled objects, this should always be at least the max number of players

        private readonly IServerEntityManager _entMan;
        private readonly IComponentManager _compMan;
        private readonly IMapManager _mapManager;

        private readonly Dictionary<ICommonSession, HashSet<EntityUid>> _playerVisibleSets = new(PlayerSetSize);

        private readonly List<(GameTick tick, EntityUid uid)> _deletionHistory = new();

        private readonly ObjectPool<HashSet<EntityUid>> _visSetPool
            = new DefaultObjectPool<HashSet<EntityUid>>(new DefaultPooledObjectPolicy<HashSet<EntityUid>>(), MaxVisPoolSize);


        /// <summary>
        /// Size of the side of the view bounds square.
        /// </summary>
        public float ViewSize { get; set; }

        /// <summary>
        /// Is view culling enabled, or will we send the whole map?
        /// </summary>
        public bool CullingEnabled { get; set; }

        public EntityViewCulling(IServerEntityManager entMan, IMapManager mapManager)
        {
            _entMan = entMan;
            _compMan = entMan.ComponentManager;
            _mapManager = mapManager;
            _compMan = _entMan.ComponentManager;
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

        private List<EntityUid>? GetDeletedEntities(GameTick fromTick)
        {
            var list = new List<EntityUid>();
            foreach (var (tick, id) in _deletionHistory)
            {
                if (tick >= fromTick)
                {
                    list.Add(id);
                }
            }

            // no point sending an empty collection
            return list.Count == 0 ? default : list;
        }

        // Not thread safe
        public void AddPlayer(ICommonSession session)
        {
            _playerVisibleSets.Add(session, new HashSet<EntityUid>(ViewSetSize));
        }

        // Not thread safe
        public void RemovePlayer(ICommonSession session)
        {
            _playerVisibleSets.Remove(session);
        }

        // thread safe
        public bool IsPointVisible(ICommonSession session, in MapCoordinates position)
        {
            //TODO: This needs to support multiple bubbles per client
            //TODO: Visibility checks

            var bounds = CalcViewBounds(session);

            if (bounds is null)
                return false;

            var (viewBox, mapId) = bounds.Value;

            if (!CullingEnabled && mapId == position.MapId)
                return true;
            
            return mapId == position.MapId && viewBox.Contains(position.Position);
        }

        // thread safe
        public (List<EntityState>? updates, List<EntityUid>? deletions) CalculateEntityStates(ICommonSession session, GameTick fromTick)
        {
            DebugTools.Assert(session.Status == SessionStatus.InGame);

            //TODO: Stop sending all sim entities to every player first tick
            List<EntityUid>? deletions;
            if (!CullingEnabled || fromTick == GameTick.Zero)
            {
                var allStates = _entMan.GetEntityStates(session, fromTick);
                deletions = GetDeletedEntities(fromTick);
                return (allStates, deletions);
            }

            var currentSet = CalcCurrentViewSet(session, fromTick);

            // If they don't have a usable eye, nothing to send, and map remove will deal with ent removal
            if (currentSet is null)
                return (null, null);

            // pretty big allocations :(
            List<EntityState> entityStates = new(currentSet.Count);
            var previousSet = _playerVisibleSets[session];

            // complement set
            var leaveSet = ExceptIterator(previousSet, currentSet);
            foreach (var entityUid in leaveSet)
            {
                //TODO: PVS Leave Message
            }

            foreach (var entityUid in currentSet)
            {
                if (previousSet.Contains(entityUid))
                {
                    //Still Visible
                    // only send new changes
                    var newState = _entMan.GetEntityState(entityUid, fromTick, session);

                    if (!newState.Empty)
                        entityStates.Add(newState);
                }
                else
                {
                    // PVS enter message
                    // don't assume the client knows anything about us
                    var newState = _entMan.GetEntityState(entityUid, GameTick.Zero, session);
                    entityStates.Add(newState);
                }
            }
            
            // swap out vis sets
            _playerVisibleSets[session] = currentSet;
            previousSet.Clear();
            _visSetPool.Return(previousSet);

            deletions = GetDeletedEntities(fromTick);

            return (entityStates, deletions);
        }

        private IEnumerable<EntityUid> ExceptIterator(HashSet<EntityUid> first, HashSet<EntityUid> second)
        {
            // Pulled out of linq, figure out a better way
            HashSet<EntityUid> set = _visSetPool.Get();
            set.UnionWith(second);

            foreach (var element in first)
            {
                if (set.Add(element))
                {
                    yield return element;
                }
            }

            set.Clear();
            _visSetPool.Return(set);
        }

        private HashSet<EntityUid>? CalcCurrentViewSet(ICommonSession session, GameTick fromTick)
        {
            var bounds = CalcViewBounds(session);

            if (bounds is null)
                return null;
            
            var (viewBox, mapId) = bounds.Value;

            //TODO: Eye Components
            if (session.AttachedEntityUid is null ||
                !_compMan.TryGetComponent<EyeComponent>(session.AttachedEntityUid.Value, out var eyeComp))
                return null;

            var visibilityMask = eyeComp.VisibilityMask;
            var visibleEnts = _visSetPool.Get();

            // assume there are no deleted ents in here, cull them first in ent/comp manager
            _entMan.FastEntitiesIntersecting(mapId, ref viewBox, entity =>
            {
                RecursiveAdd(entity.Transform, visibleEnts, visibilityMask);
            });

            // TODO: Need eye-based technology
            //Always include client AttachedEnt
            var eyeEnt = session.AttachedEntityUid!; // already verified in CalcBounds
            visibleEnts.Add(eyeEnt.Value);

            //Ensure map Critical ents are included
            IncludeMapCriticalEntities(visibleEnts);

            return visibleEnts;
        }

        private bool RecursiveAdd(ITransformComponent xform, ISet<EntityUid> visSet, uint visMask)
        {
            // we are done, this ent has already been checked and is visible
            if(visSet.Contains(xform.Owner.Uid))
                return true;

            // if we are invisible, we are not going into the visSet, so don't worry about parents, and children are not going in
            if (_compMan.TryGetComponent<VisibilityComponent>(xform.Owner.Uid, out var visComp))
            {
                if ((visMask & visComp.Layer) == 0)
                    return false;
            }

            var xformParent = xform.Parent;

            // this is the world entity, it is always visible
            if (xformParent is null)
            {
                visSet.Add(xform.Owner.Uid);
                return true;
            }

            // parent was not added, so we are not either
            if (!RecursiveAdd(xformParent, visSet, visMask))
                return false;

            // add us
            visSet.Add(xform.Owner.Uid);
            return true;
        }

        // thread safe
        private (Box2 view, MapId mapId)? CalcViewBounds(ICommonSession playerSession)
        {
            //TODO: This needs to support multiple bubbles per client

            var clientEnt = playerSession.AttachedEntityUid;

            if (clientEnt is null)
                return null;

            var xform = _compMan.GetComponent<ITransformComponent>(clientEnt.Value);

            if (!CullingEnabled)
                return (new Box2(), xform.MapID);

            var view = Box2.UnitCentered.Scale(ViewSize).Translated(xform.WorldPosition);
            var map = xform.MapID;

            return (view, map);
        }

        // Read safe
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
        private static void ExcludeInvisible(HashSet<IEntity> set, int visibilityMask)
        {
            set.RemoveWhere(e =>
            {
                if (!e.TryGetComponent(out VisibilityComponent? visibility))
                {
                    return false;
                }

                return (visibilityMask & visibility.Layer) == 0;
            });
        }
    }
}
