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
        public Filter AddPlayersByPvs(IEntity origin, float rangeMultiplier = 2f)
        {
            return AddPlayersByPvs(origin.Transform.MapPosition, rangeMultiplier);
        }

        /// <summary>
        ///     Adds all players inside an entity's PVS.
        ///     The current PVS range will be multiplied by <see cref="rangeMultiplier"/>.
        /// </summary>
        public Filter AddPlayersByPvs(ITransformComponent origin, float rangeMultiplier = 2f)
        {
            return AddPlayersByPvs(origin.MapPosition, rangeMultiplier);
        }

        /// <summary>
        ///     Adds all players inside an entity's PVS.
        ///     The current PVS range will be multiplied by <see cref="rangeMultiplier"/>.
        /// </summary>
        public Filter AddPlayersByPvs(EntityCoordinates origin, float rangeMultiplier = 2f)
        {
            var entityMan = IoCManager.Resolve<IEntityManager>();
            return AddPlayersByPvs(origin.ToMap(entityMan), rangeMultiplier);
        }

        /// <summary>
        ///     Adds all players inside an entity's PVS.
        ///     The current PVS range will be multiplied by <see cref="rangeMultiplier"/>.
        /// </summary>
        public Filter AddPlayersByPvs(MapCoordinates origin, float rangeMultiplier = 2f)
        {
            var cfgMan = IoCManager.Resolve<IConfigurationManager>();

            // If PVS is disabled, we simply return all players.
            if (!cfgMan.GetCVar(CVars.NetPVS))
                return AddAllPlayers();

            var pvsRange = cfgMan.GetCVar(CVars.NetMaxUpdateRange) * rangeMultiplier;

            return AddInRange(origin, pvsRange);
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
        public Filter AddAllPlayers()
        {
            var playerMan = IoCManager.Resolve<ISharedPlayerManager>();

            _recipients = new HashSet<ICommonSession>(playerMan.NetworkedSessions);

            return this;
        }

        /// <summary>
        ///     Adds all players that match a predicate.
        /// </summary>
        public Filter AddWhere(Predicate<ICommonSession> predicate)
        {
            var playerMan = IoCManager.Resolve<ISharedPlayerManager>();
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
        public Filter AddWhereAttachedEntity(Predicate<IEntity> predicate)
        {
            return AddWhere(session => session.AttachedEntity is { } entity && predicate(entity));
        }

        /// <summary>
        ///     Add all players whose entity is on a certain grid.
        /// </summary>
        public Filter AddInGrid(GridId gridId)
        {
            return AddWhereAttachedEntity(entity => entity.Transform.GridID == gridId);
        }

        /// <summary>
        ///     Add all players whose entity is on a certain map.
        /// </summary>
        public Filter AddInMap(MapId mapId)
        {
            return AddWhereAttachedEntity(entity => entity.Transform.MapID == mapId);
        }

        /// <summary>
        ///     Adds all players in range of a position.
        /// </summary>
        public Filter AddInRange(MapCoordinates position, float range)
        {
            return AddWhere(session =>
                session.AttachedEntity != null &&
                position.InRange(session.AttachedEntity.Transform.MapPosition, range));
        }

        /// <summary>
        ///     Removes all players without the specified visibility flag.
        /// </summary>
        public Filter RemoveByVisibility(uint flag)
        {
            return RemoveWhere(session =>
                session.AttachedEntity == null
                || !session.AttachedEntity.TryGetComponent(out SharedEyeComponent? eye)
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
        /// <param name="predicate"></param>
        /// <returns></returns>
        public Filter RemoveWhereAttachedEntity(Predicate<IEntity> predicate)
        {
            _recipients.RemoveWhere(session => session.AttachedEntity is { } entity && predicate(entity));
            return this;
        }

        /// <summary>
        ///     Removes all players in range of a position.
        /// </summary>
        public Filter RemoveInRange(MapCoordinates position, float range)
        {
            return RemoveWhere(session =>
                session.AttachedEntity != null &&
                position.InRange(session.AttachedEntity.Transform.MapPosition, range));
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
        public static Filter Pvs(IEntity origin, float rangeMultiplier = 2f)
        {
            return Empty().AddPlayersByPvs(origin, rangeMultiplier);
        }

        /// <summary>
        ///     A filter with every player who's PVS overlaps this point.
        /// </summary>
        public static Filter Pvs(ITransformComponent origin, float rangeMultiplier = 2f)
        {
            return Empty().AddPlayersByPvs(origin, rangeMultiplier);
        }

        /// <summary>
        ///     A filter with every player who's PVS overlaps this point.
        /// </summary>
        public static Filter Pvs(EntityCoordinates origin, float rangeMultiplier = 2f)
        {
            return Empty().AddPlayersByPvs(origin, rangeMultiplier);
        }

        /// <summary>
        ///     A filter with every player who's PVS overlaps this point.
        /// </summary>
        public static Filter Pvs(MapCoordinates origin, float rangeMultiplier = 2f)
        {
            return Empty().AddPlayersByPvs(origin, rangeMultiplier);
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
