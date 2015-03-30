using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS14.Server.GameObjects.Organs
{
    /// <summary>
    /// Represents an Internal Organ
    /// </summary>
    public class InternalOrgan : Organ
    {
        public ExternalOrgan Parent;
        public int Damage;


    }
}
