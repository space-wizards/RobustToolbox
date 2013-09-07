using System.Collections.Generic;
using System.Linq;
using GameObject;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO
{
    public class NewHandsComponent : Component
    {
        public readonly Dictionary<Hand, Entity> handslots;
        public Hand currentHand = Hand.Left;

        public NewHandsComponent()
        {
            Family = ComponentFamily.Hands;
            handslots = new Dictionary<Hand, Entity>();
        }

    }
}