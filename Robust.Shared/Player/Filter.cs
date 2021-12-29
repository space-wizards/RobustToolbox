using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Players;

namespace Robust.Shared.Player
{
    /// <summary>
    ///     Contains a set of recipients for a networked method call.
    /// </summary>
    [PublicAPI]
    public class Filter
    {
        private HashSet<ICommonSession> _recipients = new();

        private Filter() { }

        public bool CheckPrediction { get; private set; } = true;

        public bool SendReliable { get; private set; }

        public IEnumerable<ICommonSession> Recipients => _recipients;

        /// <summary>
        ///     Adds a single player to the filter.
        /// </summary>
        public Filter AddPlayer(ICommonSession player)
        {
            _recipients.Add(player);
            return this;
        }


        /// <summary>
        ///     Adds all players inside an entity's PVS.
        ///     The current PVS range will be multiplied by <see cref="rangeMultiplier"/>.
        /// </summary>
        public Filter AddPlayersByPvs(EntityUid origin, float rangeMultiplier = 2f, IEntityManager? entityManager = null)
        {
            IoCManager.Resolve(ref entityManager);
            var transform = entityManager.GetComponent<TransformComponent>(origin);
            return AddPlayersByPvs(transform.MapPosition, rangeMultiplier);
        }

        /// <summary>
        ///     Adds all players inside an entity's PVS.
        ///     The current PVS range will be multiplied by <see cref="rangeMultiplier"/>.
        /// </summary>
        public Filter AddPlayersByPvs(TransformComponent origin, float rangeMultiplier = 2f)
        {
            return AddPlayersByPvs(origin.MapPosition, rangeMultiplier);
        }

        /// <summary>
        ///     Adds all players inside an entity's PVS.
        ///     The current PVS range will be multiplied by <see cref="rangeMultiplier"/>.
        /// </summary>
        public Filter AddPlayersByPvs(EntityCoordinates origin, float rangeMultiplier = 2f, IEntityManager? entityMan = null, ISharedPlayerManager? playerMan = null)
        {
            IoCManager.Resolve(ref entityMan, ref playerMan);
            return AddPlayersByPvs(origin.ToMap(entityMan), rangeMultiplier, playerMan);
        }

        /// <summary>
        ///     Adds all players inside an entity's PVS.
        ///     The current PVS range will be multiplied by <see cref="rangeMultiplier"/>.
        /// </summary>
        public Filter AddPlayersByPvs(MapCoordinates origin, float rangeMultiplier = 2f, ISharedPlayerManager? playerMan = null, IConfigurationManager? cfgMan = null)
        {
            IoCManager.Resolve(ref playerMan, ref cfgMan);

            // If PVS is disabled, we simply return all players.
            if (!cfgMan.GetCVar(CVars.NetPVS))
                return AddAllPlayers();

            var pvsRange = cfgMan.GetCVar(CVars.NetMaxUpdateRange) * rangeMultiplier;

            return AddInRange(origin, pvsRange, playerMan);
        }

        /// <summary>
        ///     Adds a set of players to the filter.
        /// </summary>
        public Filter AddPlayers(IEnumerable<ICommonSession> players)
        {
            foreach (var player in players)
            {
                AddPlayer(player);
            }

            return this;
        }

        /// <summary>
        ///     Adds all players to the filter.
        /// </summary>
        public Filter AddAllPlayers(ISharedPlayerManager? playerMan = null)
        {
            IoCManager.Resolve(ref playerMan);

            _recipients = new HashSet<ICommonSession>(playerMan.NetworkedSessions);

            return this;
        }

        /// <summary>
        ///     Adds all players that match a predicate.
        /// </summary>
        public Filter AddWhere(Predicate<ICommonSession> predicate, ISharedPlayerManager? playerMan = null)
        {
            IoCManager.Resolve(ref playerMan);
            foreach (var player in playerMan.NetworkedSessions)
            {
                if (predicate(player))
                {
                    AddPlayer(player);
                }
            }

            return this;
        }

        /// <summary>
        ///     Add all players whose attached entity match a predicate.
        ///     Doesn't consider players without an attached entity.
        /// </summary>
        public Filter AddWhereAttachedEntity(Predicate<EntityUid> predicate)
        {
            return AddWhere(session => session.AttachedEntity is { } uid && predicate(uid));
        }

        /// <summary>
        ///     Add all players whose entity is on a certain grid.
        /// </summary>
        public Filter AddInGrid(GridId gridId, IEntityManager? entMan = null)
        {
            IoCManager.Resolve(ref entMan);
            return AddWhereAttachedEntity(entity => entMan.GetComponent<TransformComponent>(entity).GridID == gridId);
        }

        /// <summary>
        ///     Add all players whose entity is on a certain map.
        /// </summary>
        public Filter AddInMap(MapId mapId, IEntityManager? entMan = null)
        {
            IoCManager.Resolve(ref entMan);
            return AddWhereAttachedEntity(entity => entMan.GetComponent<TransformComponent>(entity).MapID == mapId);
        }

        /// <summary>
        ///     Adds all players in range of a position.
        /// </summary>
        public Filter AddInRange(MapCoordinates position, float range, ISharedPlayerManager? playerMan = null, IEntityManager? entMan = null)
        {
            IoCManager.Resolve(ref entMan);

            return AddWhere(session =>
                session.AttachedEntity != null &&
                position.InRange(entMan.GetComponent<TransformComponent>(session.AttachedEntity.Value).MapPosition, range), playerMan);
        }

