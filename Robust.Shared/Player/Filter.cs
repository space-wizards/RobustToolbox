using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Robust.Shared.Player
{
    /// <summary>
    /// A filter for recipients of a networked method call.
    /// </summary>
    public interface IFilter
    {
        /// <summary>
        /// Should this networked call be properly predicted?
        /// True: Check things like IsFirstTimePredicted().
        /// False: JUST DO IT.
        /// </summary>
        bool CheckPrediction { get; }

        /// <summary>
        /// Should this network call be sent as reliable?
        /// </summary>
        bool SendReliable { get; }

        /// <summary>
        /// Networked sessions that should receive the event.
        /// </summary>
        IList<ICommonSession> Recipients { get; }
    }

    /// <summary>
    /// Contains a set of recipients for a networked method call.
    /// </summary>
    [PublicAPI]
    public class Filter : IFilter
    {
        private bool _prediction;
        private List<ICommonSession> _recipients = new();
        private bool _reliable;

        private Filter() { }

        bool IFilter.CheckPrediction => _prediction;
        bool IFilter.SendReliable => _reliable;
        IList<ICommonSession> IFilter.Recipients => _recipients;

        /// <summary>
        /// Adds a single player to the filter.
        /// </summary>
        public Filter AddPlayer(ICommonSession player)
        {
            _recipients.Add(player);
            return this;
        }

        /// <summary>
        /// Adds all players inside an entity's PVS.
        /// </summary>
        protected Filter AddPlayersByPvs(Vector2 origin)
        {
            // Calculate this from the PVS system that does not exist.
            throw new NotImplementedException();

            return this;
        }

        public Filter AddPlayers(IEnumerable<ICommonSession> players)
        {
            foreach (var player in players)
            {
                AddPlayer(player);
            }

            return this;
        }

        /// <summary>
        /// Adds all players to the filter.
        /// </summary>
        public Filter AddAllPlayers()
        {
            _recipients.Clear();

            var playerMan = IoCManager.Resolve<ISharedPlayerManager>();
            _recipients.AddRange(playerMan.NetworkedSessions);
            return this;
        }

        /// <summary>
        /// Removes a single player from the filter.
        /// </summary>
        public Filter RemovePlayer(ICommonSession player)
        {
            _recipients.Remove(player);
            return this;
        }
        
        public Filter RemoveWhere(Predicate<ICommonSession> predicate)
        {
            for (int i = 0; i < _recipients.Count; i++)
            {
                var player = _recipients[i];

                if (predicate(player))
                {
                    _recipients.RemoveSwap(i);
                    i--;
                }
            }

            return this;
        }

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
        /// This filter will properly be handled by prediction.
        /// </summary>
        public Filter HandlePrediction()
        {
            _prediction = true;
            return this;
        }

        /// <summary>
        /// Should it be guaranteed that recipients receive the message?
        /// </summary>
        public Filter SendReliably()
        {
            _reliable = true;
            return this;
        }

        /// <summary>
        /// A new filter that is empty.
        /// </summary>
        /// <returns></returns>
        public static Filter Empty()
        {
            return new();
        }

        /// <summary>
        /// A new filter with a single player in it.
        /// </summary>
        public static Filter SinglePlayer(ICommonSession player)
        {
            return Empty().AddPlayer(player);
        }

        /// <summary>
        /// A new filter with all players in it.
        /// </summary>
        public static Filter Broadcast()
        {
            return Empty().AddAllPlayers();
        }

        /// <summary>
        /// A filter with every player who's PVS overlaps this point.
        /// </summary>
        public static Filter Pvs(Vector2 origin)
        {
            return Empty().AddPlayersByPvs(origin);
        }

        /// <summary>
        /// A filter with only the local player.
        /// </summary>
        public static Filter Local()
        {
            return Empty();
        }
    }
}
