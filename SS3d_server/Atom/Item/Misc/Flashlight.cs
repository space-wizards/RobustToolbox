using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SS3D_shared.HelperClasses;
using System.Runtime.Serialization;
using SGO;
using SGO.Component.Item.ItemCapability;

namespace SS3D_Server.Atom.Item.Misc
{
    [Serializable()]
    public class Flashlight : Item
    {
        public Flashlight()
            : base()
        {
            name = "Flashlight";
        }

        public override void Initialize(bool loaded = false)
        {
            base.Initialize(loaded);

            BasicItemComponent itemcomp = (BasicItemComponent)this.GetComponent(SS3D_shared.GO.ComponentFamily.Item);
            ItemCapability cap = new ToolCapability();
            itemcomp.AddCapability(cap);
        }

        public Flashlight(SerializationInfo info, StreamingContext ctxt)
        {
            SerializeBasicInfo(info, ctxt);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
        }
    }
}