        /// <summary>
        ///     Removes all players without the specified visibility flag.
        /// </summary>
        public Filter RemoveByVisibility(uint flag, IEntityManager? entMan = null)
        {
            IoCManager.Resolve(ref entMan);

            return RemoveWhere(session =>
                session.AttachedEntity == null
                || !entMan.TryGetComponent(session.AttachedEntity, out SharedEyeComponent? eye)
                || (eye.VisibilityMask & flag) == 0);
        }

        /// <summary>
        ///     Removes a single player from the filter.
        /// </summary>
        public Filter RemovePlayer(ICommonSession player)
        {
            _recipients.Remove(player);
            return this;
        }

        /// <summary>
        ///     Removes all players from the filter that match a predicate.
        /// </summary>
        public Filter RemoveWhere(Predicate<ICommonSession> predicate)
        {
            _recipients.RemoveWhere(predicate);
            return this;
        }

        /// <summary>
        ///     Removes all players whose attached entity match a predicate.
        ///     Doesn't consider players without an attached entity.
        /// </summary>
        public Filter RemoveWhereAttachedEntity(Predicate<EntityUid> predicate)
        {
            _recipients.RemoveWhere(session => session.AttachedEntity is { } uid && predicate(uid));
            return this;
        }

        /// <summary>
        ///     Removes all players in range of a position.
        /// </summary>
        public Filter RemoveInRange(MapCoordinates position, float range, IEntityManager? entMan = null)
        {
            IoCManager.Resolve(ref entMan);

            return RemoveWhere(session =>
                session.AttachedEntity != null &&
                position.InRange(entMan.GetComponent<TransformComponent>(session.AttachedEntity.Value).MapPosition, range));
        }

        /// <summary>
        ///     Adds all players from a different filter into this one.
        /// </summary>
        public Filter Merge(Filter other)
        {
            return AddPlayers(other._recipients);
        }

        /// <summary>
        ///     Adds all players attached to the given entities to this filter, then returns it.
        /// </summary>
        public Filter FromEntities(params EntityUid[] entities)
        {
            return EntitySystem.TryGet(out SharedFilterSystem? filterSystem)
                ? filterSystem.FromEntities(this, entities)
                : this;
        }

        /// <summary>
        ///     Returns a new filter with the same parameters as this one.
        /// </summary>
        public Filter Clone()
        {
            return new()
            {
                _recipients = new HashSet<ICommonSession>(_recipients),
                SendReliable = SendReliable,
                CheckPrediction = CheckPrediction,
            };
        }

        /// <summary>
        ///     Normally a filter will properly handle client side prediction. Calling this
        ///     function disables that, and the event will be spammed during every prediction
        ///     tick.
        /// </summary>
        public Filter Unpredicted()
        {
            CheckPrediction = false;
            return this;
        }

        /// <summary>
        ///     Should it be guaranteed that recipients receive the message?
        /// </summary>
        public Filter SendReliably()
        {
            SendReliable = true;
            return this;
        }

        /// <summary>
        ///     A new filter that is empty.
        /// </summary>
        public static Filter Empty()
        {
            return new();
        }

        /// <summary>
        ///     A new filter with a single player in it.
        /// </summary>
        public static Filter SinglePlayer(ICommonSession player)
        {
            return Empty().AddPlayer(player);
        }

        /// <summary>
        ///     A new filter with all players in it.
        /// </summary>
        public static Filter Broadcast()
        {
            return Empty().AddAllPlayers();
        }

        /// <summary>
        ///     A new filter with all players whose attached entity is on a certain grid.
        /// </summary>
        public static Filter BroadcastGrid(GridId grid)
        {
            return Empty().AddInGrid(grid);
        }

        /// <summary>
        ///     A new filter with all players whose attached entity is on a certain map.
        /// </summary>
        public static Filter BroadcastMap(MapId map)
        {
            return Empty().AddInMap(map);
        }

        /// <summary>
        ///     A filter with every player who's PVS overlaps this entity.
        /// </summary>
        public static Filter Pvs(EntityUid origin, float rangeMultiplier = 2f, IEntityManager? entityManager = null)
        {
            return Empty().AddPlayersByPvs(origin, rangeMultiplier, entityManager);
        }

        /// <summary>
        ///     A filter with every player who's PVS overlaps this point.
        /// </summary>
        public static Filter Pvs(TransformComponent origin, float rangeMultiplier = 2f)
        {
            return Empty().AddPlayersByPvs(origin, rangeMultiplier);
        }

        /// <summary>
        ///     A filter with every player who's PVS overlaps this point.
        /// </summary>
        public static Filter Pvs(EntityCoordinates origin, float rangeMultiplier = 2f, IEntityManager? entityMan = null, ISharedPlayerManager? playerMan = null)
        {
            return Empty().AddPlayersByPvs(origin, rangeMultiplier, entityMan, playerMan);
        }

        /// <summary>
        ///     A filter with every player who's PVS overlaps this point.
        /// </summary>
        public static Filter Pvs(MapCoordinates origin, float rangeMultiplier = 2f)
        {
            return Empty().AddPlayersByPvs(origin, rangeMultiplier);
        }

        /// <summary>
        ///     A filter with every player attached to the given entities.
        /// </summary>
        public static Filter Entities(params EntityUid[] entities)
        {
            return Empty().FromEntities(entities);
        }

        /// <summary>
        ///     A filter with only the local player.
        /// </summary>
        public static Filter Local()
        {
            return Empty();
        }
    }
}
