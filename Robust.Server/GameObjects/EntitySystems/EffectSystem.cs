using System.Collections.Generic;
using System.Linq;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameObjects
{
    /// <summary>
    /// An entity system that displays temporary effects to the user
    /// </summary>
    public class EffectSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        /// <summary>
        /// Priority queue sorted by how soon the effect will die, we remove messages from the front of the queue during update until caught up
        /// </summary>
        private readonly PriorityQueue<EffectSystemMessage> _currentEffects = new(new EffectMessageComparer());

        /// <summary>
        ///     Creates a particle effect and sends it to clients.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="excludedSession">Session to be excluded for prediction</param>
        public void CreateParticle(EffectSystemMessage effect, IPlayerSession? excludedSession = null)
        {
            _currentEffects.Add(effect);

            //For now we will use this which sends to ALL clients
            //TODO: Client bubbling
            foreach (var player in _playerManager.ServerSessions)
            {
                if (player.Status != SessionStatus.InGame || player == excludedSession)
                    continue;
                
                RaiseNetworkEvent(effect, player.ConnectedClient);
            }
        }

        public override void Update(float frameTime)
        {
            //Take elements from front of priority queue until they are old
            while (_currentEffects.Count != 0 && _currentEffects.Peek().DeathTime < _timing.CurTime)
            {
                _currentEffects.Take();
            }
        }

        /// <summary>
        /// Comparer that keeps the device dictionary sorted by powernet priority
        /// </summary>
        public class EffectMessageComparer : IComparer<EffectSystemMessage>
        {
            public int Compare(EffectSystemMessage? x, EffectSystemMessage? y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }

                if (y == null)
                {
                    return 1;
                }

                if (x == null)
                {
                    return -1;
                }

                return y.DeathTime.CompareTo(x.DeathTime);
            }
        }

        //TODO: Send all current effects to new clients on login
        //TODO: Send effects only to relevant client bubbles
    }
}
