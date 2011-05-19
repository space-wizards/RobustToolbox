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
        public Mob holder; // Just temporary holder for now.

        public float power = 0.0f;

        public Mogre.Vector3 heldOffset = Mogre.Vector3.ZERO;           // the offset vector when held
        public Mogre.Quaternion heldQuat = Mogre.Quaternion.IDENTITY;   // the rotation when held

        public Item()
        {
            AtomType = global::AtomType.Item;
        }

    }
}
