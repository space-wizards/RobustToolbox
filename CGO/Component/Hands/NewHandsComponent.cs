using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.UserInterface;
using GameObject;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    public class NewHandsComponent : Component
    {
        public NewHandsComponent()
        {
            HandSlots = new Dictionary<Hand, Entity>();
            Family = ComponentFamily.Hands;
        }

        public Dictionary<Hand, Entity> HandSlots { get; private set; }
        public Hand CurrentHand { get; private set; }
    }
}