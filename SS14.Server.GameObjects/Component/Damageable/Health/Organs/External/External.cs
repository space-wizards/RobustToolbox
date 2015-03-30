using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS14.Shared;


namespace SS14.Server.GameObjects.Organs
{
    /// <summary>
    /// Represents an External Organ
    /// </summary>
    public class ExternalOrgan : Organ
    {
        /// <summary>
        /// The organ that this is attached to (like an arm)
        /// </summary>
        public ExternalOrgan Parent;
        public List<ExternalOrgan> Children; // The organ that is attached to this (like a hand)
        public List<InternalOrgan> InternalOrgans;
        public int Damage;
        public BodyPart zone;

              


    }
}
