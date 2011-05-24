using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using Mogre;

namespace SS3D_shared
{
    public abstract class Item : AtomBaseClass
    {
        public ushort itemID = 0;
        public string meshName;
        public ServerItemInfo serverInfo;
        public List<InterpolationPacket> interpolationPacket;
        public DateTime lastUpdate;
        public ItemType ItemType = ItemType.None;
        public Mob holder; // Who's holding us?
        public MobHand holderHand; // Which hand are we held in?

        public float power = 0.0f; // This is just a temporary thing to differentiate how much damage each item will do.

        public Mogre.Vector3 heldOffset = Mogre.Vector3.ZERO;           // the offset vector when held
        public Mogre.Quaternion heldQuat = Mogre.Quaternion.IDENTITY;   // the rotation when held

        public Item()
        {
            AtomType = global::AtomType.Item;
        }

    }
}
