using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;

namespace SGO.Component.Item.ItemCapability
{
    public class ItemCapability
    {
        public InteractsWith interactsWith; //What types of shit this interacts with
        public int priority; //Where in the stack this puppy is
        public ItemCapabilityType itemType;

        public bool ApplyTo(Entity target)
        {
            throw new NotImplementedException();
        }
    }
}
