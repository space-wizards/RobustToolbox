using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using System.Runtime.Serialization;
using SGO;
using SGO.Component.Item.ItemCapability;

namespace SS3D_Server.Atom.Item.Tool
{
    [Serializable()]
    public class Crowbar : Tool
    {
        public Crowbar()
            : base()
        {
            name = "Crowbar";
        }

        public override void Initialize(bool loaded = false)
        {
            base.Initialize(loaded);

            BasicItemComponent itemcomp = (BasicItemComponent)this.GetComponent(SS3D_shared.GO.ComponentFamily.Item);
            ItemCapability cap = new ToolCapability();
            itemcomp.AddCapability(cap);
            cap.AddVerb(0, SS3D_shared.GO.ItemCapabilityVerb.Pry);

            cap = new MeleeWeaponCapability();
            cap.SetParameter(new ComponentParameter("damageAmount", typeof(int), 30));
            cap.SetParameter(new ComponentParameter("damageType", typeof(string), "Bludgeon"));
            itemcomp.AddCapability(cap);

            
        }

        public Crowbar(SerializationInfo info, StreamingContext ctxt)
        {
            SerializeBasicInfo(info, ctxt);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
        }

    }
}
