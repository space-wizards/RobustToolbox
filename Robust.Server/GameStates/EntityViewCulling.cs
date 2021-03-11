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
        private const int PlayerSetSize = 64; // Starting size of the number of players
        private const int MaxVisPoolSize = 256; // Maximum number of pooled objects, this should always be at least the max number of players

        private readonly IServerEntityManager _entMan;
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
            _mapManager = mapManager;
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

            var previousSet = _playerVisibleSets[session];

            //TODO: Set theory to calc enter/exit events

            // pretty big allocations :(
            List<EntityState> entityStates = new(currentSet.Count);
            foreach (var entityUid in currentSet)
            {
                var newState = _entMan.GetEntityState(entityUid, fromTick, session);

                if(!newState.Empty)
                    entityStates.Add(newState);
            }

            // pivot out vis sets
            _playerVisibleSets[session] = currentSet;
            previousSet.Clear();
            _visSetPool.Return(previousSet);

            deletions = GetDeletedEntities(fromTick);

            return (entityStates, deletions);
        }

        private HashSet<EntityUid>? CalcCurrentViewSet(ICommonSession session, GameTick fromTick)
        {
            var bounds = CalcViewBounds(session);

            if (bounds is null)
                return null;
            
            var (viewBox, mapId) = bounds.Value;

            // assume there are no deleted ents in here, cull them first in ent/comp manager
            var potentialVisibleEnts = _entMan.GetEntitiesIntersecting(mapId, viewBox);
            var visibleEnts = _visSetPool.Get();
            //TODO: Move visibility to EyeComponent

            foreach (var potentialEnt in potentialVisibleEnts)
            {
                //TODO: Add Parents
                //TODO: Container culling
                //TODO: per-eye VisibilityComponent cull

                //TODO: If parent is already in the set, it has already been checked
                visibleEnts.Add(potentialEnt.Uid);
            }

            // TODO: Need eye-based technology
            //Always include client AttachedEnt
            var eyeEnt = session.AttachedEntityUid!; // already verified in CalcBounds
            visibleEnts.Add(eyeEnt.Value);

            //Ensure map Critical ents are included
            IncludeMapCriticalEntities(visibleEnts);

            return visibleEnts;
        }

        // thread safe
        private (Box2 view, MapId mapId)? CalcViewBounds(ICommonSession playerSession)
        {
            //TODO: This needs to support multiple bubbles per client

            var clientEnt = playerSession.AttachedEntityUid;

            if (clientEnt is null)
                return null;

            var xform = _entMan.ComponentManager.GetComponent<ITransformComponent>(clientEnt.Value);

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
