using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS13_Shared.GO
{
    [Serializable]
    public class ComponentState
    {
        public byte Family { get; private set; }
        public ComponentState(ComponentFamily family)
        {
            Family = (byte)family;
        }
    }
}
