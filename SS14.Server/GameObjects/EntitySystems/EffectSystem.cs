using DataStructures;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.EntitySystemMessages;
using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;

namespace SS14.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// An entity system that displays temporary effects to the user
    /// </summary>
    public class EffectSystem : EntitySystem
    {
        /// <summary>
        /// Priority queue sorted by how soon the effect will die, we remove messages from the front of the queue during update until caught up
        /// </summary>
        private PriorityQueue<EffectSystemMessage> _CurrentEffects = new PriorityQueue<EffectSystemMessage>(new EffectMessageComparer());

        public void CreateParticle(EffectSystemMessage effect)
        {
            _CurrentEffects.Add(effect);
            
            //For now we will use this which sends to ALL clients
            //TODO: Client bubbling
            RaiseNetworkEvent(effect);
        }

        public override void Update(float frametime)
        {
            var gametime = IoCManager.Resolve<IGameTiming>().CurTime;

            //Take elements from front of priority queue until they are old
            while(_CurrentEffects.Count != 0 && _CurrentEffects.Peek().DeathTime < gametime)
            {
                _CurrentEffects.Take();
            }
        }

        /// <summary>
        /// Comparer that keeps the device dictionary sorted by powernet priority
        /// </summary>
        public class EffectMessageComparer : IComparer<EffectSystemMessage>
        {
            public int Compare(EffectSystemMessage x, EffectSystemMessage y)
            {
                return y.DeathTime.CompareTo(x.DeathTime);
            }
        }

        //TODO: Send all current effects to new clients on login
        //TODO: Send effects only to relevant client bubbles
    }
}
